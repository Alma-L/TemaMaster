using HybridDecisionIntelligence.Application.Requests;
using HybridDecisionIntelligence.Application.Repositories;
using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Domain.Entities;
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
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class DecisionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IDecisionRepository _decisionRepository;
        private readonly IBankCustomerRepository _customerRepository;
        private readonly IDecisionReportGenerator _reportGenerator;
        private readonly ILogger<DecisionsController> _logger;

        public DecisionsController(
            IMediator mediator,
            IDecisionRepository decisionRepository,
            IBankCustomerRepository customerRepository,
            IDecisionReportGenerator reportGenerator,
            ILogger<DecisionsController> logger)
        {
            _mediator = mediator;
            _decisionRepository = decisionRepository;
            _customerRepository = customerRepository;
            _reportGenerator = reportGenerator;
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
        /// Get recent decision records
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Number of results per page</param>
        /// <returns>Paginated list of decisions</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<HybridDecision>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDecisions([FromQuery] int page = 1, [FromQuery] int pageSize = 25)
        {
            try
            {
                _logger.LogInformation("Fetching paginated decision records");
                var decisions = await _decisionRepository.GetDecisionsAsync(page, pageSize);
                return Ok(decisions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching decision records");
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

        /// <summary>
        /// Generate a formal PDF report for a single decision, including the
        /// client profile (when available) and the full XAI audit trail
        /// </summary>
        /// <param name="id">Decision ID</param>
        [HttpGet("{id}/report")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDecisionReport(int id)
        {
            HybridDecision decision;
            try
            {
                decision = await _decisionRepository.GetDecisionByIdAsync(id);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Decision with ID {id} not found" });
            }

            var customer = await _customerRepository.FindCustomerByIdAsync(decision.CustomerId);

            try
            {
                var pdfBytes = _reportGenerator.GenerateDecisionReportPdf(decision, customer);
                return File(pdfBytes, "application/pdf", $"vendim-{decision.Id}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating PDF report for decision {id}");
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }
    }
}
