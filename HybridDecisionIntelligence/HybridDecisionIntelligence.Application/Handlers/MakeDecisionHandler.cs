using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Domain.ValueObjects;
using HybridDecisionIntelligence.Application.Repositories;
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
        private readonly IBankCustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MakeDecisionHandler> _logger;

        public MakeDecisionHandler(
            IMLPredictor mlPredictor,
            IDecisionEngine decisionEngine,
            IBankCustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<MakeDecisionHandler> logger)
        {
            _mlPredictor = mlPredictor;
            _decisionEngine = decisionEngine;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
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
                    Default = request.Default,
                    Duration = request.Duration,
                    Campaign = request.Campaign,
                    Previous = request.Previous,
                    Contact = request.Contact,
                    Day = request.Day,
                    Month = request.Month,
                    PDays = request.PDays,
                    POutcome = request.POutcome
                };

                // Customer profile, ML prediction and decision are written atomically:
                // if any step fails, none of them is persisted.
                var decision = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    // Persist the customer's latest submitted profile.
                    await _customerRepository.SaveOrUpdateCustomerAsync(customer);

                    // Step 1: Get ML Prediction (saved, so it gets an Id)
                    _logger.LogInformation("Requesting ML prediction...");
                    var mlResult = await _mlPredictor.PredictAsync(customer);

                    // Step 2: Apply Hybrid Decision Engine
                    _logger.LogInformation("Applying hybrid decision engine...");
                    var hybridDecision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

                    // Freeze the exact input that was evaluated; the customer row above
                    // may be overwritten by later requests for the same customer.
                    hybridDecision.CustomerSnapshotJson = CustomerSnapshot.Serialize(customer);

                    // Step 3: Save decision to repository
                    await _decisionEngine.SaveDecisionAsync(hybridDecision);
                    return hybridDecision;
                }, cancellationToken);

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
