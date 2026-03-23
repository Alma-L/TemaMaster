using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace HybridDecisionIntelligence.Application.Services
{
    /// <summary>
    /// Business rule engine for evaluating customer eligibility
    /// Returns structured results indicating if rules pass/fail
    /// </summary>
    public interface IBusinessRuleEngine
    {
        Task<BusinessRuleResult> EvaluateAsync(BankCustomer customer);
    }

    public class BusinessRuleResult
    {
        public bool IsApproved { get; set; }
        public string RiskLevel { get; set; } = "Unknown"; // Low, Medium, High
        public List<string> AppliedRules { get; set; } = new();
        public List<string> FailedRules { get; set; } = new();
    }

    public class BusinessRuleEngine : IBusinessRuleEngine
    {
        private readonly IBusinessRuleRepository _ruleRepository;
        private readonly ILogger<BusinessRuleEngine> _logger;

        public BusinessRuleEngine(
            IBusinessRuleRepository ruleRepository,
            ILogger<BusinessRuleEngine> logger)
        {
            _ruleRepository = ruleRepository;
            _logger = logger;
        }

        public async Task<BusinessRuleResult> EvaluateAsync(BankCustomer customer)
        {
            var result = new BusinessRuleResult();
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
                }
            }
            
            // Overall approval: all rules must pass
            result.IsApproved = result.FailedRules.Count == 0;
            result.RiskLevel = DetermineRiskLevel(customer, result);
            
            _logger.LogInformation($"Rule evaluation complete. Approved: {result.IsApproved}, Risk: {result.RiskLevel}");
            
            return result;
        }

        private bool EvaluateRule(BankCustomer customer, BusinessRule rule)
        {
            // Balance check
            if (customer.Balance < rule.MinBalance)
            {
                _logger.LogWarning($"Rule '{rule.Name}' failed: Balance {customer.Balance} < {rule.MinBalance}");
                return false;
            }
            
            // Age check
            if (customer.Age < rule.MinAge || customer.Age > rule.MaxAge)
            {
                _logger.LogWarning($"Rule '{rule.Name}' failed: Age {customer.Age} not in range [{rule.MinAge}, {rule.MaxAge}]");
                return false;
            }
            
            // Job check
            if (rule.AllowedJobs.Any() && !rule.AllowedJobs.Contains(customer.Job))
            {
                _logger.LogWarning($"Rule '{rule.Name}' failed: Job '{customer.Job}' not in allowed list");
                return false;
            }
            
            // Default history check
            if (customer.Default == "yes")
            {
                _logger.LogWarning($"Rule '{rule.Name}' failed: Customer has default history");
                return false;
            }
            
            _logger.LogInformation($"Rule '{rule.Name}' passed for customer {customer.Id}");
            return true;
        }

        private string DetermineRiskLevel(BankCustomer customer, BusinessRuleResult result)
        {
            // Determine risk based on customer profile
            int riskScore = 0;
            
            // Balance score
            if (customer.Balance < 0) riskScore += 3;
            else if (customer.Balance < 5000) riskScore += 2;
            else if (customer.Balance < 50000) riskScore += 1;
            
            // Age score
            if (customer.Age < 25) riskScore += 2;
            else if (customer.Age > 65) riskScore += 1;
            
            // Loan/Housing score
            if (customer.Loan == "yes") riskScore += 1;
            
            // Failed rules penalty
            riskScore += result.FailedRules.Count * 2;
            
            return riskScore switch
            {
                <= 2 => "Low",
                <= 5 => "Medium",
                _ => "High"
            };
        }
    }
}
