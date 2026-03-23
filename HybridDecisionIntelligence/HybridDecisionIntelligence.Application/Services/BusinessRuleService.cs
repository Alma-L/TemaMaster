using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace HybridDecisionIntelligence.Application.Services
{
    public enum OverrideDecision
    {
        None,
        Rejected,
        HighRisk
    }

    public class BusinessRuleServiceResult
    {
        public bool IsApproved { get; set; }
        public string RiskLevel { get; set; } = "Unknown";
        public OverrideDecision OverrideDecision { get; set; } = OverrideDecision.None;
        public string OverrideReason { get; set; } = string.Empty;
        public List<string> AppliedRules { get; set; } = new();
        public List<string> FailedRules { get; set; } = new();
        public string ExplainableDetails { get; set; } = string.Empty;

        public bool IsRejectedByInterestRate { get; set; }
    }

    public interface IBusinessRuleService
    {
        Task<BusinessRuleServiceResult> EvaluateAsync(BankCustomer customer, MLPredictionResult mlResult);
    }

    public class BusinessRuleService : IBusinessRuleService
    {
        private readonly IBusinessRuleRepository _ruleRepository;
        private readonly ILogger<BusinessRuleService> _logger;

        public BusinessRuleService(IBusinessRuleRepository ruleRepository, ILogger<BusinessRuleService> logger)
        {
            _ruleRepository = ruleRepository;
            _logger = logger;
        }

        public async Task<BusinessRuleServiceResult> EvaluateAsync(BankCustomer customer, MLPredictionResult mlResult)
        {
            var result = new BusinessRuleServiceResult();

            // Thought (XAI): log ML probability
            result.ExplainableDetails += $"Thought: ML probability={mlResult.Probability:P2}; ";
            _logger.LogInformation($"Thought: ML probability={mlResult.Probability:P2}");

            // Action: evaluate all stored business rules
            var rules = await _ruleRepository.GetActiveRulesAsync();
            _logger.LogInformation($"Evaluating {rules.Count} business rules for customer {customer.Id}");

            foreach (var rule in rules)
            {
                if (EvaluateRule(customer, rule))
                {
                    result.AppliedRules.Add(rule.Name);
                }
                else
                {
                    result.FailedRules.Add(rule.Name);
                    result.ExplainableDetails += $"Action: Rule '{rule.Name}' failed; ";
                }
            }

            result.IsApproved = result.FailedRules.Count == 0;
            result.RiskLevel = DetermineRisk(customer, result);

            // Action: dynamic interest rate override
            decimal expectedInterest = CalculateInterestRate(customer, mlResult.Probability, result.RiskLevel);
            result.ExplainableDetails += $"Action: Dynamic interest estimation={expectedInterest:P2}; ";

            if (expectedInterest < 0.025m)
            {
                result.OverrideDecision = OverrideDecision.Rejected;
                result.OverrideReason += "Dynamic Interest Rate < 2.5% (auto reject). ";
                result.IsRejectedByInterestRate = true;
                result.IsApproved = false;
                result.ExplainableDetails += "Action: Override decision=Rejected by DynamicInterest rule; ";
            }

            // Action: debt status rule
            if (customer.Housing?.ToLower() == "yes" && customer.Balance < 500)
            {
                result.OverrideDecision = OverrideDecision.HighRisk;
                result.OverrideReason += "Debt status: housing=true and balance < 500 => HighRisk. ";
                result.IsApproved = false;
                result.ExplainableDetails += "Action: Override decision=HighRisk by DebtStatus rule; ";
            }

            // Observation: summary for XAI dashboard
            result.ExplainableDetails += $"Observation: FinalIsApproved={result.IsApproved}; RiskLevel={result.RiskLevel}; Overrides={result.OverrideDecision}; " ;

            return result;
        }

        private bool EvaluateRule(BankCustomer customer, BusinessRule rule)
        {
            if (customer.Balance < rule.MinBalance)
            {
                _logger.LogWarning($"Rule '{rule.Name}' failed: Balance {customer.Balance} < {rule.MinBalance}");
                return false;
            }

            if (customer.Age < rule.MinAge || customer.Age > rule.MaxAge)
            {
                _logger.LogWarning($"Rule '{rule.Name}' failed: Age {customer.Age} not in range [{rule.MinAge}, {rule.MaxAge}]");
                return false;
            }

            if (rule.AllowedJobs != null && rule.AllowedJobs.Any() && !rule.AllowedJobs.Contains(customer.Job))
            {
                _logger.LogWarning($"Rule '{rule.Name}' failed: Job '{customer.Job}' not allowed");
                return false;
            }

            if (customer.Default?.ToLower() == "yes")
            {
                _logger.LogWarning($"Rule '{rule.Name}' failed: default history is yes");
                return false;
            }

            _logger.LogInformation($"Rule '{rule.Name}' passed");
            return true;
        }

        private string DetermineRisk(BankCustomer customer, BusinessRuleServiceResult result)
        {
            int risk = 0;
            if (customer.Balance < 0) risk += 3;
            else if (customer.Balance < 5000) risk += 2;
            else if (customer.Balance < 50000) risk += 1;

            if (customer.Age < 25) risk += 2;
            else if (customer.Age > 65) risk += 1;

            if (customer.Loan?.ToLower() == "yes") risk += 1;
            risk += result.FailedRules.Count * 2;

            return risk switch
            {
                <= 2 => "Low",
                <= 5 => "Medium",
                _ => "High"
            };
        }

        private decimal CalculateInterestRate(BankCustomer customer, float mlProbability, string riskLevel)
        {
            decimal baseRate = 0.04m;
            decimal confidenceFactor = (1 - (decimal)mlProbability) * 0.01m;
            decimal riskFactor = riskLevel switch
            {
                "Low" => -0.005m,
                "Medium" => 0.005m,
                "High" => 0.015m,
                _ => 0.0m
            };

            var rate = baseRate + confidenceFactor + riskFactor;
            return Math.Max(0.02m, Math.Min(0.15m, rate));
        }
    }
} 