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

    /// <summary>
    /// Runs every decision as an explicit Thought–Action–Observation cycle:
    ///   Thought     – the ML model's hypothesis (predicted label + probability)
    ///   Action      – deterministic business rules, risk level, offer pricing on the
    ///                 macro-economic reference rate, and the interest-rate policy rule
    ///   Observation – the final decision, any override and its reason
    /// Each audit-trail entry is tagged with its phase ([Thought]/[Action]/[Observation]).
    /// </summary>
    public class DecisionEngine : IDecisionEngine
    {
        private readonly IBusinessRuleEngine _ruleEngine;
        private readonly IDecisionRepository _repository;
        private readonly ILogger<DecisionEngine> _logger;
        private readonly DecisionPolicyOptions _policy;

        public DecisionEngine(
            IBusinessRuleEngine ruleEngine,
            IDecisionRepository repository,
            ILogger<DecisionEngine> logger,
            DecisionPolicyOptions? policy = null)
        {
            _ruleEngine = ruleEngine;
            _repository = repository;
            _logger = logger;
            _policy = policy ?? new DecisionPolicyOptions();
        }

        /// <summary>
        /// Makes a hybrid decision by combining ML prediction with business rule evaluation
        /// Returns a detailed audit trail explaining any overrides
        /// </summary>
        public async Task<HybridDecision> MakeDecisionAsync(BankCustomer customer, MLPredictionResult mlResult)
        {
            _logger.LogInformation($"Starting hybrid decision for customer {customer.Id}");

            var auditTrail = new List<string>();
            void Thought(string entry) => auditTrail.Add($"[Thought] {entry}");
            void Action(string entry) => auditTrail.Add($"[Action] {entry}");
            void Observation(string entry) => auditTrail.Add($"[Observation] {entry}");

            // THOUGHT: the model's hypothesis
            bool mlDecision = mlResult.PredictedLabel;
            float mlConfidence = mlResult.Probability;
            Thought($"ML Prediction: {(mlDecision ? "APPROVE" : "REJECT")} (Confidence: {mlConfidence:P2})");

            // ACTION 1: deterministic business rules (stored in the database)
            var ruleResult = await _ruleEngine.EvaluateAsync(customer);
            Action($"Business Rules Evaluation: {(ruleResult.IsApproved ? "PASS" : "FAIL")} (Risk: {ruleResult.RiskLevel})");

            // ACTION 2: price the offer on the macro-economic reference rate
            var interestRate = CalculateInterestRate(customer, mlConfidence, ruleResult.RiskLevel);
            Action($"Interest Rate Calculated: {interestRate:P2} (reference {_policy.ReferenceRate:P2} + spread {interestRate - _policy.ReferenceRate:P2})");

            // ACTION 3: dynamic rule - the offer must stay inside the rules' rate corridor
            var ratePolicyFailure = EvaluateRatePolicy(interestRate, ruleResult);
            Action(ratePolicyFailure == null
                ? $"Interest Rate Policy: PASS ({FormatCorridor(ruleResult)})"
                : $"Interest Rate Policy: FAIL - {ratePolicyFailure}");

            // OBSERVATION: combine and record
            var failures = new List<string>(ruleResult.FailedRules);
            if (ratePolicyFailure != null) failures.Add("Interest Rate Policy");

            bool finalDecision = mlDecision && failures.Count == 0;
            var wasOverridden = mlDecision != finalDecision;

            if (wasOverridden)
            {
                Observation("OVERRIDE APPLIED: ML predicted APPROVE, but rules require REJECT");
                Observation($"Override Reason: {string.Join(", ", failures)}");
            }
            else
            {
                Observation("ML prediction and business rules are aligned");
            }
            Observation($"Final Decision: {(finalDecision ? "APPROVED" : "REJECTED")}");

            var decision = new HybridDecision
            {
                CustomerId = customer.Id,
                MLPredictionResultId = mlResult.Id,
                MLPredicted = mlDecision,
                MLConfidence = mlConfidence,
                FinalDecision = finalDecision,
                WasOverridden = wasOverridden,
                OverrideReason = wasOverridden ? string.Join("; ", failures) : string.Empty,
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

        /// <summary>
        /// Returns why the offered rate breaks the rules' corridor, or null if it is inside.
        /// Too high: the offer is not competitive / too costly for the customer.
        /// Too low: the offer does not cover the bank's funding cost.
        /// </summary>
        private static string? EvaluateRatePolicy(decimal rate, BusinessRuleResult rules)
        {
            if (rate > rules.MaxInterestRate)
                return $"offered rate {rate:P2} is above the policy maximum {rules.MaxInterestRate:P2}";
            if (rate < rules.MinInterestRate)
                return $"offered rate {rate:P2} is below the policy minimum {rules.MinInterestRate:P2}";
            return null;
        }

        private static string FormatCorridor(BusinessRuleResult rules) =>
            rules.MaxInterestRate == decimal.MaxValue
                ? "no rate corridor configured"
                : $"within {rules.MinInterestRate:P2}–{rules.MaxInterestRate:P2}";

        /// <summary>
        /// Offered rate = reference rate + spread. The spread grows with model
        /// uncertainty and risk and shrinks for high balances and mature customers.
        /// It is not clamped: an offer outside the policy corridor is rejected by the
        /// interest-rate policy rule instead of being silently adjusted.
        /// </summary>
        private decimal CalculateInterestRate(BankCustomer customer, float mlConfidence, string riskLevel)
        {
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

            // 4 decimals = the precision the rate is stored with (decimal(5,4))
            return Math.Round(
                _policy.ReferenceRate + confidenceAdjustment + riskAdjustment + balanceAdjustment + ageAdjustment, 4);
        }
    }
}
