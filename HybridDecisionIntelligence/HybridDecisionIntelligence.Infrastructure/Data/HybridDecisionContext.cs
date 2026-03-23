using HybridDecisionIntelligence.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HybridDecisionIntelligence.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework Core DbContext for the Hybrid Decision Intelligence system
    /// Manages all database operations with SQL Server
    /// </summary>
    public class HybridDecisionContext : DbContext
    {
        public HybridDecisionContext(DbContextOptions<HybridDecisionContext> options) : base(options)
        {
        }

        public DbSet<BankCustomer> BankCustomers { get; set; }
        public DbSet<MLPredictionResult> MLPredictionResults { get; set; }
        public DbSet<BusinessRule> BusinessRules { get; set; }
        public DbSet<HybridDecision> HybridDecisions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // BankCustomer configuration
            modelBuilder.Entity<BankCustomer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Age).IsRequired();
                entity.Property(e => e.Job).HasMaxLength(20);
                entity.Property(e => e.Marital).HasMaxLength(20);
                entity.Property(e => e.Education).HasMaxLength(30);
                entity.Property(e => e.Default).HasMaxLength(5);
                entity.Property(e => e.Balance).HasPrecision(18, 2);
                entity.Property(e => e.Housing).HasMaxLength(5);
                entity.Property(e => e.Loan).HasMaxLength(5);
                entity.Property(e => e.Contact).HasMaxLength(20);
                entity.Property(e => e.Month).HasMaxLength(10);
                entity.Property(e => e.POutcome).HasMaxLength(20);
                entity.HasIndex(e => e.CreatedAt);
            });

            // MLPredictionResult configuration
            modelBuilder.Entity<MLPredictionResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerId).IsRequired();
                entity.Property(e => e.PredictedLabel).IsRequired();
                entity.Property(e => e.Score).IsRequired();
                entity.Property(e => e.Probability).IsRequired();
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // BusinessRule configuration
            modelBuilder.Entity<BusinessRule>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.MinBalance).HasPrecision(18, 2);
                entity.Property(e => e.MaxInterestRate).HasPrecision(5, 4);
                entity.Property(e => e.MinInterestRate).HasPrecision(5, 4);
                entity.Property(e => e.IsActive).IsRequired();
                entity.HasIndex(e => e.IsActive);
            });

            // HybridDecision configuration
            modelBuilder.Entity<HybridDecision>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerId).IsRequired();
                entity.Property(e => e.MLPredictionResultId).IsRequired();
                entity.Property(e => e.MLConfidence).IsRequired();
                entity.Property(e => e.FinalDecision).IsRequired();
                entity.Property(e => e.AuditTrail).HasMaxLength(2000);
                entity.Property(e => e.RulesApplied).HasMaxLength(500);
                entity.Property(e => e.ApprovedInterestRate).HasPrecision(5, 4);
                entity.Property(e => e.OverrideReason).HasMaxLength(500);
                entity.HasIndex(e => new { e.CustomerId, e.CreatedAt });
                entity.HasIndex(e => e.FinalDecision);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Seed default business rules
            SeedBusinessRules(modelBuilder);
        }

        private static void SeedBusinessRules(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BusinessRule>().HasData(
                new BusinessRule
                {
                    Id = 1,
                    Name = "Minimum Balance Rule",
                    Description = "Customer must have minimum balance of €1000",
                    MinBalance = 1000,
                    MinAge = 18,
                    MaxAge = 100,
                    MaxInterestRate = 0.12m,
                    MinInterestRate = 0.02m,
                    IsActive = true
                },
                new BusinessRule
                {
                    Id = 2,
                    Name = "Age Eligibility Rule",
                    Description = "Customer must be between 25 and 70 years old",
                    MinBalance = 0,
                    MinAge = 25,
                    MaxAge = 70,
                    MaxInterestRate = 0.12m,
                    MinInterestRate = 0.02m,
                    IsActive = true
                },
                new BusinessRule
                {
                    Id = 3,
                    Name = "No Default History",
                    Description = "Customer must not have previous default",
                    MinBalance = 0,
                    MinAge = 18,
                    MaxAge = 100,
                    MaxInterestRate = 0.12m,
                    MinInterestRate = 0.02m,
                    IsActive = true
                }
            );
        }
    }
}
