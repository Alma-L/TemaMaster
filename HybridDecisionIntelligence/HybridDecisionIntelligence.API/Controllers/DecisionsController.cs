using HybridDecisionIntelligence.Application.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HybridDecisionIntelligence.API.Controllers
{
    /// <summary>
    /// API endpoints for hybrid decision intelligence
    /// Exposes ML prediction + business rule evaluation through REST
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DecisionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<DecisionsController> _logger;

        public DecisionsController(IMediator mediator, ILogger<DecisionsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Make a hybrid decision for a customer
        /// Combines ML prediction with business rules
        /// Returns audit trail explaining the decision
        /// </summary>
        /// <param name="request">Customer information for decision</param>
        /// <returns>Decision with explainable AI audit trail</returns>
        [HttpPost("make-decision")]
        [ProducesResponseType(typeof(MakeDecisionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MakeDecision([FromBody] MakeDecisionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation($"Decision request received for customer {request.CustomerId}");
                var response = await _mediator.Send(request);
                
                // Log decision result
                _logger.LogInformation($"Decision completed - Final: {response.FinalDecision}, Override: {response.WasOverridden}");
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing decision request");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get decision history for a customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>List of previous decisions with audit trails</returns>
        [HttpGet("customer/{customerId}/history")]
        [ProducesResponseType(typeof(List<MakeDecisionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCustomerHistory(int customerId)
        {
            try
            {
                _logger.LogInformation($"Fetching decision history for customer {customerId}");
                var request = new GetCustomerDecisionHistoryRequest { CustomerId = customerId };
                var decisions = await _mediator.Send(request);
                
                if (!decisions.Any())
                {
                    return NotFound(new { message = $"No decisions found for customer {customerId}" });
                }
                
                return Ok(decisions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching history for customer {customerId}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>Service status</returns>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult Health()
        {
            return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
        }
    }
}
