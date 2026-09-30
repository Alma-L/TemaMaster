using HybridDecisionIntelligence.Domain.Entities;

namespace HybridDecisionIntelligence.Application.Services
{
    /// <summary>
    /// Operations over the UCI Bank Marketing CSV: bulk import through the hybrid
    /// pipeline, what-if simulation of the reference rate, and raw customer rows.
    /// </summary>
    public interface IDatasetService
    {
        /// <summary>Run every CSV row through the pipeline and store the decisions</summary>
        Task<DatasetImportResult> ImportAsync(int? limit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Re-decide the dataset under different reference rates without storing anything.
        /// The ML predictions are computed once; only the decision layer varies.
        /// </summary>
        Task<List<ReferenceRateScenario>> SimulateReferenceRatesAsync(
            IReadOnlyList<decimal> referenceRates, int? limit = null, CancellationToken cancellationToken = default);

        /// <summary>CSV rows mapped to customers (Id = offset + row number)</summary>
        IReadOnlyList<BankCustomer> LoadCustomers(int? limit = null);
    }

    public class DatasetImportResult
    {
        public int RowsInDataset { get; set; }
        public int Imported { get; set; }
        public int SkippedAlreadyImported { get; set; }
        public int Approved { get; set; }
        public int Overridden { get; set; }
        public double ElapsedSeconds { get; set; }

        /// <summary>Customer Id = CustomerIdOffset + row number (1-based) in the CSV</summary>
        public int CustomerIdOffset { get; set; }
    }

    /// <summary>Outcome of the whole dataset under one reference rate</summary>
    public class ReferenceRateScenario
    {
        public decimal ReferenceRate { get; set; }
        public int Customers { get; set; }
        public int AiApproved { get; set; }
        public int Approved { get; set; }
        public int Overridden { get; set; }

        /// <summary>Overrides where the interest-rate policy rule failed (possibly with other rules)</summary>
        public int OverriddenByRatePolicy { get; set; }

        /// <summary>Real subscribers (y = yes) among the approved customers</summary>
        public int ApprovedRealSubscribers { get; set; }

        public decimal AverageOfferedRateApproved { get; set; }
    }
}
