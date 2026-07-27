using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Domain.ValueObjects;
using HybridDecisionIntelligence.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace HybridDecisionIntelligence.Application.Services
{
    /// <summary>
    /// Interface for ML prediction service
    /// Abstracts away ML.NET implementation details
    /// </summary>
    public interface IMLPredictor
    {
        Task<MLPredictionResult> PredictAsync(BankCustomer customer);
        Task<bool> TrainModelAsync(string dataPath);
        float EvaluateModel(string testDataPath);
    }

    /// <summary>
    /// Concrete implementation of ML predictor
    /// Uses ML.NET for binary classification
    /// </summary>
    public class MLPredictor : IMLPredictor
    {
        private readonly IMLModelService _modelService;
        private readonly IMLPredictionRepository _predictionRepository;
        private readonly ILogger<MLPredictor> _logger;

        public MLPredictor(
            IMLModelService modelService,
            IMLPredictionRepository predictionRepository,
            ILogger<MLPredictor> logger)
        {
            _modelService = modelService;
            _predictionRepository = predictionRepository;
            _logger = logger;
        }

        public async Task<MLPredictionResult> PredictAsync(BankCustomer customer)
        {
            try
            {
                _logger.LogInformation($"Making ML prediction for customer {customer.Id}");
                
                // Convert customer to ML format
                var mlData = CustomerToMLData(customer);
                
                // Get prediction from model
                var prediction = await _modelService.PredictAsync(mlData);
                
                // Create and save result
                var result = new MLPredictionResult
                {
                    CustomerId = customer.Id,
                    PredictedLabel = prediction.Prediction,
                    Score = prediction.Score,
                    Probability = prediction.Probability,
                    CreatedAt = DateTime.UtcNow
                };
                
                await _predictionRepository.SavePredictionAsync(result);
                
                _logger.LogInformation($"Prediction: {prediction.Prediction}, Confidence: {prediction.Probability:P2}");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error making prediction for customer {customer.Id}");
                throw;
            }
        }

        public async Task<bool> TrainModelAsync(string dataPath)
        {
            _logger.LogInformation($"Training ML model with data from {dataPath}");
            return await _modelService.TrainModelAsync(dataPath);
        }

        public float EvaluateModel(string testDataPath)
        {
            _logger.LogInformation($"Evaluating ML model with test data from {testDataPath}");
            return _modelService.EvaluateModel(testDataPath);
        }

        private BankMarketingData CustomerToMLData(BankCustomer customer)
        {
            return new BankMarketingData
            {
                Age = customer.Age,
                Job = customer.Job,
                Marital = customer.Marital,
                Education = customer.Education,
                Default = customer.Default,
                Balance = (float)customer.Balance,
                Housing = customer.Housing,
                Loan = customer.Loan,
                Contact = customer.Contact,
                Day = customer.Day,
                Month = customer.Month,
                Duration = customer.Duration,
                Campaign = customer.Campaign,
                PDays = customer.PDays,
                Previous = customer.Previous,
                POutcome = customer.POutcome,
                Label = customer.SubscribedToTerm
            };
        }
    }
}
