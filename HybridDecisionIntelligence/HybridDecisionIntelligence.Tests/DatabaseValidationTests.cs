using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Infrastructure.Data;

namespace HybridDecisionIntelligence.Tests
{
    /// <summary>
    /// Phase 5 Database Validation Tests
    /// Ensures DecisionHistory correctly stores AuditLog with XAI requirements
    /// </summary>
    public class DatabaseValidationTests : IDisposable
    {
        private readonly HybridDecisionContext _context;

        public DatabaseValidationTests()
        {
            // Use in-memory SQLite for testing
            var options = new DbContextOptionsBuilder<HybridDecisionContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

            _context = new HybridDecisionContext(options);
            _context.Database.EnsureCreated();
        }

        /// <summary>
        /// Test: HybridDecision entity can be saved to database with complete audit trail
        /// </summary>
        [Fact]
        public void SaveHybridDecision_WithCompleteAuditTrail_ShouldPersistCorrectly()
        {
            // Arrange
            var customerId = 1;
            var decision = new HybridDecision
            {
                CustomerId = customerId,
                MLPredictionResultId = 1,
                MLPredicted = true,
                MLConfidence = 0.85f,
                FinalDecision = false,
                WasOverridden = true,
                AuditTrail = "ML Prediction: APPROVE (Confidence: 85%) | Business Rules Evaluation: FAIL | Override Reason: Interest Rate < 2.5%",
                ApprovedInterestRate = 0.023m,
                RulesApplied = "Low Interest Rate Rule, Age Eligibility Rule",
                OverrideReason = "Calculated interest rate 2.3% is below minimum threshold of 2.5%",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            _context.HybridDecisions.Add(decision);
            var result = _context.SaveChanges();

            // Assert
            result.Should().Be(1, "One record should be inserted");
            
            var retrievedDecision = _context.HybridDecisions
                .FirstOrDefault(d => d.CustomerId == customerId);
            
            retrievedDecision.Should().NotBeNull();
            retrievedDecision!.AuditTrail.Should().NotBeEmpty();
            retrievedDecision.AuditTrail.Should().Contain("ML Prediction");
            retrievedDecision.AuditTrail.Should().Contain("Business Rules");
            retrievedDecision.WasOverridden.Should().BeTrue();
            retrievedDecision.OverrideReason.Should().NotBeEmpty();
        }

        /// <summary>
        /// Test: AuditTrail field correctly stores XAI explanation details
        /// Verifies that all required XAI components are persisted
        /// </summary>
        [Fact]
        public void AuditTrail_ShouldContainCompleteXAI_Explanation()
        {
            // Arrange
            var auditTrail = "Thought: ML probability=78%; " +
                           "Action: Business Rules Evaluated; " +
                           "Observation: Risk Level=HIGH; " +
                           "Interest Rate=3.50%; " +
                           "Final Decision=APPROVED";

            var decision = new HybridDecision
            {
                CustomerId = 2,
                MLPredictionResultId = 2,
                MLPredicted = false,
                MLConfidence = 0.78f,
                FinalDecision = true,
                WasOverridden = false,
                AuditTrail = auditTrail,
                ApprovedInterestRate = 0.035m,
                RulesApplied = "All Rules Passed",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            _context.HybridDecisions.Add(decision);
            _context.SaveChanges();

            var retrieved = _context.HybridDecisions.First(d => d.CustomerId == 2);

            // Assert
            retrieved.AuditTrail.Should().Contain("Thought:");
            retrieved.AuditTrail.Should().Contain("Action:");
            retrieved.AuditTrail.Should().Contain("Observation:");
            retrieved.AuditTrail.Should().Contain("Final Decision");
        }

        /// <summary>
        /// Test: Multiple decisions for same customer are stored with separate audit trails
        /// Validates decision history for XAI requirement traceability
        /// </summary>
        [Fact]
        public void MultipleDecisions_SameCoreCustomer_ShouldMaintainSeparateAuditTrails()
        {
            // Arrange
            var customerId = 3;
            var decision1 = new HybridDecision
            {
                CustomerId = customerId,
                MLPredictionResultId = 3,
                MLPredicted = true,
                MLConfidence = 0.75f,
                FinalDecision = true,
                AuditTrail = "First decision audit trail - APPROVED",
                ApprovedInterestRate = 0.04m,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            };

            var decision2 = new HybridDecision
            {
                CustomerId = customerId,
                MLPredictionResultId = 4,
                MLPredicted = false,
                MLConfidence = 0.55f,
                FinalDecision = false,
                AuditTrail = "Second decision audit trail - REJECTED",
                ApprovedInterestRate = 0.0m,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            _context.HybridDecisions.Add(decision1);
            _context.HybridDecisions.Add(decision2);
            _context.SaveChanges();

            var decisions = _context.HybridDecisions
                .Where(d => d.CustomerId == customerId)
                .OrderBy(d => d.CreatedAt)
                .ToList();

            // Assert
            decisions.Should().HaveCount(2);
            decisions[0].AuditTrail.Should().Contain("First decision");
            decisions[1].AuditTrail.Should().Contain("Second decision");
            decisions[0].CreatedAt.Should().BeBefore(decisions[1].CreatedAt);
        }

        /// <summary>
        /// Test: Database correctly stores override flag and reason
        /// XAI Requirement: Transparency in decision overrides
        /// </summary>
        [Fact]
        public void Override_Information_ShouldBePersisted()
        {
            // Arrange
            var decision = new HybridDecision
            {
                CustomerId = 4,
                MLPredictionResultId = 5,
                MLPredicted = true,
                FinalDecision = false,
                WasOverridden = true,
                OverrideReason = "ML predicted APPROVE but business rules require REJECT due to low interest rate threshold",
                AuditTrail = "Override applied: Interest rate calculation below 2.5% minimum",
                ApprovedInterestRate = 0m,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            _context.HybridDecisions.Add(decision);
            _context.SaveChanges();

            var retrieved = _context.HybridDecisions.First(d => d.CustomerId == 4);

            // Assert
            retrieved.WasOverridden.Should().BeTrue();
            retrieved.OverrideReason.Should().NotBeEmpty();
            retrieved.OverrideReason.Should().Contain("Interest rate");
        }

        /// <summary>
        /// Test: Interest rate is correctly stored with proper precision
        /// </summary>
        [Fact]
        public void ApprovedInterestRate_ShouldBeStoredWithPrecision()
        {
            // Arrange
            var interestRates = new[] { 0.025m, 0.0345m, 0.04999m, 0.055m };
            var customerId = 5;

            foreach (var (rate, index) in interestRates.Select((r, i) => (r, i)))
            {
                var decision = new HybridDecision
                {
                    CustomerId = customerId + index,
                    MLPredictionResultId = 6 + index,
                    MLPredicted = true,
                    FinalDecision = true,
                    ApprovedInterestRate = rate,
                    AuditTrail = $"Interest rate: {rate:P2}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.HybridDecisions.Add(decision);
            }
            _context.SaveChanges();

            // Act
            var retrieved = _context.HybridDecisions
                .Where(d => d.CustomerId >= customerId && d.CustomerId < customerId + 4)
                .OrderBy(d => d.CustomerId)
                .ToList();

            // Assert
            retrieved.Should().HaveCount(4);
            retrieved[0].ApprovedInterestRate.Should().Be(0.025m);
            retrieved[1].ApprovedInterestRate.Should().Be(0.0345m);
            retrieved[2].ApprovedInterestRate.Should().Be(0.04999m);
            retrieved[3].ApprovedInterestRate.Should().Be(0.055m);
        }

        /// <summary>
        /// Test: Decision timestamps are recorded correctly
        /// XAI Requirement: Audit trails with timestamps for compliance
        /// </summary>
        [Fact]
        public void DecisionTimestamp_ShouldBeRecordedAccurately()
        {
            // Arrange
            var beforeSave = DateTime.UtcNow;
            var decision = new HybridDecision
            {
                CustomerId = 6,
                MLPredictionResultId = 10,
                MLPredicted = true,
                FinalDecision = true,
                AuditTrail = "Decision recorded",
                ApprovedInterestRate = 0.04m
            };

            // Act
            _context.HybridDecisions.Add(decision);
            _context.SaveChanges();
            var afterSave = DateTime.UtcNow;

            var retrieved = _context.HybridDecisions.First(d => d.CustomerId == 6);

            // Assert
            retrieved.CreatedAt.Should().BeOnOrAfter(beforeSave);
            retrieved.CreatedAt.Should().BeOnOrBefore(afterSave.AddSeconds(1));
        }

        /// <summary>
        /// Test: Large audit trail strings are persisted correctly
        /// Validates that detailed explanations don't get truncated
        /// </summary>
        [Fact]
        public void LargeAuditTrail_ShouldBePersisted_WithoutTruncation()
        {
            // Arrange
            var largeAuditTrail = string.Concat(
                "Step 1: ML Analysis [COMPLETE] - ",
                string.Join("; ", Enumerable.Range(1, 50).Select(i => $"Rule {i} evaluated={i % 2 == 0}"))
            );

            var decision = new HybridDecision
            {
                CustomerId = 7,
                MLPredictionResultId = 11,
                MLPredicted = true,
                FinalDecision = true,
                AuditTrail = largeAuditTrail,
                ApprovedInterestRate = 0.04m,
                CreatedAt = DateTime.UtcNow
            };

            // Act
            _context.HybridDecisions.Add(decision);
            _context.SaveChanges();

            var retrieved = _context.HybridDecisions.First(d => d.CustomerId == 7);

            // Assert
            retrieved.AuditTrail.Length.Should().Be(largeAuditTrail.Length, "Audit trail should not be truncated");
            retrieved.AuditTrail.Should().Contain("Rule 50 evaluated=true");
        }

        /// <summary>
        /// Test: Query for decision history by customer
        /// XAI Requirement: Ability to retrieve audit trail history for compliance/review
        /// </summary>
        [Fact]
        public void QueryDecisionHistory_ByCustomer_ShouldReturnAuditTrails()
        {
            // Arrange
            var customerId = 8;
            for (int i = 0; i < 3; i++)
            {
                var decision = new HybridDecision
                {
                    CustomerId = customerId,
                    MLPredictionResultId = 12 + i,
                    MLPredicted = i % 2 == 0,
                    FinalDecision = i % 2 == 0,
                    AuditTrail = $"Decision {i + 1} audit trail",
                    ApprovedInterestRate = 0.04m,
                    CreatedAt = DateTime.UtcNow.AddMinutes(i)
                };
                _context.HybridDecisions.Add(decision);
            }
            _context.SaveChanges();

            // Act
            var history = _context.HybridDecisions
                .Where(d => d.CustomerId == customerId)
                .OrderByDescending(d => d.CreatedAt)
                .ToList();

            // Assert
            history.Should().HaveCount(3);
            history[0].AuditTrail.Should().Contain("Decision 3");
            history[1].AuditTrail.Should().Contain("Decision 2");
            history[2].AuditTrail.Should().Contain("Decision 1");
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
