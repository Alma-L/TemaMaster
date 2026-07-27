using HybridDecisionIntelligence.Domain.Entities;

namespace HybridDecisionIntelligence.Application.Services
{
    /// <summary>
    /// Generates a formal, printable PDF report for a single hybrid decision,
    /// supporting the audit/compliance (XAI) requirement that every automated
    /// decision must be explainable to bank staff, not just logged.
    /// </summary>
    public interface IDecisionReportGenerator
    {
        byte[] GenerateDecisionReportPdf(HybridDecision decision, BankCustomer? customer);
    }
}
