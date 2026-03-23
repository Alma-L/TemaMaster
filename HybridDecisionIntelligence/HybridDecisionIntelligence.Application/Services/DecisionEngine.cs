using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace HybridDecisionIntelligence.Application.Services
{
    /// <summary>
    /// Core hybrid decision engine that combines ML predictions with business rules
    /// Implements explainable AI through detailed audit trails
    /// </summary>
    public interface IDecisionEngine
    {
        Task<HybridDecision> MakeDecisionAsync(BankCustomer customer, MLPredictionResult mlResult);
        Task<List<HybridDecision>> GetCustomerDecisionsAsync(int customerId);
        Task SaveDecisionAsync(HybridDecision decision);
    }

    public class DecisionEngine : IDecisionEngine
    {
        private readonly IBusinessRuleEngine _ruleEngine;
        private readonly IDecisionRepository _repository;
        private readonly ILogger<DecisionEngine> _logger;

        public DecisionEngine(
            IBusinessRuleEngine ruleEngine,
            IDecisionRepository repository,
            ILogger<DecisionEngine> logger)
        {
            _ruleEngine = ruleEngine;
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Makes a hybrid decision by combining ML prediction with business rule evaluation
        /// Returns a detailed audit trail explaining any overrides
        /// </summary>
        public async Task<HybridDecision> MakeDecisionAsync(BankCustomer customer, MLPredictionResult mlResult)
        {
            _logger.LogInformation($"Starting hybrid decision for customer {customer.Id}");
            
            var auditTrail = new List<string>();
            var appliedRules = new List<string>();
            
            // Step 1: ML Prediction
            bool mlDecision = mlResult.PredictedLabel;
            float mlConfidence = mlResult.Probability;
            auditTrail.Add($"ML Prediction: {(mlDecision ? "APPROVE" : "REJECT")} (Confidence: {mlConfidence:P2})");
            
            // Step 2: Apply Business Rules
            var ruleResult = await _ruleEngine.EvaluateAsync(customer);
            auditTrail.Add($"Business Rules Evaluation: {(ruleResult.IsApproved ? "PASS" : "FAIL")}");
            
            // Step 3: Combine Results (Hybrid Logic)
            bool finalDecision = mlDecision && ruleResult.IsApproved;
            var wasOverridden = mlDecision != finalDecision;
            
            if (wasOverridden)
            {
                auditTrail.Add($"OVERRIDE APPLIED: ML predicted {(mlDecision ? "APPROVE" : "REJECT")}, but rules require {(finalDecision ? "APPROVE" : "REJECT")}");
                auditTrail.Add($"Override Reason: {string.Join(", ", ruleResult.FailedRules)}");
            }
            else
            {
                auditTrail.Add("ML prediction and business rules are aligned");
            }
            
            // Step 4: Calculate Interest Rate
            var interestRate = CalculateInterestRate(customer, mlConfidence, ruleResult.RiskLevel);
            auditTrail.Add($"Interest Rate Calculated: {interestRate:P2}");
            
            // Step 5: Create Decision Record with XAI Trail
            var decision = new HybridDecision
            {
                CustomerId = customer.Id,
                MLPredictionResultId = mlResult.Id,
                MLPredicted = mlDecision,
                MLConfidence = mlConfidence,
                FinalDecision = finalDecision,
                WasOverridden = wasOverridden,
                OverrideReason = wasOverridden ? string.Join("; ", ruleResult.FailedRules) : string.Empty,
                ApprovedInterestRate = interestRate,
                AuditTrail = string.Join(" | ", auditTrail),
                RulesApplied = string.Join(", ", ruleResult.AppliedRules),
                CreatedAt = DateTime.UtcNow
            };
            
            _logger.LogInformation($"Decision made for customer {customer.Id}: {decision.FinalDecision}, Override: {decision.WasOverridden}");
            
            return decision;
        }

        public async Task<List<HybridDecision>> GetCustomerDecisionsAsync(int customerId)
        {
            return await _repository.GetCustomerDecisionsAsync(customerId);
        }

        public async Task SaveDecisionAsync(HybridDecision decision)
        {
            await _repository.SaveDecisionAsync(decision);
        }

        private decimal CalculateInterestRate(BankCustomer customer, float mlConfidence, string riskLevel)
        {
            // Base rate: 4%
            decimal baseRate = 0.04m;
            
            // Confidence adjustment: Higher confidence = lower rate
            decimal confidenceAdjustment = (1 - (decimal)mlConfidence) * 0.02m;
            
            // Risk level adjustment
            decimal riskAdjustment = riskLevel switch
            {
                "Low" => 0.0m,
                "Medium" => 0.015m,
                "High" => 0.03m,
                _ => 0.02m
            };
            
            // Balance adjustment: Higher balance = lower rate
            decimal balanceAdjustment = customer.Balance switch
            {
                > 50000 => -0.005m,
                > 10000 => -0.002m,
                _ => 0.0m
            };
            
            // Age adjustment: Mature customers get slightly better rates
            decimal ageAdjustment = customer.Age switch
            {
                > 55 => -0.005m,
                > 45 => -0.002m,
                _ => 0.0m
            };
            
            var finalRate = baseRate + confidenceAdjustment + riskAdjustment + balanceAdjustment + ageAdjustment;
            
            // Clamp rate between 2% and 12%
            return Math.Max(0.02m, Math.Min(0.12m, finalRate));
        }
    }
}
