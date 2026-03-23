using Microsoft.ML.Data;

namespace HybridDecisionIntelligence.Domain.ValueObjects
{
    /// <summary>
    /// Data structure for ML.NET training data
    /// </summary>
    public class BankMarketingData
    {
        public float Age { get; set; }
        public string Job { get; set; } = string.Empty;
        public string Marital { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string Default { get; set; } = string.Empty;
        public float Balance { get; set; }
        public string Housing { get; set; } = string.Empty;
        public string Loan { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public float Day { get; set; }
        public string Month { get; set; } = string.Empty;
        public float Duration { get; set; }
        public float Campaign { get; set; }
        public float PDays { get; set; }
        public float Previous { get; set; }
        public string POutcome { get; set; } = string.Empty;
        
        // Label: whether customer subscribed to term deposit
        [LoadColumn(17)]
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
        public float[] Score { get; set; } = Array.Empty<float>();

        [ColumnName("Probability")]
        public float Probability { get; set; }
    }
}
