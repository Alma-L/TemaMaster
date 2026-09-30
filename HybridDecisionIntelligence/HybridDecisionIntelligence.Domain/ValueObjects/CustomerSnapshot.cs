using System.Text.Json;
using HybridDecisionIntelligence.Domain.Entities;

namespace HybridDecisionIntelligence.Domain.ValueObjects
{
    /// <summary>
    /// Serializes the customer profile stored on each HybridDecision, so the
    /// audit record keeps the exact input the ML model and rules evaluated.
    /// </summary>
    public static class CustomerSnapshot
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        public static string Serialize(BankCustomer customer) =>
            JsonSerializer.Serialize(customer, Options);

        public static BankCustomer? Deserialize(string? json) =>
            string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<BankCustomer>(json, Options);
    }
}
