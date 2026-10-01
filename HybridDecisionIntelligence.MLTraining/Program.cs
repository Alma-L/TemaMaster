using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using HybridDecisionIntelligence.Application.Repositories;
using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.ML;
using Microsoft.ML.Data;

// Reproducible experiments behind Chapter 5 of the thesis:
//   1. model comparison on the same 80/20 split as the production model (seed 0)
//   2. 5-fold cross-validation on the training split + a small FastTree grid search
//   3. threshold / precision-recall analysis and confusion matrices
//   4. permutation feature importance per original attribute
//   5. the hybrid system evaluated on the held-out 20% only
//   6. rule impact, risk levels and reference-rate simulation on the full dataset
//
// Usage: dotnet run -c Release -- [dataPath] [modelPath] [outDir]
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var apiDir = Path.Combine("..", "HybridDecisionIntelligence", "HybridDecisionIntelligence.API");
var dataPath = args.Length > 0 ? args[0] : Path.Combine(apiDir, "Data", "BankData.csv");
var modelPath = args.Length > 1 ? args[1] : Path.Combine(apiDir, "Models", "BankMarketingModel.zip");
var outDir = args.Length > 2 ? args[2] : "results";
Directory.CreateDirectory(outDir);

var ml = new MLContext(seed: 0);
var data = ml.Data.LoadFromTextFile<BankMarketingData>(dataPath, hasHeader: true, separatorChar: ';', allowQuoting: true);
var split = ml.Data.TrainTestSplit(data, testFraction: 0.2, seed: 0);
var results = new Dictionary<string, object>();

// Same featurization as MLNetModelService ("Duration" excluded: target leakage)
string[] categorical = { "Job", "Marital", "Education", "Default", "Housing", "Loan", "Contact", "Month", "POutcome" };
string[] features = { "Age", "Job", "Marital", "Education", "Default", "Balance", "Housing", "Loan", "Contact",
                      "Day", "Month", "Campaign", "PDays", "Previous", "POutcome" };

IEstimator<ITransformer> Featurize()
{
    IEstimator<ITransformer> p = ml.Transforms.Categorical.OneHotEncoding(categorical[0], categorical[0]);
    foreach (var c in categorical.Skip(1)) p = p.Append(ml.Transforms.Categorical.OneHotEncoding(c, c));
    return p.Append(ml.Transforms.Concatenate("Features", features))
            .Append(ml.Transforms.NormalizeMinMax("Features", "Features"));
}

IEstimator<ITransformer> FastTree(int trees, int leaves, double lr = 0.2) =>
    Featurize().Append(ml.BinaryClassification.Trainers.FastTree(
        numberOfTrees: trees, numberOfLeaves: leaves, learningRate: lr));

var trainTestRows = ml.Data.CreateEnumerable<BankMarketingData>(split.TestSet, reuseRowObject: false).ToList();
var testPositives = trainTestRows.Count(r => r.Label);
Console.WriteLine($"Train/test split: test rows = {trainTestRows.Count}, positives = {testPositives}");

// ---------------------------------------------------------------- 1 + 2. comparison
var candidates = new (string Name, IEstimator<ITransformer> Pipeline)[]
{
    ("Logistic Regression (L-BFGS)", Featurize().Append(ml.BinaryClassification.Trainers.LbfgsLogisticRegression())),
    ("FastForest (Random Forest, 100 pemë)", Featurize().Append(ml.BinaryClassification.Trainers.FastForest(numberOfTrees: 100))),
    ("LightGBM (100 iteracione)", Featurize().Append(ml.BinaryClassification.Trainers.LightGbm())),
    ("FastTree (100 pemë, 10 gjethe) – modeli i sistemit", FastTree(100, 10)),
};

var comparison = new List<Dictionary<string, object>>();
foreach (var (name, pipeline) in candidates)
{
    var sw = Stopwatch.StartNew();
    var cv = ml.BinaryClassification.CrossValidateNonCalibrated(split.TrainSet, pipeline, numberOfFolds: 5, seed: 0);
    var cvAuc = cv.Select(f => f.Metrics.AreaUnderRocCurve).ToArray();
    var cvAuprc = cv.Select(f => f.Metrics.AreaUnderPrecisionRecallCurve).ToArray();

    var model = pipeline.Fit(split.TrainSet);
    var m = ml.BinaryClassification.EvaluateNonCalibrated(model.Transform(split.TestSet));
    var row = new Dictionary<string, object>
    {
        ["model"] = name,
        ["cvAucMean"] = cvAuc.Average(), ["cvAucStd"] = Std(cvAuc),
        ["cvAuprcMean"] = cvAuprc.Average(), ["cvAuprcStd"] = Std(cvAuprc),
        ["testAuc"] = m.AreaUnderRocCurve, ["testAuprc"] = m.AreaUnderPrecisionRecallCurve,
        ["testAccuracy"] = m.Accuracy, ["testPrecision"] = m.PositivePrecision,
        ["testRecall"] = m.PositiveRecall, ["testF1"] = m.F1Score,
        ["seconds"] = Math.Round(sw.Elapsed.TotalSeconds, 1)
    };
    comparison.Add(row);
    Console.WriteLine($"{name,-52} CV AUC {cvAuc.Average():P1} ± {Std(cvAuc):P1} | test AUC {m.AreaUnderRocCurve:P1} " +
                      $"AUPRC {m.AreaUnderPrecisionRecallCurve:P1} Acc {m.Accuracy:P1} P {m.PositivePrecision:P1} " +
                      $"R {m.PositiveRecall:P1} F1 {m.F1Score:P1} ({sw.Elapsed.TotalSeconds:F0}s)");
}
results["comparison"] = comparison;

// FastTree grid search, selected by 5-fold CV AUC on the training split only
var grid = new List<Dictionary<string, object>>();
foreach (var trees in new[] { 100, 300 })
foreach (var leaves in new[] { 10, 31 })
foreach (var lr in new[] { 0.1, 0.2 })
{
    var cv = ml.BinaryClassification.CrossValidateNonCalibrated(split.TrainSet, FastTree(trees, leaves, lr), numberOfFolds: 5, seed: 0);
    var auc = cv.Select(f => f.Metrics.AreaUnderRocCurve).ToArray();
    grid.Add(new() { ["trees"] = trees, ["leaves"] = leaves, ["learningRate"] = lr,
                     ["cvAucMean"] = auc.Average(), ["cvAucStd"] = Std(auc) });
    Console.WriteLine($"grid trees={trees,3} leaves={leaves,2} lr={lr:0.00}: CV AUC {auc.Average():P2} ± {Std(auc):P2}");
}
var best = grid.OrderByDescending(g => (double)g["cvAucMean"]).First();
var bestModel = FastTree((int)best["trees"], (int)best["leaves"], (double)best["learningRate"]).Fit(split.TrainSet);
var bestMetrics = ml.BinaryClassification.Evaluate(bestModel.Transform(split.TestSet));
best["testAuc"] = bestMetrics.AreaUnderRocCurve;
best["testAuprc"] = bestMetrics.AreaUnderPrecisionRecallCurve;
best["testPrecision"] = bestMetrics.PositivePrecision;
best["testRecall"] = bestMetrics.PositiveRecall;
best["testF1"] = bestMetrics.F1Score;
results["grid"] = grid;
results["gridBest"] = best;
Console.WriteLine($"Best grid config: {JsonSerializer.Serialize(best)}");

// ---------------------------------------------------------------- 3. thresholds
var production = ml.Model.Load(modelPath, out _);
var prodMetrics = ml.BinaryClassification.Evaluate(production.Transform(split.TestSet));
Console.WriteLine($"Production model on test: AUC {prodMetrics.AreaUnderRocCurve:P2}, AUPRC {prodMetrics.AreaUnderPrecisionRecallCurve:P2}, " +
                  $"Acc {prodMetrics.Accuracy:P2}, P {prodMetrics.PositivePrecision:P2}, R {prodMetrics.PositiveRecall:P2}");
results["productionTest"] = new
{
    prodMetrics.Accuracy, prodMetrics.AreaUnderRocCurve, prodMetrics.AreaUnderPrecisionRecallCurve,
    prodMetrics.PositivePrecision, prodMetrics.PositiveRecall, prodMetrics.F1Score, prodMetrics.LogLoss
};

var scored = Probabilities(production, trainTestRows);
var thresholds = new List<object>();
foreach (var t in new[] { 0.1f, 0.15f, 0.2f, 0.25f, 0.3f, 0.35f, 0.4f, 0.5f, 0.6f, 0.7f })
    thresholds.Add(Confusion(scored, t));

// F1-optimal threshold over every distinct probability
var bestF1 = scored.Select(s => s.P).Distinct()
    .Select(t => Confusion(scored, t))
    .OrderByDescending(c => c.F1).First();
results["thresholds"] = thresholds;
results["thresholdF1Optimal"] = bestF1;
results["confusionAt05"] = Confusion(scored, 0.5f);
Console.WriteLine($"F1-optimal threshold: {JsonSerializer.Serialize(bestF1)}");
Console.WriteLine($"At 0.5: {JsonSerializer.Serialize(Confusion(scored, 0.5f))}");

// Precision-recall curve points (for the thesis figure)
using (var w = new StreamWriter(Path.Combine(outDir, "pr_curve.csv")))
{
    w.WriteLine("threshold;precision;recall");
    foreach (var t in Enumerable.Range(1, 99).Select(i => i / 100f))
    {
        var c = Confusion(scored, t);
        if (c.Tp + c.Fp > 0) w.WriteLine($"{t:0.00};{c.Precision:0.0000};{c.Recall:0.0000}");
    }
}

// ---------------------------------------------------------------- 4. permutation importance
var baseAuc = prodMetrics.AreaUnderRocCurve;
var pfi = new List<Dictionary<string, object>>();
foreach (var feature in features)
{
    var drops = new List<double>();
    for (var repeat = 0; repeat < 5; repeat++)
    {
        var rows = trainTestRows.Select(Clone).ToList();
        var prop = typeof(BankMarketingData).GetProperty(feature)!;
        var values = rows.Select(r => prop.GetValue(r)).ToList();
        var rng = new Random(1000 + repeat);
        values = values.OrderBy(_ => rng.Next()).ToList();
        for (var i = 0; i < rows.Count; i++) prop.SetValue(rows[i], values[i]);
        var m = ml.BinaryClassification.Evaluate(production.Transform(ml.Data.LoadFromEnumerable(rows)));
        drops.Add(baseAuc - m.AreaUnderRocCurve);
    }
    pfi.Add(new() { ["feature"] = feature, ["aucDrop"] = drops.Average(), ["aucDropStd"] = Std(drops.ToArray()) });
}
pfi = pfi.OrderByDescending(p => (double)p["aucDrop"]).ToList();
results["permutationImportance"] = pfi;
foreach (var p in pfi) Console.WriteLine($"PFI {p["feature"],-10} ΔAUC {(double)p["aucDrop"]:P2} ± {(double)p["aucDropStd"]:P2}");

// ---------------------------------------------------------------- 5. hybrid on the held-out test set
var seededRules = new List<BusinessRule>
{
    new() { Id = 1, Name = "Minimum Balance Rule", MinBalance = 1000, MinInterestRate = 0.02m, MaxInterestRate = 0.12m },
    new() { Id = 2, Name = "Age Eligibility Rule", MinAge = 25, MaxAge = 70, MinInterestRate = 0.02m, MaxInterestRate = 0.12m },
    new() { Id = 3, Name = "No Default History", RequireNoDefault = true, MinInterestRate = 0.02m, MaxInterestRate = 0.12m },
};
var ruleEngine = new BusinessRuleEngine(new FixedRuleRepository(seededRules), NullLogger<BusinessRuleEngine>.Instance);
DecisionEngine Engine(decimal referenceRate) =>
    new(ruleEngine, null!, NullLogger<DecisionEngine>.Instance, new DecisionPolicyOptions { ReferenceRate = referenceRate });

var testCustomers = trainTestRows.Select((r, i) => ToCustomer(r, i + 1)).ToList();
var testPredictions = scored.Select(s => s.P).ToList();
results["hybridTestSet"] = await Strategies(testCustomers, testPredictions, bestF1.Threshold);

// ---------------------------------------------------------------- 6. full dataset (rules are model-independent)
var allRows = ml.Data.CreateEnumerable<BankMarketingData>(data, reuseRowObject: false).ToList();
var allCustomers = allRows.Select((r, i) => ToCustomer(r, i + 1)).ToList();
var allProbabilities = Probabilities(production, allRows).Select(s => s.P).ToList();
var subscribers = allCustomers.Count(c => c.SubscribedToTerm);

var ruleResults = new List<BusinessRuleResult>();
foreach (var c in allCustomers) ruleResults.Add(await ruleEngine.EvaluateAsync(c));
double Share(Func<int, bool> pred) => Enumerable.Range(0, allCustomers.Count).Count(pred) / (double)allCustomers.Count;
double Rate(Func<int, bool> pred)
{
    var idx = Enumerable.Range(0, allCustomers.Count).Where(pred).ToList();
    return idx.Count == 0 ? 0 : idx.Count(i => allCustomers[i].SubscribedToTerm) / (double)idx.Count;
}
bool Passes(int i, string rule) => ruleResults[i].AppliedRules.Contains(rule);
results["ruleImpact"] = new Dictionary<string, object>
{
    ["passBalance"] = Share(i => Passes(i, "Minimum Balance Rule")),
    ["passAge"] = Share(i => Passes(i, "Age Eligibility Rule")),
    ["passDefault"] = Share(i => Passes(i, "No Default History")),
    ["passAll"] = Share(i => ruleResults[i].IsApproved),
    ["subscriptionRatePass"] = Rate(i => ruleResults[i].IsApproved),
    ["subscriptionRateFail"] = Rate(i => !ruleResults[i].IsApproved),
    ["subscribersPassingShare"] = Enumerable.Range(0, allCustomers.Count)
        .Count(i => ruleResults[i].IsApproved && allCustomers[i].SubscribedToTerm) / (double)subscribers,
    ["customersFailingMoreThanOneRule"] = Enumerable.Range(0, allCustomers.Count).Count(i => ruleResults[i].FailedRules.Count > 1),
};
results["riskLevels"] = new[] { "Low", "Medium", "High" }.Select(level => new
{
    level,
    share = Share(i => ruleResults[i].RiskLevel == level),
    subscriptionRate = Rate(i => ruleResults[i].RiskLevel == level)
}).ToList();

var simulation = new List<Dictionary<string, object>>();
foreach (var rate in new[] { 0m, 0.02m, 0.04m, 0.06m, 0.08m, 0.10m, 0.12m })
{
    var engine = Engine(rate);
    int ai = 0, approved = 0, overridden = 0, byRate = 0, bal = 0, age = 0, def = 0;
    decimal rateSum = 0;
    for (var i = 0; i < allCustomers.Count; i++)
    {
        var d = await engine.MakeDecisionAsync(allCustomers[i], Prediction(allProbabilities[i], 0.5f));
        if (d.MLPredicted) ai++;
        if (d.FinalDecision) { approved++; rateSum += d.ApprovedInterestRate; }
        if (d.WasOverridden)
        {
            overridden++;
            if (d.OverrideReason.Contains("Interest Rate Policy")) byRate++;
            if (d.OverrideReason.Contains("Minimum Balance Rule")) bal++;
            if (d.OverrideReason.Contains("Age Eligibility Rule")) age++;
            if (d.OverrideReason.Contains("No Default History")) def++;
        }
    }
    simulation.Add(new()
    {
        ["referenceRate"] = rate, ["aiApproved"] = ai, ["approved"] = approved, ["overridden"] = overridden,
        ["overriddenByRatePolicy"] = byRate, ["overriddenByBalance"] = bal, ["overriddenByAge"] = age,
        ["overriddenByDefault"] = def, ["avgOfferedRate"] = approved == 0 ? 0 : Math.Round(rateSum / approved, 4)
    });
    Console.WriteLine($"sim {rate:P0}: AI {ai} approved {approved} overridden {overridden} (rate {byRate}, bal {bal}, age {age}, def {def}) avg {(approved == 0 ? 0 : rateSum / approved):P2}");
}
results["simulation"] = simulation;
results["hybridFullDataset"] = await Strategies(allCustomers, allProbabilities, bestF1.Threshold);

// ---------------------------------------------------------------- 7. in-process decision latency (no database)
var engine4 = Engine(0.04m);
var single = ml.Model.CreatePredictionEngine<BankMarketingData, BankMarketingPrediction>(production);
var sample = testCustomers.Take(5000).ToList();
for (var i = 0; i < 200; i++) { single.Predict(MLPredictor.CustomerToMLData(sample[i])); }
var timings = new List<double>();
var predictTimings = new List<double>();
foreach (var c in sample)
{
    var t0 = Stopwatch.GetTimestamp();
    var p = single.Predict(MLPredictor.CustomerToMLData(c));
    var t1 = Stopwatch.GetTimestamp();
    await engine4.MakeDecisionAsync(c, new MLPredictionResult { PredictedLabel = p.Prediction, Probability = p.Probability });
    var t2 = Stopwatch.GetTimestamp();
    predictTimings.Add((t1 - t0) * 1000.0 / Stopwatch.Frequency);
    timings.Add((t2 - t0) * 1000.0 / Stopwatch.Frequency);
}
results["inProcessDecisionLatencyMs"] = new
{
    predictMean = predictTimings.Average(), predictP95 = Percentile(predictTimings, 0.95),
    decisionMean = timings.Average(), decisionP95 = Percentile(timings, 0.95)
};
Console.WriteLine($"In-process: predict {predictTimings.Average():F4} ms, predict+rules+pricing {timings.Average():F4} ms (p95 {Percentile(timings, 0.95):F4})");

var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync(Path.Combine(outDir, "experiment_results.json"), json);
Console.WriteLine($"Results written to {Path.GetFullPath(outDir)}");

// ================================================================= helpers
async Task<List<Dictionary<string, object>>> Strategies(List<BankCustomer> customers, List<float> probabilities, float tuned)
{
    var total = customers.Count(c => c.SubscribedToTerm);
    var engine = Engine(0.04m);
    var sets = new Dictionary<string, List<bool>>
    {
        ["Fushatë masive (të gjithë)"] = new(), ["Vetëm rregullat e biznesit"] = new(),
        ["Vetëm modeli ML (prag 0,5)"] = new(), ["Arkitektura hibride (prag 0,5)"] = new(),
        [$"Vetëm modeli ML (prag {tuned:0.00})"] = new(), [$"Arkitektura hibride (prag {tuned:0.00})"] = new(),
    };
    for (var i = 0; i < customers.Count; i++)
    {
        var d05 = await engine.MakeDecisionAsync(customers[i], Prediction(probabilities[i], 0.5f));
        var dT = await engine.MakeDecisionAsync(customers[i], Prediction(probabilities[i], tuned));
        var rules = await ruleEngine.EvaluateAsync(customers[i]);
        sets["Fushatë masive (të gjithë)"].Add(true);
        sets["Vetëm rregullat e biznesit"].Add(rules.IsApproved);
        sets["Vetëm modeli ML (prag 0,5)"].Add(d05.MLPredicted);
        sets["Arkitektura hibride (prag 0,5)"].Add(d05.FinalDecision);
        sets[$"Vetëm modeli ML (prag {tuned:0.00})"].Add(dT.MLPredicted);
        sets[$"Arkitektura hibride (prag {tuned:0.00})"].Add(dT.FinalDecision);
    }
    var rows = new List<Dictionary<string, object>>();
    foreach (var (name, selected) in sets)
    {
        var n = selected.Count(s => s);
        var hits = Enumerable.Range(0, customers.Count).Count(i => selected[i] && customers[i].SubscribedToTerm);
        rows.Add(new()
        {
            ["strategy"] = name, ["customers"] = customers.Count, ["selected"] = n,
            ["selectedShare"] = n / (double)customers.Count, ["subscribers"] = hits,
            ["precision"] = n == 0 ? 0 : hits / (double)n, ["recall"] = hits / (double)total
        });
        Console.WriteLine($"[{customers.Count}] {name,-40} selected {n,6} precision {(n == 0 ? 0 : hits / (double)n):P1} recall {hits / (double)total:P1}");
    }
    return rows;
}

List<(float P, bool Label)> Probabilities(ITransformer model, List<BankMarketingData> rows) =>
    ml.Data.CreateEnumerable<BankMarketingPrediction>(model.Transform(ml.Data.LoadFromEnumerable(rows)), reuseRowObject: false)
        .Zip(rows, (p, r) => (p.Probability, r.Label)).ToList();

static MLPredictionResult Prediction(float probability, float threshold) =>
    new() { PredictedLabel = probability >= threshold, Probability = probability };

static ConfusionResult Confusion(List<(float P, bool Label)> scored, float t)
{
    int tp = 0, fp = 0, tn = 0, fn = 0;
    foreach (var (p, label) in scored)
    {
        var pred = p >= t;
        if (pred && label) tp++; else if (pred) fp++; else if (label) fn++; else tn++;
    }
    double precision = tp + fp == 0 ? 0 : tp / (double)(tp + fp);
    double recall = tp + fn == 0 ? 0 : tp / (double)(tp + fn);
    double f1 = precision + recall == 0 ? 0 : 2 * precision * recall / (precision + recall);
    return new ConfusionResult(t, tp, fp, tn, fn, precision, recall, f1, (tp + tn) / (double)scored.Count);
}

static BankCustomer ToCustomer(BankMarketingData r, int id) => new()
{
    Id = id, Age = (int)r.Age, Job = r.Job, Marital = r.Marital, Education = r.Education, Default = r.Default,
    Balance = (decimal)r.Balance, Housing = r.Housing, Loan = r.Loan, Contact = r.Contact, Day = (int)r.Day,
    Month = r.Month, Duration = (int)r.Duration, Campaign = (int)r.Campaign, PDays = (int)r.PDays,
    Previous = (int)r.Previous, POutcome = r.POutcome, SubscribedToTerm = r.Label
};

static BankMarketingData Clone(BankMarketingData r) => new()
{
    Age = r.Age, Job = r.Job, Marital = r.Marital, Education = r.Education, Default = r.Default, Balance = r.Balance,
    Housing = r.Housing, Loan = r.Loan, Contact = r.Contact, Day = r.Day, Month = r.Month, Duration = r.Duration,
    Campaign = r.Campaign, PDays = r.PDays, Previous = r.Previous, POutcome = r.POutcome, Label = r.Label
};

static double Std(double[] v)
{
    var mean = v.Average();
    return Math.Sqrt(v.Sum(x => (x - mean) * (x - mean)) / (v.Length - 1));
}

static double Percentile(List<double> v, double q)
{
    var sorted = v.OrderBy(x => x).ToList();
    return sorted[(int)Math.Ceiling(q * sorted.Count) - 1];
}

record ConfusionResult(float Threshold, int Tp, int Fp, int Tn, int Fn,
                       double Precision, double Recall, double F1, double Accuracy);

sealed class FixedRuleRepository : IBusinessRuleRepository
{
    private readonly List<BusinessRule> _rules;
    public FixedRuleRepository(List<BusinessRule> rules) => _rules = rules;
    public Task<List<BusinessRule>> GetActiveRulesAsync() => Task.FromResult(_rules);
    public Task<BusinessRule> GetRuleByIdAsync(int id) => Task.FromResult(_rules.First(r => r.Id == id));
    public Task SaveRuleAsync(BusinessRule rule) => throw new NotSupportedException();
    public Task UpdateRuleAsync(BusinessRule rule) => throw new NotSupportedException();
    public Task DeleteRuleAsync(int id) => throw new NotSupportedException();
}
