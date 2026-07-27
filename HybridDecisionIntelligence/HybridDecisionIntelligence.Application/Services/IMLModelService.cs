using HybridDecisionIntelligence.Domain.ValueObjects;

namespace HybridDecisionIntelligence.Application.Services
{
    /// <summary>
    /// Interface for ML model operations
    /// Abstracts away ML.NET implementation details
    /// </summary>
    public interface IMLModelService
    {
        /// <summary>
        /// Train a new ML model from CSV data
        /// </summary>
        Task<bool> TrainModelAsync(string trainingDataPath);

        /// <summary>
        /// Evaluate model performance on test data
        /// </summary>
        float EvaluateModel(string testDataPath);

        /// <summary>
        /// Make a prediction using the trained model
        /// </summary>
        Task<BankMarketingPrediction> PredictAsync(BankMarketingData data);

        /// <summary>
        /// Save trained model to disk
        /// </summary>
        void SaveModel(string modelPath);

        /// <summary>
        /// Load previously trained model from disk
        /// </summary>
        void LoadModel(string modelPath);
    }
}
