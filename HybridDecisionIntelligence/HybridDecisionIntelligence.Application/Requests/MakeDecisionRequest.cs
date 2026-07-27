using MediatR;

namespace HybridDecisionIntelligence.Application.Requests
{
    /// <summary>
    /// Request to make a hybrid decision for a customer
    /// Combines ML prediction with business rule evaluation
    /// </summary>
    public class MakeDecisionRequest : IRequest<MakeDecisionResponse>
    {
        public int CustomerId { get; set; }
        public int Age { get; set; }
        public required string Job { get; set; }
        public required string Marital { get; set; }
        public required string Education { get; set; }
        public decimal Balance { get; set; }
        public required string Housing { get; set; }
        public required string Loan { get; set; }
        public string Default { get; set; } = "no";
        public int Duration { get; set; }
        public int Campaign { get; set; }
        public int Previous { get; set; }

        // Optional context features the ML model was trained on. When omitted,
        // they default to values meaning "no prior contact" so single manual
        // evaluations still work without requiring every UCI dataset column.
        public string Contact { get; set; } = "unknown";
        public int Day { get; set; } = 1;
        public string Month { get; set; } = "may";
        public int PDays { get; set; } = -1;
        public string POutcome { get; set; } = "unknown";
    }

    /// <summary>
    /// Response containing the final decision and audit trail
    /// Includes explainable AI information
    /// </summary>
    public class MakeDecisionResponse
    {
        public int CustomerId { get; set; }
        public bool MLPrediction { get; set; }
        public float MLConfidence { get; set; }
        public bool FinalDecision { get; set; }
        public decimal ApprovedInterestRate { get; set; }
        public bool WasOverridden { get; set; }
        public string AuditTrail { get; set; } = string.Empty;
        public List<string> AppliedRules { get; set; } = new();
        public DateTime DecisionTime { get; set; } = DateTime.UtcNow;
    }
}
