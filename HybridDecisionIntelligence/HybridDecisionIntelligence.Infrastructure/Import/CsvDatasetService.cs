using System.Diagnostics;
using System.Globalization;
using HybridDecisionIntelligence.Application.Repositories;
using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Domain.ValueObjects;
using HybridDecisionIntelligence.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HybridDecisionIntelligence.Infrastructure.Import
{
    /// <summary>
    /// Works with the UCI Bank Marketing CSV. Every row goes through the same ML.NET
    /// model and the same DecisionEngine/BusinessRuleEngine as a single API request;
    /// only the transport differs (no HTTP, batched writes, per-row logging silenced)
    /// so all 45,211 rows finish in minutes.
    /// </summary>
    public class CsvDatasetService : IDatasetService
    {
        /// <summary>Customer Id = 100000 + CSV row number, so imported rows never
        /// collide with manually evaluated customers and re-runs skip them.</summary>
        public const int CustomerIdOffset = 100000;
        private const int BatchSize = 1000;

        private readonly HybridDecisionContext _context;
        private readonly IBusinessRuleRepository _ruleRepository;
        private readonly IDecisionRepository _decisionRepository;
        private readonly IMLModelService _model;
        private readonly DecisionPolicyOptions _policy;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CsvDatasetService> _logger;

        public CsvDatasetService(
            HybridDecisionContext context,
            IBusinessRuleRepository ruleRepository,
            IDecisionRepository decisionRepository,
            IMLModelService model,
            DecisionPolicyOptions policy,
            IConfiguration configuration,
            ILogger<CsvDatasetService> logger)
        {
            _context = context;
            _ruleRepository = ruleRepository;
            _decisionRepository = decisionRepository;
            _model = model;
            _policy = policy;
            _configuration = configuration;
            _logger = logger;
        }

        private string DataPath => _configuration["MLModel:DataPath"] ?? "Data/BankData.csv";

        public IReadOnlyList<BankCustomer> LoadCustomers(int? limit = null)
        {
            var customers = ReadCustomers(DataPath);
            return limit.HasValue ? customers.Take(limit.Value).ToList() : customers;
        }

        /// <summary>
        /// Same decision logic as the API, with the active rules loaded once instead of
        /// queried per row, and per-row Information logs silenced.
        /// </summary>
        private DecisionEngine CreateDecisionEngine(List<BusinessRule> rules, DecisionPolicyOptions policy) =>
            new(
                new BusinessRuleEngine(new FixedRuleRepository(rules), NullLogger<BusinessRuleEngine>.Instance),
                _decisionRepository,
                NullLogger<DecisionEngine>.Instance,
                policy);

        public async Task<List<ReferenceRateScenario>> SimulateReferenceRatesAsync(
            IReadOnlyList<decimal> referenceRates, int? limit = null, CancellationToken cancellationToken = default)
        {
            var customers = LoadCustomers(limit);
            var rules = await _ruleRepository.GetActiveRulesAsync();

            // The model does not depend on the reference rate: predict every row once
            var predictions = new List<MLPredictionResult>(customers.Count);
            foreach (var customer in customers)
            {
                var p = await _model.PredictAsync(MLPredictor.CustomerToMLData(customer));
                predictions.Add(new MLPredictionResult
                {
                    CustomerId = customer.Id,
                    PredictedLabel = p.Prediction,
                    Score = p.Score,
                    Probability = p.Probability
                });
            }

            var scenarios = new List<ReferenceRateScenario>();
            foreach (var rate in referenceRates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var engine = CreateDecisionEngine(rules, new DecisionPolicyOptions { ReferenceRate = rate });
                var scenario = new ReferenceRateScenario { ReferenceRate = rate, Customers = customers.Count };
                decimal approvedRateSum = 0;

                for (var i = 0; i < customers.Count; i++)
                {
                    var decision = await engine.MakeDecisionAsync(customers[i], predictions[i]);
                    if (decision.MLPredicted) scenario.AiApproved++;
                    if (decision.WasOverridden)
                    {
                        scenario.Overridden++;
                        if (decision.OverrideReason.Contains("Interest Rate Policy")) scenario.OverriddenByRatePolicy++;
                    }
                    if (decision.FinalDecision)
                    {
                        scenario.Approved++;
                        approvedRateSum += decision.ApprovedInterestRate;
                        if (customers[i].SubscribedToTerm) scenario.ApprovedRealSubscribers++;
                    }
                }

                scenario.AverageOfferedRateApproved =
                    scenario.Approved == 0 ? 0 : Math.Round(approvedRateSum / scenario.Approved, 4);
                scenarios.Add(scenario);
            }

            return scenarios;
        }

        public async Task<DatasetImportResult> ImportAsync(int? limit = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var allRows = ReadCustomers(DataPath);
            var rows = limit.HasValue ? allRows.Take(limit.Value).ToList() : allRows;

            var result = new DatasetImportResult
            {
                RowsInDataset = allRows.Count,
                CustomerIdOffset = CustomerIdOffset
            };

            // Resumable: skip rows whose customer was already imported
            var maxId = CustomerIdOffset + allRows.Count;
            var existing = (await _context.BankCustomers
                    .Where(c => c.Id > CustomerIdOffset && c.Id <= maxId)
                    .Select(c => c.Id)
                    .ToListAsync(cancellationToken))
                .ToHashSet();
            var pending = rows.Where(c => !existing.Contains(c.Id)).ToList();
            result.SkippedAlreadyImported = rows.Count - pending.Count;

            var rules = await _ruleRepository.GetActiveRulesAsync();
            var decisionEngine = CreateDecisionEngine(rules, _policy);

            _logger.LogInformation(
                "Dataset import: {Pending} rows to process ({Skipped} already imported), {Rules} active rules",
                pending.Count, result.SkippedAlreadyImported, rules.Count);

            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                foreach (var batch in pending.Chunk(BatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                    var predictions = new List<MLPredictionResult>(batch.Length);
                    foreach (var customer in batch)
                    {
                        var prediction = await _model.PredictAsync(MLPredictor.CustomerToMLData(customer));
                        predictions.Add(new MLPredictionResult
                        {
                            CustomerId = customer.Id,
                            PredictedLabel = prediction.Prediction,
                            Score = prediction.Score,
                            Probability = prediction.Probability,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // Customers and predictions first, so predictions get their Ids
                    _context.BankCustomers.AddRange(batch);
                    _context.MLPredictionResults.AddRange(predictions);
                    await _context.SaveChangesAsync(cancellationToken);

                    for (var i = 0; i < batch.Length; i++)
                    {
                        var decision = await decisionEngine.MakeDecisionAsync(batch[i], predictions[i]);
                        decision.CustomerSnapshotJson = CustomerSnapshot.Serialize(batch[i]);
                        _context.HybridDecisions.Add(decision);

                        if (decision.FinalDecision) result.Approved++;
                        if (decision.WasOverridden) result.Overridden++;
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    _context.ChangeTracker.Clear();

                    result.Imported += batch.Length;
                    _logger.LogInformation("Dataset import: {Done}/{Total} rows", result.Imported, pending.Count);
                }
            }
            finally
            {
                _context.ChangeTracker.AutoDetectChangesEnabled = true;
            }

            result.ElapsedSeconds = Math.Round(stopwatch.Elapsed.TotalSeconds, 1);
            _logger.LogInformation(
                "Dataset import finished: {Imported} imported, {Approved} approved, {Overridden} overridden in {Seconds}s",
                result.Imported, result.Approved, result.Overridden, result.ElapsedSeconds);
            return result;
        }

        /// <summary>
        /// Parses the semicolon-separated UCI file (quoted strings, header row).
        /// Columns are looked up by header name, not position.
        /// </summary>
        private static List<BankCustomer> ReadCustomers(string path)
        {
            using var reader = new StreamReader(path);
            var header = Split(reader.ReadLine() ?? throw new InvalidDataException($"{path} is empty"));
            var col = header
                .Select((name, index) => (name, index))
                .ToDictionary(x => x.name, x => x.index, StringComparer.OrdinalIgnoreCase);

            var customers = new List<BankCustomer>();
            string? line;
            var rowNumber = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                rowNumber++;
                var f = Split(line);
                int Int(string name) => int.Parse(f[col[name]], CultureInfo.InvariantCulture);

                customers.Add(new BankCustomer
                {
                    Id = CustomerIdOffset + rowNumber,
                    Age = Int("age"),
                    Job = f[col["job"]],
                    Marital = f[col["marital"]],
                    Education = f[col["education"]],
                    Default = f[col["default"]],
                    Balance = decimal.Parse(f[col["balance"]], CultureInfo.InvariantCulture),
                    Housing = f[col["housing"]],
                    Loan = f[col["loan"]],
                    Contact = f[col["contact"]],
                    Day = Int("day"),
                    Month = f[col["month"]],
                    Duration = Int("duration"),
                    Campaign = Int("campaign"),
                    PDays = Int("pdays"),
                    Previous = Int("previous"),
                    POutcome = f[col["poutcome"]],
                    // Ground truth: did this customer really subscribe? Lets decisions
                    // be compared with real outcomes.
                    SubscribedToTerm = f[col["y"]] == "yes",
                    CreatedAt = DateTime.UtcNow
                });
            }

            return customers;
        }

        private static string[] Split(string line) =>
            line.Split(';').Select(v => v.Trim().Trim('"')).ToArray();

        /// <summary>Rule repository over a pre-loaded rule list (read-only)</summary>
        private sealed class FixedRuleRepository : IBusinessRuleRepository
        {
            private readonly List<BusinessRule> _rules;
            public FixedRuleRepository(List<BusinessRule> rules) => _rules = rules;

            public Task<List<BusinessRule>> GetActiveRulesAsync() => Task.FromResult(_rules);
            public Task<BusinessRule> GetRuleByIdAsync(int id) => Task.FromResult(_rules.First(r => r.Id == id));
            public Task SaveRuleAsync(BusinessRule rule) => throw new NotSupportedException();
            public Task UpdateRuleAsync(BusinessRule rule) => throw new NotSupportedException();
            public Task DeleteRuleAsync(int id) => throw new NotSupportedException();
        }
    }
}
