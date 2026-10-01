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
    /// Represents a business rule that can override ML predictions.
    /// A rule checks only the criteria it configures: MinBalance > 0 (balance),
    /// MinAge/MaxAge > 0 (age; MaxAge 0 = no upper bound), a non-empty AllowedJobs
    /// list (job) and RequireNoDefault (default history).
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
        public bool RequireNoDefault { get; set; }
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

        /// <summary>
        /// Immutable JSON copy of the customer profile exactly as it was evaluated.
        /// The BankCustomers row is overwritten on every new request for the same
        /// customer, so audits and reports must read this snapshot instead.
        /// </summary>
        public string CustomerSnapshotJson { get; set; } = string.Empty;
    }
}
