using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace HybridDecisionIntelligence.Application.Services
{
    public class HybridDecisionObservation
    {
        public int CustomerId { get; set; }
        public bool MLPredicted { get; set; }
        public float MLConfidence { get; set; }
        public bool BusinessRulesApproved { get; set; }
        public OverrideDecision OverrideDecision { get; set; }
        public string RuleReason { get; set; } = string.Empty;
        public decimal ComputedInterestRate { get; set; }
        public string ExplainableDetails { get; set; } = string.Empty;
        public bool FinalDecision { get; set; }
        public string FinalDecisionLabel => FinalDecision ? "Approved" : "Rejected";
    }

    public interface IHybridDecisionService
    {
        Task<HybridDecisionObservation> AssessDecisionAsync(BankCustomer customer, MLPredictionResult mlResult);
    }

    public class HybridDecisionService : IHybridDecisionService
    {
        private readonly IBusinessRuleService _ruleService;
        private readonly ILogger<HybridDecisionService> _logger;

        public HybridDecisionService(IBusinessRuleService ruleService, ILogger<HybridDecisionService> logger)
        {
            _ruleService = ruleService;
            _logger = logger;
        }

        public async Task<HybridDecisionObservation> AssessDecisionAsync(BankCustomer customer, MLPredictionResult mlResult)
        {
            _logger.LogInformation($"HybridDecisionService: Beginning TAO for customer {customer.Id}");

            // Thought
            _logger.LogInformation($"Thought: ML predicted={mlResult.PredictedLabel}, probability={mlResult.Probability:P2}");

            // Action
            var ruleResult = await _ruleService.EvaluateAsync(customer, mlResult);

            // Observation
            var finalDecision = mlResult.PredictedLabel && ruleResult.IsApproved && ruleResult.OverrideDecision == OverrideDecision.None;
            if (ruleResult.OverrideDecision == OverrideDecision.Rejected) finalDecision = false;
            if (ruleResult.OverrideDecision == OverrideDecision.HighRisk) finalDecision = false;

            var computedInterestRate = ComputeInterestRate(customer, mlResult.Probability, ruleResult.RiskLevel);

            var observation = new HybridDecisionObservation
            {
                CustomerId = customer.Id,
                MLPredicted = mlResult.PredictedLabel,
                MLConfidence = mlResult.Probability,
                BusinessRulesApproved = ruleResult.IsApproved,
                OverrideDecision = ruleResult.OverrideDecision,
                RuleReason = ruleResult.OverrideReason,
                ComputedInterestRate = computedInterestRate,
                ExplainableDetails = $"Thought ML Probability={mlResult.Probability:P2}; " +
                                     $"Action rulesApproved={ruleResult.IsApproved}; " +
                                     $"Action override={ruleResult.OverrideDecision}; " +
                                     $"Observation risk={ruleResult.RiskLevel}; " +
                                     $"Observation interest={computedInterestRate:P2}; " +
                                     $"Observation finalDecision={(finalDecision ? "Approved" : "Rejected")};",
                FinalDecision = finalDecision
            };

            _logger.LogInformation($"Observation complete for customer {customer.Id}: {observation.ExplainableDetails}");

            return observation;
        }

        private decimal ComputeInterestRate(BankCustomer customer, float mlProbability, string riskLevel)
        {
            decimal baseRate = 0.03m;
            decimal confidenceAdjustment = (1m - (decimal)mlProbability) * 0.02m;

            decimal riskAdjustment = riskLevel switch
            {
                "Low" => -0.005m,
                "Medium" => 0.01m,
                "High" => 0.02m,
                _ => 0.0m
            };

            decimal rate = baseRate + confidenceAdjustment + riskAdjustment;

            if (customer.Housing?.ToLower() == "yes" && customer.Balance < 500)
            {
                rate += 0.03m;
            }

            return Math.Max(0.02m, Math.Min(0.15m, rate));
        }
    }
}
