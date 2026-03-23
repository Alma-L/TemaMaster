#r "nuget:Microsoft.ML,5.0.0"
#r "nuget:Microsoft.ML.FastTree,5.0.0"

using System;
using Microsoft.ML;
using Microsoft.ML.Data;

public class BankMarketingData
{
    [LoadColumn(0)] public float Age;
    [LoadColumn(1)] public string Job;
    [LoadColumn(2)] public string Marital;
    [LoadColumn(3)] public string Education;
    [LoadColumn(4)] public string Default;
    [LoadColumn(5)] public float Balance;
    [LoadColumn(6)] public string Housing;
    [LoadColumn(7)] public string Loan;
    [LoadColumn(8)] public string Contact;
    [LoadColumn(9)] public float Day;
    [LoadColumn(10)] public string Month;
    [LoadColumn(11)] public float Duration;
    [LoadColumn(12)] public float Campaign;
    [LoadColumn(13)] public float PDays;
    [LoadColumn(14)] public float Previous;
    [LoadColumn(15)] public string POutcome;
    [LoadColumn(16), ColumnName("Label")] public bool Label;
}

public class BankMarketingPrediction
{
    [ColumnName("PredictedLabel")] public bool Prediction;
    public float Score;
    public float Probability;
}

void Train(string dataPath, string modelPath)
{
    var mlContext = new MLContext(seed: 123);

    var data = mlContext.Data.LoadFromTextFile<BankMarketingData>(dataPath, hasHeader: true, separatorChar: ';');
    var split = mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

    var pipeline = mlContext.Transforms.Categorical.OneHotEncoding(new[]
    {
        new InputOutputColumnPair("JobEncoded", nameof(BankMarketingData.Job)),
        new InputOutputColumnPair("MaritalEncoded", nameof(BankMarketingData.Marital)),
        new InputOutputColumnPair("EducationEncoded", nameof(BankMarketingData.Education)),
        new InputOutputColumnPair("DefaultEncoded", nameof(BankMarketingData.Default)),
        new InputOutputColumnPair("HousingEncoded", nameof(BankMarketingData.Housing)),
        new InputOutputColumnPair("LoanEncoded", nameof(BankMarketingData.Loan)),
        new InputOutputColumnPair("ContactEncoded", nameof(BankMarketingData.Contact)),
        new InputOutputColumnPair("MonthEncoded", nameof(BankMarketingData.Month)),
        new InputOutputColumnPair("POutcomeEncoded", nameof(BankMarketingData.POutcome))
    })
    .Append(mlContext.Transforms.Concatenate("Features", new[] {
        "Age", "Balance", "Day", "Duration", "Campaign", "PDays", "Previous",
        "JobEncoded", "MaritalEncoded", "EducationEncoded", "DefaultEncoded", "HousingEncoded", "LoanEncoded", "ContactEncoded", "MonthEncoded", "POutcomeEncoded"
    }))
    .Append(mlContext.Transforms.NormalizeMinMax("Features"))
    .Append(mlContext.BinaryClassification.Trainers.FastTree(new FastTreeBinaryTrainer.Options
    {
        LabelColumnName = "Label",
        FeatureColumnName = "Features",
        NumberOfTrees = 150,
        NumberOfLeaves = 20,
        MinimumExampleCountPerLeaf = 10
    }));

    var model = pipeline.Fit(split.TrainSet);

    var predictions = model.Transform(split.TestSet);
    var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

    Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");
    Console.WriteLine($"AUC: {metrics.AreaUnderRocCurve:P2}");
    Console.WriteLine($"F1 Score: {metrics.F1Score:P2}");

    mlContext.Model.Save(model, data.Schema, modelPath);
    Console.WriteLine($"Model saved to {modelPath}");
}

var dataPath = @"Data/Bank/bank.csv";
var modelPath = @"Models/BankMarketingModel.zip";
Train(dataPath, modelPath);
