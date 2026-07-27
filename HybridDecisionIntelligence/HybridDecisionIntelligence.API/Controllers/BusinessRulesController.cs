using HybridDecisionIntelligence.Application.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HybridDecisionIntelligence.API.Controllers
{
    /// <summary>
    /// Read-only endpoint exposing the active business rules the decision
    /// engine evaluates, so the frontend can explain the bank's real
    /// approval criteria instead of a hardcoded description.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class BusinessRulesController : ControllerBase
    {
        private readonly IBusinessRuleRepository _ruleRepository;

        public BusinessRulesController(IBusinessRuleRepository ruleRepository)
        {
            _ruleRepository = ruleRepository;
        }

        /// <summary>
        /// Get all currently active business rules
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetActiveRules()
        {
            var rules = await _ruleRepository.GetActiveRulesAsync();
            return Ok(rules);
        }
    }
}
