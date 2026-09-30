using System.Diagnostics;
using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace HybridDecisionIntelligence.API.Controllers
{
    /// <summary>
    /// The ML model as a standalone HTTP service, plus a benchmark comparing in-process
    /// inference (how the decision pipeline calls ML.NET) with calling the same model
    /// over HTTP (how a separate model service, e.g. in Python, would be called).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class ModelController : ControllerBase
    {
        private const int WarmupCalls = 50;

        private readonly IMLModelService _model;
        private readonly IDatasetService _datasetService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ModelController(
            IMLModelService model,
            IDatasetService datasetService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _model = model;
            _datasetService = datasetService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Held-out evaluation of the trained model (Accuracy, AUC, F1, Precision, Recall),
        /// as written next to the model file when it was trained
        /// </summary>
        [HttpGet("metrics")]
        [ProducesResponseType(typeof(ModelEvaluationMetrics), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Metrics()
        {
            var modelPath = _configuration["MLModel:ModelPath"] ?? "Models/BankMarketingModel.zip";
            var metricsPath = Path.ChangeExtension(modelPath, ".metrics.json");
            if (!System.IO.File.Exists(metricsPath))
            {
                return NotFound(new { message = "No evaluation metrics found - retrain the model to produce them" });
            }

            var metrics = System.Text.Json.JsonSerializer.Deserialize<ModelEvaluationMetrics>(
                System.IO.File.ReadAllText(metricsPath),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Ok(metrics);
        }

        /// <summary>
        /// Score one customer row with the ML.NET model (no business rules, nothing stored)
        /// </summary>
        [HttpPost("predict")]
        [ProducesResponseType(typeof(BankMarketingPrediction), StatusCodes.Status200OK)]
        public async Task<ActionResult<BankMarketingPrediction>> Predict([FromBody] BankMarketingData row) =>
            Ok(await _model.PredictAsync(row));

        /// <summary>
        /// Scores the first N dataset rows twice: directly in-process, and through
        /// POST /api/v1/model/predict over HTTP. Both paths are warmed up first so JIT
        /// and model loading are excluded. The HTTP path runs over loopback on the same
        /// machine, so it is a lower bound for a real remote model service.
        /// </summary>
        /// <param name="samples">Number of rows to score on each path (10–10,000)</param>
        [HttpGet("benchmark")]
        [ProducesResponseType(typeof(InferenceBenchmarkResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> Benchmark([FromQuery] int samples = 1000, CancellationToken cancellationToken = default)
        {
            samples = Math.Clamp(samples, 10, 10_000);
            var rows = _datasetService.LoadCustomers(samples).Select(MLPredictor.CustomerToMLData).ToList();

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}/");

            async Task<BankMarketingPrediction?> ViaHttp(BankMarketingData row)
            {
                using var response = await client.PostAsJsonAsync("api/v1/model/predict", row, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<BankMarketingPrediction>(cancellationToken: cancellationToken);
            }

            for (var i = 0; i < WarmupCalls; i++)
            {
                var row = rows[i % rows.Count];
                await _model.PredictAsync(row);
                await ViaHttp(row);
            }

            var inProcess = await Measure(rows, row => _model.PredictAsync(row));
            var http = await Measure(rows, ViaHttp);

            return Ok(new InferenceBenchmarkResult
            {
                Samples = rows.Count,
                InProcess = inProcess,
                HttpLoopback = http,
                SpeedupMean = Math.Round(http.MeanMs / inProcess.MeanMs, 1),
                Note = "HTTP measured over loopback on the same machine (no network, no TLS): a lower bound " +
                       "for a remote model service. Warm-up calls excluded."
            });
        }

        private static async Task<LatencySummary> Measure<T>(List<BankMarketingData> rows, Func<BankMarketingData, Task<T>> call)
        {
            var timings = new List<double>(rows.Count);
            var total = Stopwatch.StartNew();
            foreach (var row in rows)
            {
                var sw = Stopwatch.StartNew();
                await call(row);
                timings.Add(sw.Elapsed.TotalMilliseconds);
            }
            total.Stop();

            timings.Sort();
            double Percentile(double p) => timings[Math.Min(timings.Count - 1, (int)Math.Ceiling(p * timings.Count) - 1)];

            return new LatencySummary
            {
                MeanMs = Math.Round(timings.Average(), 4),
                P50Ms = Math.Round(Percentile(0.50), 4),
                P95Ms = Math.Round(Percentile(0.95), 4),
                P99Ms = Math.Round(Percentile(0.99), 4),
                MaxMs = Math.Round(timings[^1], 4),
                ThroughputPerSecond = Math.Round(rows.Count / total.Elapsed.TotalSeconds, 0)
            };
        }
    }

    /// <summary>Contents of BankMarketingModel.metrics.json (held-out evaluation)</summary>
    public class ModelEvaluationMetrics
    {
        public DateTime EvaluatedAtUtc { get; set; }
        public TrainTestSplitInfo TrainTestSplit { get; set; } = new();
        public double Accuracy { get; set; }
        public double AreaUnderRocCurve { get; set; }
        public double F1Score { get; set; }
        public double PositivePrecision { get; set; }
        public double PositiveRecall { get; set; }
        public double NegativePrecision { get; set; }
        public double NegativeRecall { get; set; }
        public double LogLoss { get; set; }

        public class TrainTestSplitInfo
        {
            public double TestFraction { get; set; }
            public int Seed { get; set; }
            public string Note { get; set; } = string.Empty;
        }
    }

    public class InferenceBenchmarkResult
    {
        public int Samples { get; set; }
        public LatencySummary InProcess { get; set; } = new();
        public LatencySummary HttpLoopback { get; set; } = new();

        /// <summary>How many times slower the HTTP path is on average</summary>
        public double SpeedupMean { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class LatencySummary
    {
        public double MeanMs { get; set; }
        public double P50Ms { get; set; }
        public double P95Ms { get; set; }
        public double P99Ms { get; set; }
        public double MaxMs { get; set; }
        public double ThroughputPerSecond { get; set; }
    }
}
