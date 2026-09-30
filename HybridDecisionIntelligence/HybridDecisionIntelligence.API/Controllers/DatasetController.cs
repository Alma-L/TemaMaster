using HybridDecisionIntelligence.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HybridDecisionIntelligence.API.Controllers
{
    /// <summary>
    /// Bulk operations on the UCI Bank Marketing dataset: import and reference-rate simulation
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class DatasetController : ControllerBase
    {
        private readonly IDatasetService _datasetService;
        private readonly ILogger<DatasetController> _logger;

        public DatasetController(IDatasetService datasetService, ILogger<DatasetController> logger)
        {
            _datasetService = datasetService;
            _logger = logger;
        }

        /// <summary>
        /// Run every CSV row through the hybrid pipeline (ML.NET + business rules)
        /// and store the decisions. Already-imported rows are skipped, so it is safe
        /// to call again. Takes a few minutes for the full 45,211 rows.
        /// </summary>
        /// <param name="limit">Optional: only the first N rows of the CSV</param>
        [HttpPost("import")]
        [ProducesResponseType(typeof(DatasetImportResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Import([FromQuery] int? limit, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _datasetService.ImportAsync(limit, cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dataset import failed");
                return StatusCode(500, new { error = "Dataset import failed", details = ex.Message });
            }
        }

        /// <summary>
        /// What-if analysis of the macro-economic reference rate: re-decides the dataset
        /// under each given rate (nothing is stored) and reports approvals and overrides.
        /// The ML predictions are identical in every scenario; only the decision layer changes.
        /// </summary>
        /// <param name="referenceRates">Comma-separated fractions, e.g. 0,0.02,0.04 (default 0%–12% in 2% steps)</param>
        /// <param name="limit">Optional: only the first N rows of the CSV</param>
        [HttpGet("simulate")]
        [ProducesResponseType(typeof(List<ReferenceRateScenario>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Simulate(
            [FromQuery] string? referenceRates, [FromQuery] int? limit, CancellationToken cancellationToken)
        {
            var rates = new List<decimal>();
            foreach (var part in (referenceRates ?? "0,0.02,0.04,0.06,0.08,0.10,0.12").Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!decimal.TryParse(part.Trim(), System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var rate) || rate < 0 || rate > 1)
                {
                    return BadRequest(new { error = $"Invalid reference rate '{part}' - use fractions between 0 and 1, e.g. 0.04" });
                }
                rates.Add(rate);
            }

            try
            {
                return Ok(await _datasetService.SimulateReferenceRatesAsync(rates, limit, cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reference rate simulation failed");
                return StatusCode(500, new { error = "Simulation failed", details = ex.Message });
            }
        }
    }
}
