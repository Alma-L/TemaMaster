using Microsoft.EntityFrameworkCore;

namespace HybridDecisionIntelligence.Infrastructure.Data
{
    /// <summary>
    /// EnsureCreated() only builds the schema for a brand-new database; it never
    /// alters existing tables. This idempotent script brings a database created by an
    /// earlier version up to the current model (snapshot column, foreign keys).
    /// Safe to run on every startup.
    /// </summary>
    public static class SqlServerSchemaUpgrader
    {
        private static readonly string[] Steps =
        {
            // 1. Immutable customer snapshot on each decision
            @"IF COL_LENGTH('dbo.HybridDecisions', 'CustomerSnapshotJson') IS NULL
                ALTER TABLE dbo.HybridDecisions
                    ADD CustomerSnapshotJson nvarchar(max) NOT NULL
                    CONSTRAINT DF_HybridDecisions_CustomerSnapshotJson DEFAULT N'';",

            // 2. Backfill snapshots for decisions made before the column existed, from the
            //    current customer row (the best information available for old records).
            @"UPDATE d SET CustomerSnapshotJson = (
                  SELECT c.Id, c.Age, c.Job, c.Marital, c.Education, c.[Default], c.Balance,
                         c.Housing, c.Loan, c.Contact, c.[Day], c.[Month], c.Duration,
                         c.Campaign, c.PDays, c.Previous, c.POutcome, c.SubscribedToTerm,
                         c.CreatedAt, c.UpdatedAt
                  FROM dbo.BankCustomers c WHERE c.Id = d.CustomerId
                  FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
              FROM dbo.HybridDecisions d
              WHERE d.CustomerSnapshotJson = N''
                AND EXISTS (SELECT 1 FROM dbo.BankCustomers c WHERE c.Id = d.CustomerId);",

            // 3. Referential integrity
            @"IF OBJECT_ID('dbo.FK_MLPredictionResults_BankCustomers_CustomerId', 'F') IS NULL
                ALTER TABLE dbo.MLPredictionResults
                    ADD CONSTRAINT FK_MLPredictionResults_BankCustomers_CustomerId
                    FOREIGN KEY (CustomerId) REFERENCES dbo.BankCustomers (Id);",

            @"IF OBJECT_ID('dbo.FK_HybridDecisions_BankCustomers_CustomerId', 'F') IS NULL
                ALTER TABLE dbo.HybridDecisions
                    ADD CONSTRAINT FK_HybridDecisions_BankCustomers_CustomerId
                    FOREIGN KEY (CustomerId) REFERENCES dbo.BankCustomers (Id);",

            @"IF NOT EXISTS (SELECT 1 FROM sys.indexes
                             WHERE name = 'IX_HybridDecisions_MLPredictionResultId'
                               AND object_id = OBJECT_ID('dbo.HybridDecisions'))
                CREATE UNIQUE INDEX IX_HybridDecisions_MLPredictionResultId
                    ON dbo.HybridDecisions (MLPredictionResultId);",

            @"IF OBJECT_ID('dbo.FK_HybridDecisions_MLPredictionResults_MLPredictionResultId', 'F') IS NULL
                ALTER TABLE dbo.HybridDecisions
                    ADD CONSTRAINT FK_HybridDecisions_MLPredictionResults_MLPredictionResultId
                    FOREIGN KEY (MLPredictionResultId) REFERENCES dbo.MLPredictionResults (Id);",
        };

        public static void Apply(HybridDecisionContext db)
        {
            if (!db.Database.IsSqlServer()) return;

            foreach (var sql in Steps)
            {
                db.Database.ExecuteSqlRaw(sql);
            }
        }
    }
}
