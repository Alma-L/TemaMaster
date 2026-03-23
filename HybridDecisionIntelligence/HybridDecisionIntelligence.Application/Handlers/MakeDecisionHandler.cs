using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Application.Requests;
using HybridDecisionIntelligence.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HybridDecisionIntelligence.Application.Handlers
{
    /// <summary>
    /// MediatR handler for the MakeDecisionRequest
    /// Orchestrates ML prediction and business rule evaluation
    /// </summary>
    public class MakeDecisionHandler : IRequestHandler<MakeDecisionRequest, MakeDecisionResponse>
    {
        private readonly IMLPredictor _mlPredictor;
        private readonly IDecisionEngine _decisionEngine;
        private readonly ILogger<MakeDecisionHandler> _logger;

        public MakeDecisionHandler(
            IMLPredictor mlPredictor,
            IDecisionEngine decisionEngine,
            ILogger<MakeDecisionHandler> logger)
        {
            _mlPredictor = mlPredictor;
            _decisionEngine = decisionEngine;
            _logger = logger;
        }

        public async Task<MakeDecisionResponse> Handle(MakeDecisionRequest request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Processing decision request for customer {request.CustomerId}");
                
                // Create customer entity from request
                var customer = new BankCustomer
                {
                    Id = request.CustomerId,
                    Age = request.Age,
                    Job = request.Job,
                    Marital = request.Marital,
                    Education = request.Education,
                    Balance = request.Balance,
                    Housing = request.Housing,
                    Loan = request.Loan,
                    Duration = request.Duration,
                    Campaign = request.Campaign,
                    Previous = request.Previous
                };
                
                // Step 1: Get ML Prediction
                _logger.LogInformation("Requesting ML prediction...");
                var mlResult = await _mlPredictor.PredictAsync(customer);
                
                // Step 2: Apply Hybrid Decision Engine
                _logger.LogInformation("Applying hybrid decision engine...");
                var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);
                
                // Step 3: Save decision to repository
                await _decisionEngine.SaveDecisionAsync(decision);
                
                // Step 4: Build response with audit trail
                var response = new MakeDecisionResponse
                {
                    CustomerId = request.CustomerId,
                    MLPrediction = decision.MLPredicted,
                    MLConfidence = decision.MLConfidence,
                    FinalDecision = decision.FinalDecision,
                    ApprovedInterestRate = decision.ApprovedInterestRate,
                    WasOverridden = decision.WasOverridden,
                    AuditTrail = decision.AuditTrail,
                    AppliedRules = decision.RulesApplied.Split(", ").ToList(),
                    DecisionTime = decision.CreatedAt
                };
                
                _logger.LogInformation($"Decision completed for customer {request.CustomerId}: {response.FinalDecision}");
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing decision request for customer {request.CustomerId}");
                throw;
            }
        }
    }
}
