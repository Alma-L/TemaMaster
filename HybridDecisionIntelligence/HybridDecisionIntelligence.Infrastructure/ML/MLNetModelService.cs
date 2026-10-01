using System.Text.Json;
using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Domain.ValueObjects;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Trainers;

namespace HybridDecisionIntelligence.Infrastructure.ML
{
    /// <summary>
    /// Concrete implementation using ML.NET
    /// Binary classification model for banking decisions.
    /// Registered as a singleton so the model is loaded from disk once; PredictionEngine
    /// is not thread-safe, so every use of it is serialized through _sync.
    /// </summary>
    public class MLNetModelService : IMLModelService
    {
        private readonly object _sync = new();
        private readonly MLContext _mlContext;
        private readonly string _modelPath;
        private ITransformer? _trainedModel;
        private PredictionEngine<BankMarketingData, BankMarketingPrediction>? _predictionEngine;
        private readonly ILogger<MLNetModelService> _logger;

        public MLNetModelService(ILogger<MLNetModelService> logger, IConfiguration configuration)
        {
            _mlContext = new MLContext(seed: 0);
            _modelPath = configuration["MLModel:ModelPath"] ?? "Models/BankMarketingModel.zip";
            _logger = logger;
        }

        private string MetricsPath =>
            Path.ChangeExtension(_modelPath, ".metrics.json");

        private IDataView LoadData(string dataPath) =>
            _mlContext.Data.LoadFromTextFile<BankMarketingData>(
                dataPath, hasHeader: true, separatorChar: ';', allowQuoting: true);

        /// <summary>
        /// Train binary classification model on bank marketing data
        /// Uses fast tree learner for high accuracy, holding out 20% of the data to report
        /// Accuracy/AUC/F1/Precision/Recall on unseen rows before saving the model.
        /// </summary>
        public async Task<bool> TrainModelAsync(string dataPath)
        {
            try
            {
                _logger.LogInformation($"Starting model training with data from {dataPath}");

                // Load data
                var data = LoadData(dataPath);
                _logger.LogInformation("Data loaded successfully");

                var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2, seed: 0);

                // Build pipeline
                var pipeline = _mlContext.Transforms
                    .Categorical.OneHotEncoding("Job", "Job")
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("Marital", "Marital"))
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("Education", "Education"))
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("Default", "Default"))
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("Housing", "Housing"))
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("Loan", "Loan"))
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("Contact", "Contact"))
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("Month", "Month"))
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding("POutcome", "POutcome"))
                    // "Duration" (last-contact call length) is deliberately excluded.
                    // The UCI dataset documentation itself flags it as target leakage:
                    // you can't know how long a call will last before deciding whether
                    // to make it, so a model trained on it looks stronger than it would
                    // be in a real pre-decision scenario. Excluding it costs some
                    // accuracy/AUC but makes the model honest for this use case.
                    .Append(_mlContext.Transforms.Concatenate("Features",
                        new[] {
                            "Age", "Job", "Marital", "Education", "Default", "Balance",
                            "Housing", "Loan", "Contact", "Day", "Month",
                            "Campaign", "PDays", "Previous", "POutcome"
                        }))
                    .Append(_mlContext.Transforms.NormalizeMinMax("Features", "Features"))
                    .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                        labelColumnName: "Label",
                        featureColumnName: "Features",
                        numberOfTrees: 100,
                        numberOfLeaves: 10
                    ));

                _logger.LogInformation("Training pipeline created");

                // Train model on the 80% training split
                _trainedModel = pipeline.Fit(split.TrainSet);
                _logger.LogInformation("Model training completed");

                // Evaluate on the 20% held-out test split
                var testPredictions = _trainedModel.Transform(split.TestSet);
                var metrics = _mlContext.BinaryClassification.Evaluate(testPredictions, labelColumnName: "Label");
                _logger.LogInformation(
                    "Held-out evaluation - Accuracy: {Accuracy:P2}, AUC: {Auc:P2}, F1: {F1:P2}, Precision: {Precision:P2}, Recall: {Recall:P2}",
                    metrics.Accuracy, metrics.AreaUnderRocCurve, metrics.F1Score,
                    metrics.PositivePrecision, metrics.PositiveRecall);

                // Persist metrics next to the model so thesis/results can cite a reproducible artifact
                SaveMetrics(new ModelTrainingMetrics
                {
                    EvaluatedAtUtc = DateTime.UtcNow,
                    DatasetPath = Path.GetFullPath(dataPath),
                    ModelPath = Path.GetFullPath(_modelPath),
                    TrainTestSplit = new TrainTestSplitInfo
                    {
                        TestFraction = 0.2,
                        Seed = 0,
                        Note = "MLContext(seed:0) and TrainTestSplit(seed:0); Duration excluded (UCI leakage)"
                    },
                    Accuracy = metrics.Accuracy,
                    AreaUnderRocCurve = metrics.AreaUnderRocCurve,
                    F1Score = metrics.F1Score,
                    PositivePrecision = metrics.PositivePrecision,
                    PositiveRecall = metrics.PositiveRecall,
                    NegativePrecision = metrics.NegativePrecision,
                    NegativeRecall = metrics.NegativeRecall,
                    LogLoss = metrics.LogLoss
                });

                // Create prediction engine
                lock (_sync)
                {
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<BankMarketingData, BankMarketingPrediction>(_trainedModel);
                }

                // Save model
                SaveModel(_modelPath);

                _logger.LogInformation("Model saved successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during model training");
                return false;
            }
        }

        /// <summary>
        /// Evaluate model on test data
        /// Returns accuracy metric
        /// </summary>
        public float EvaluateModel(string testDataPath)
        {
            try
            {
                _logger.LogInformation($"Evaluating model with test data from {testDataPath}");
                
                // The model is loaded lazily on the first prediction; evaluation may come first
                if (_trainedModel == null) LoadModel(_modelPath);
                var model = _trainedModel
                    ?? throw new InvalidOperationException("Cannot evaluate: model not trained or loaded");

                var testData = LoadData(testDataPath);
                var predictions = model.Transform(testData);
                
                var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");
                
                _logger.LogInformation($"Model Evaluation Metrics:");
                _logger.LogInformation($"  Accuracy: {metrics.Accuracy:P2}");
                _logger.LogInformation($"  AUC: {metrics.AreaUnderRocCurve:P2}");
                _logger.LogInformation($"  F1 Score: {metrics.F1Score:P2}");
                
                return (float)metrics.Accuracy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating model");
                return 0f;
            }
        }

        /// <summary>
        /// Make prediction for a single customer
        /// </summary>
        public async Task<BankMarketingPrediction> PredictAsync(BankMarketingData data)
        {
            try
            {
                BankMarketingPrediction prediction;
                lock (_sync)
                {
                    if (_predictionEngine == null)
                    {
                        LoadModel(_modelPath);
                    }

                    if (_predictionEngine == null)
                    {
                        throw new InvalidOperationException("Prediction engine is unavailable after model load.");
                    }

                    prediction = _predictionEngine.Predict(data);
                }

                _logger.LogDebug("Prediction: {Prediction}, Probability: {Probability:P2}", prediction.Prediction, prediction.Probability);

                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making prediction");
                throw;
            }
        }

        /// <summary>
        /// Save trained model to file
        /// </summary>
        public void SaveModel(string path)
        {
            try
            {
                var directoryName = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(directoryName))
                {
                    throw new InvalidOperationException($"Invalid directory path for model save: '{path}'");
                }

                Directory.CreateDirectory(directoryName);

                if (_trainedModel == null)
                {
                    throw new InvalidOperationException("Cannot save model: model not trained or loaded");
                }

                _mlContext.Model.Save(_trainedModel, null, path);
                _logger.LogInformation($"Model saved to {path}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving model");
                throw;
            }
        }

        /// <summary>
        /// Writes held-out evaluation metrics beside the model zip for thesis citation.
        /// Path: Models/BankMarketingModel.metrics.json
        /// </summary>
        private void SaveMetrics(ModelTrainingMetrics metrics)
        {
            try
            {
                var directoryName = Path.GetDirectoryName(MetricsPath);
                if (!string.IsNullOrWhiteSpace(directoryName))
                    Directory.CreateDirectory(directoryName);

                var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(MetricsPath, json);
                _logger.LogInformation("Training metrics saved to {MetricsPath}", Path.GetFullPath(MetricsPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save training metrics to {MetricsPath}", MetricsPath);
                throw;
            }
        }

        private sealed class ModelTrainingMetrics
        {
            public DateTime EvaluatedAtUtc { get; set; }
            public string DatasetPath { get; set; } = "";
            public string ModelPath { get; set; } = "";
            public TrainTestSplitInfo TrainTestSplit { get; set; } = new();
            public double Accuracy { get; set; }
            public double AreaUnderRocCurve { get; set; }
            public double F1Score { get; set; }
            public double PositivePrecision { get; set; }
            public double PositiveRecall { get; set; }
            public double NegativePrecision { get; set; }
            public double NegativeRecall { get; set; }
            public double LogLoss { get; set; }
        }

        private sealed class TrainTestSplitInfo
        {
            public double TestFraction { get; set; }
            public int Seed { get; set; }
            public string Note { get; set; } = "";
        }

        /// <summary>
        /// Load trained model from file
        /// </summary>
        public void LoadModel(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"Model file not found: {path}");
                }
                
                lock (_sync)
                {
                    _trainedModel = _mlContext.Model.Load(path, out _);
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<BankMarketingData, BankMarketingPrediction>(_trainedModel);
                }
                
                _logger.LogInformation($"Model loaded from {path}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading model");
                throw;
            }
        }
    }
}
