namespace HybridDecisionIntelligence.Domain.Entities
{
    /// <summary>
    /// Represents a bank customer with demographic and financial attributes
    /// Maps to UCI Bank Marketing dataset
    /// </summary>
    public class BankCustomer
    {
        public int Id { get; set; }
        public int Age { get; set; }
        public string Job { get; set; } = string.Empty;
        public string Marital { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public string Default { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Housing { get; set; } = string.Empty;
        public string Loan { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public int Day { get; set; }
        public string Month { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int Campaign { get; set; }
        public int PDays { get; set; }
        public int Previous { get; set; }
        public string POutcome { get; set; } = string.Empty;
        public bool SubscribedToTerm { get; set; }
        
        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
