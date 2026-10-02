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
        /// <summary>
        /// Customer profile for the offer: P1 (base), P2 (intermediate), P3 (high incentive): how far the customer is from the
        /// bank's core segment. It sizes the deposit-rate incentive; it is not a credit-risk score.
        /// </summary>
        public string ProfileLevel { get; set; } = "Unknown";
        public List<string> AppliedRules { get; set; } = new();
        public List<string> FailedRules { get; set; } = new();

        /// <summary>
        /// Interest-rate corridor allowed by the active rules (strictest bounds across
        /// all of them). Without active rules the corridor is unbounded.
        /// </summary>
        public decimal MinInterestRate { get; set; } = 0m;
        public decimal MaxInterestRate { get; set; } = decimal.MaxValue;
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

            // Interest-rate corridor: the strictest bounds of the active rules
            if (rules.Count > 0)
            {
                result.MinInterestRate = rules.Max(r => r.MinInterestRate);
                result.MaxInterestRate = rules.Min(r => r.MaxInterestRate);
            }
            result.ProfileLevel = DetermineProfileLevel(customer, result);
            
            _logger.LogInformation($"Rule evaluation complete. Approved: {result.IsApproved}, Profile: {result.ProfileLevel}");
            
            return result;
        }

        /// <summary>
        /// A rule checks only the criteria it configures, so a customer who fails one
        /// condition (e.g. default history) fails exactly the rule that owns it.
        /// </summary>
        private bool EvaluateRule(BankCustomer customer, BusinessRule rule)
        {
            // Balance check
            if (rule.MinBalance > 0 && customer.Balance < rule.MinBalance)
            {
                _logger.LogWarning($"Rule '{rule.Name}' failed: Balance {customer.Balance} < {rule.MinBalance}");
                return false;
            }

            // Age check (MaxAge 0 = no upper bound)
            if ((rule.MinAge > 0 && customer.Age < rule.MinAge) ||
                (rule.MaxAge > 0 && customer.Age > rule.MaxAge))
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
            if (rule.RequireNoDefault && customer.Default == "yes")
            {
                _logger.LogWarning($"Rule '{rule.Name}' failed: Customer has default history");
                return false;
            }
            
            _logger.LogInformation($"Rule '{rule.Name}' passed for customer {customer.Id}");
            return true;
        }

        private string DetermineProfileLevel(BankCustomer customer, BusinessRuleResult result)
        {
            // Distance from the core segment, scored on the customer profile
            int profileScore = 0;
            
            // Balance score
            if (customer.Balance < 0) profileScore += 3;
            else if (customer.Balance < 5000) profileScore += 2;
            else if (customer.Balance < 50000) profileScore += 1;
            
            // Age score
            if (customer.Age < 25) profileScore += 2;
            else if (customer.Age > 65) profileScore += 1;
            
            // Loan/Housing score
            if (customer.Loan == "yes") profileScore += 1;
            
            // Failed rules penalty
            profileScore += result.FailedRules.Count * 2;
            
            return profileScore switch
            {
                <= 2 => "P1",
                <= 5 => "P2",
                _ => "P3"
            };
        }
    }
}
