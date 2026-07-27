namespace HybridDecisionIntelligence.Domain.Entities
{
    /// <summary>
    /// Represents the ML prediction result with confidence score
    /// </summary>
    public class MLPredictionResult
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public bool PredictedLabel { get; set; }
        public float Score { get; set; }
        public float Probability { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a business rule that can override ML predictions
    /// </summary>
    public class BusinessRule
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MinBalance { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public List<string> AllowedJobs { get; set; } = new();
        public decimal MaxInterestRate { get; set; }
        public decimal MinInterestRate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents a hybrid decision combining ML and business rules
    /// </summary>
    public class HybridDecision
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int MLPredictionResultId { get; set; }
        public bool MLPredicted { get; set; }
        public float MLConfidence { get; set; }
        public bool FinalDecision { get; set; }
        public string AuditTrail { get; set; } = string.Empty;
        public decimal ApprovedInterestRate { get; set; }
        public string RulesApplied { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool WasOverridden { get; set; }
        public string OverrideReason { get; set; } = string.Empty;
    }
}
