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
    /// Binary classification model for banking decisions
    /// </summary>
    public class MLNetModelService : IMLModelService
    {
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

                // Create prediction engine
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<BankMarketingData, BankMarketingPrediction>(_trainedModel);

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
                
                var testData = LoadData(testDataPath);
                var predictions = _trainedModel.Transform(testData);
                
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
                if (_predictionEngine == null)
                {
                    LoadModel(_modelPath);
                }

                if (_predictionEngine == null)
                {
                    throw new InvalidOperationException("Prediction engine is unavailable after model load.");
                }
                
                var prediction = _predictionEngine!.Predict(data);
                
                _logger.LogInformation($"Prediction: {prediction.Prediction}, Probability: {prediction.Probability:P2}");
                
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
                
                DataViewSchema schema;
                _trainedModel = _mlContext.Model.Load(path, out schema);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<BankMarketingData, BankMarketingPrediction>(_trainedModel);
                
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
