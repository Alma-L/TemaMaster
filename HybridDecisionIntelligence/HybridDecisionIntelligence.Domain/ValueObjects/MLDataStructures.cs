using Microsoft.ML.Data;

namespace HybridDecisionIntelligence.Domain.ValueObjects
{
    /// <summary>
    /// Data structure for ML.NET training data
    /// </summary>
    public class BankMarketingData
    {
        [LoadColumn(0)] public float Age { get; set; }
        [LoadColumn(1)] public string Job { get; set; } = string.Empty;
        [LoadColumn(2)] public string Marital { get; set; } = string.Empty;
        [LoadColumn(3)] public string Education { get; set; } = string.Empty;
        [LoadColumn(4)] public string Default { get; set; } = string.Empty;
        [LoadColumn(5)] public float Balance { get; set; }
        [LoadColumn(6)] public string Housing { get; set; } = string.Empty;
        [LoadColumn(7)] public string Loan { get; set; } = string.Empty;
        [LoadColumn(8)] public string Contact { get; set; } = string.Empty;
        [LoadColumn(9)] public float Day { get; set; }
        [LoadColumn(10)] public string Month { get; set; } = string.Empty;
        [LoadColumn(11)] public float Duration { get; set; }
        [LoadColumn(12)] public float Campaign { get; set; }
        [LoadColumn(13)] public float PDays { get; set; }
        [LoadColumn(14)] public float Previous { get; set; }
        [LoadColumn(15)] public string POutcome { get; set; } = string.Empty;

        // Label: whether customer subscribed to term deposit (column 16, "y" in the UCI dataset)
        [LoadColumn(16)]
        [ColumnName("Label")]
        public bool Label { get; set; }
    }

    /// <summary>
    /// Output from ML model prediction
    /// </summary>
    public class BankMarketingPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }

        [ColumnName("Score")]
        public float Score { get; set; }

        [ColumnName("Probability")]
        public float Probability { get; set; }
    }
}
