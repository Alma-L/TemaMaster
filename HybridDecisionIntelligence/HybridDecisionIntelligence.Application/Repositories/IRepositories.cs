using HybridDecisionIntelligence.Domain.Entities;

namespace HybridDecisionIntelligence.Application.Repositories
{
    /// <summary>
    /// Repository interface for hybrid decisions
    /// </summary>
    public interface IDecisionRepository
    {
        Task<HybridDecision> GetDecisionByIdAsync(int id);
        Task<List<HybridDecision>> GetCustomerDecisionsAsync(int customerId);
        Task SaveDecisionAsync(HybridDecision decision);
        Task<List<HybridDecision>> GetDecisionsAsync(int pageNumber, int pageSize, DecisionFilter? filter = null);
        Task<DecisionStats> GetDecisionStatsAsync(DecisionFilter? filter = null);
    }

    /// <summary>
    /// Totals over all stored decisions matching a filter, for dashboard rates
    /// </summary>
    public class DecisionStats
    {
        public int Total { get; set; }
        public int MLApproved { get; set; }
        public int Approved { get; set; }
        public int Overridden { get; set; }
        public double AverageProbability { get; set; }
        public decimal AverageInterestRate { get; set; }

        /// <summary>How many overridden decisions each rule failed (one decision can fail several)</summary>
        public List<RuleOverrideCount> OverridesByRule { get; set; } = new();
    }

    public class RuleOverrideCount
    {
        public string Rule { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// Optional filters for the decision register. Null fields are not applied.
    /// Probability and interest rate are fractions (0.25 = 25%).
    /// </summary>
    public class DecisionFilter
    {
        public int? CustomerId { get; set; }
        public bool? FinalDecision { get; set; }
        public bool? WasOverridden { get; set; }
        public float? MinProbability { get; set; }
        public float? MaxProbability { get; set; }
        public decimal? MinInterestRate { get; set; }
        public decimal? MaxInterestRate { get; set; }
    }

    /// <summary>
    /// Repository interface for business rules
    /// </summary>
    public interface IBusinessRuleRepository
    {
        Task<List<BusinessRule>> GetActiveRulesAsync();
        Task<BusinessRule> GetRuleByIdAsync(int id);
        Task SaveRuleAsync(BusinessRule rule);
        Task UpdateRuleAsync(BusinessRule rule);
        Task DeleteRuleAsync(int id);
    }

    /// <summary>
    /// Repository interface for bank customers
    /// </summary>
    public interface IBankCustomerRepository
    {
        Task<BankCustomer?> FindCustomerByIdAsync(int id);
        Task<BankCustomer> GetCustomerByIdAsync(int id);
        Task<List<BankCustomer>> GetAllCustomersAsync();
        Task SaveCustomerAsync(BankCustomer customer);
        Task UpdateCustomerAsync(BankCustomer customer);

        /// <summary>
        /// Insert the customer if this Id hasn't been seen before, otherwise
        /// overwrite their stored profile with the latest submitted data.
        /// </summary>
        Task SaveOrUpdateCustomerAsync(BankCustomer customer);
    }

    /// <summary>
    /// Repository interface for ML predictions
    /// </summary>
    public interface IMLPredictionRepository
    {
        Task<MLPredictionResult> GetPredictionByIdAsync(int id);
        Task<List<MLPredictionResult>> GetCustomerPredictionsAsync(int customerId);
        Task SavePredictionAsync(MLPredictionResult prediction);
    }

    /// <summary>
    /// Runs several repository writes as one atomic database transaction, so a
    /// decision request never leaves a customer or prediction without its decision.
    /// </summary>
    public interface IUnitOfWork
    {
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken = default);
    }
}
