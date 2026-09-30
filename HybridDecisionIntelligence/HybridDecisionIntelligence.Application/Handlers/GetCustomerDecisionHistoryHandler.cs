using HybridDecisionIntelligence.Application.Repositories;
using HybridDecisionIntelligence.Application.Requests;
using HybridDecisionIntelligence.Domain.Entities;
using MediatR;

namespace HybridDecisionIntelligence.Application.Handlers
{
    /// <summary>
    /// MediatR handler for GetCustomerDecisionHistoryRequest: a customer's decisions, newest first
    /// </summary>
    public class GetCustomerDecisionHistoryHandler : IRequestHandler<GetCustomerDecisionHistoryRequest, List<HybridDecision>>
    {
        private readonly IDecisionRepository _decisionRepository;

        public GetCustomerDecisionHistoryHandler(IDecisionRepository decisionRepository)
        {
            _decisionRepository = decisionRepository;
        }

        public Task<List<HybridDecision>> Handle(GetCustomerDecisionHistoryRequest request, CancellationToken cancellationToken) =>
            _decisionRepository.GetCustomerDecisionsAsync(request.CustomerId);
    }
}
