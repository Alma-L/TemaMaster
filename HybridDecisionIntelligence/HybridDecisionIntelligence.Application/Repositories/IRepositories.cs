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
        Task<List<HybridDecision>> GetDecisionsAsync(int pageNumber, int pageSize);
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
        Task<BankCustomer> GetCustomerByIdAsync(int id);
        Task<List<BankCustomer>> GetAllCustomersAsync();
        Task SaveCustomerAsync(BankCustomer customer);
        Task UpdateCustomerAsync(BankCustomer customer);
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
}
