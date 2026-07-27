using HybridDecisionIntelligence.Application.Repositories;
using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HybridDecisionIntelligence.Infrastructure.Repositories
{
    public class DecisionRepository : IDecisionRepository
    {
        private readonly HybridDecisionContext _context;
        private readonly ILogger<DecisionRepository> _logger;

        public DecisionRepository(HybridDecisionContext context, ILogger<DecisionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HybridDecision> GetDecisionByIdAsync(int id)
        {
            var decision = await _context.HybridDecisions.FindAsync(id);
            return decision ?? throw new KeyNotFoundException($"Decision with ID {id} not found");
        }

        public async Task<List<HybridDecision>> GetCustomerDecisionsAsync(int customerId)
        {
            return await _context.HybridDecisions
                .Where(d => d.CustomerId == customerId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<HybridDecision>> GetDecisionsAsync(int pageNumber, int pageSize)
        {
            return await _context.HybridDecisions
                .OrderByDescending(d => d.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task SaveDecisionAsync(HybridDecision decision)
        {
            _context.HybridDecisions.Add(decision);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Decision saved for customer {decision.CustomerId}");
        }
    }

    public class BusinessRuleRepository : IBusinessRuleRepository
    {
        private readonly HybridDecisionContext _context;
        private readonly ILogger<BusinessRuleRepository> _logger;

        public BusinessRuleRepository(HybridDecisionContext context, ILogger<BusinessRuleRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<BusinessRule>> GetActiveRulesAsync()
        {
            return await _context.BusinessRules
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task<BusinessRule> GetRuleByIdAsync(int id)
        {
            var rule = await _context.BusinessRules.FindAsync(id);
            return rule ?? throw new KeyNotFoundException($"Business rule with ID {id} not found");
        }

        public async Task SaveRuleAsync(BusinessRule rule)
        {
            _context.BusinessRules.Add(rule);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Business rule saved: {rule.Name}");
        }

        public async Task UpdateRuleAsync(BusinessRule rule)
        {
            _context.BusinessRules.Update(rule);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Business rule updated: {rule.Name}");
        }

        public async Task DeleteRuleAsync(int id)
        {
            var rule = await _context.BusinessRules.FindAsync(id);
            if (rule != null)
            {
                _context.BusinessRules.Remove(rule);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Business rule deleted: {rule.Name}");
            }
        }
    }

    public class BankCustomerRepository : IBankCustomerRepository
    {
        private readonly HybridDecisionContext _context;
        private readonly ILogger<BankCustomerRepository> _logger;

        public BankCustomerRepository(HybridDecisionContext context, ILogger<BankCustomerRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BankCustomer?> FindCustomerByIdAsync(int id)
        {
            return await _context.BankCustomers.FindAsync(id);
        }

        public async Task<BankCustomer> GetCustomerByIdAsync(int id)
        {
            var customer = await _context.BankCustomers.FindAsync(id);
            return customer ?? throw new KeyNotFoundException($"Customer with ID {id} not found");
        }

        public async Task<List<BankCustomer>> GetAllCustomersAsync()
        {
            return await _context.BankCustomers.ToListAsync();
        }

        public async Task SaveCustomerAsync(BankCustomer customer)
        {
            _context.BankCustomers.Add(customer);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Customer saved: {customer.Id}");
        }

        public async Task UpdateCustomerAsync(BankCustomer customer)
        {
            customer.UpdatedAt = DateTime.UtcNow;
            _context.BankCustomers.Update(customer);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Customer updated: {customer.Id}");
        }

        public async Task SaveOrUpdateCustomerAsync(BankCustomer customer)
        {
            var existing = await _context.BankCustomers.FindAsync(customer.Id);
            if (existing == null)
            {
                _context.BankCustomers.Add(customer);
            }
            else
            {
                customer.UpdatedAt = DateTime.UtcNow;
                _context.Entry(existing).CurrentValues.SetValues(customer);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Customer profile saved for {customer.Id}");
        }
    }

    public class MLPredictionRepository : IMLPredictionRepository
    {
        private readonly HybridDecisionContext _context;
        private readonly ILogger<MLPredictionRepository> _logger;

        public MLPredictionRepository(HybridDecisionContext context, ILogger<MLPredictionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MLPredictionResult> GetPredictionByIdAsync(int id)
        {
            var prediction = await _context.MLPredictionResults.FindAsync(id);
            return prediction ?? throw new KeyNotFoundException($"Prediction with ID {id} not found");
        }

        public async Task<List<MLPredictionResult>> GetCustomerPredictionsAsync(int customerId)
        {
            return await _context.MLPredictionResults
                .Where(p => p.CustomerId == customerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task SavePredictionAsync(MLPredictionResult prediction)
        {
            _context.MLPredictionResults.Add(prediction);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Prediction saved for customer {prediction.CustomerId}");
        }
    }
}
