using HybridDecisionIntelligence.Domain.Entities;
using MediatR;

namespace HybridDecisionIntelligence.Application.Requests
{
    /// <summary>
    /// Request to retrieve a customer's decision history
    /// </summary>
    public class GetCustomerDecisionHistoryRequest : IRequest<List<HybridDecision>>
    {
        public int CustomerId { get; set; }
    }

    /// <summary>
    /// Request to train the ML model with customer data
    /// </summary>
    public class TrainMLModelRequest : IRequest<bool>
    {
        public required string DataPath { get; set; }
        public required string ModelPath { get; set; }
    }

    /// <summary>
    /// Request to update business rules
    /// </summary>
    public class UpdateBusinessRulesRequest : IRequest<bool>
    {
        public required List<BusinessRule> Rules { get; set; }
    }
}
