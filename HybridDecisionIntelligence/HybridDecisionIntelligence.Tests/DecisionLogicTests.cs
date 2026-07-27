using Xunit;
using Moq;
using FluentAssertions;
using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace HybridDecisionIntelligence.Tests
{
    /// <summary>
    /// Phase 5 Integration Tests - Validation Phase
    /// Tests hybrid decision logic with ML predictions and business rule overrides
    /// Includes XAI audit trail validation and performance metrics
    /// </summary>
    public class DecisionLogicTests
    {
        private readonly Mock<IDecisionRepository> _mockDecisionRepository;
        private readonly Mock<IBusinessRuleEngine> _mockRuleEngine;
        private readonly Mock<ILogger<DecisionEngine>> _mockLogger;
        private readonly DecisionEngine _decisionEngine;

        public DecisionLogicTests()
        {
            _mockDecisionRepository = new Mock<IDecisionRepository>();
            _mockRuleEngine = new Mock<IBusinessRuleEngine>();
            _mockLogger = new Mock<ILogger<DecisionEngine>>();

            _decisionEngine = new DecisionEngine(
                _mockRuleEngine.Object,
                _mockDecisionRepository.Object,
                _mockLogger.Object
            );

            _mockDecisionRepository
                .Setup(m => m.SaveDecisionAsync(It.IsAny<HybridDecision>()))
                .Returns(Task.CompletedTask);
        }

        private static BankCustomer BuildCustomer(int customerId, int age = 35, decimal balance = 5000m) =>
            new BankCustomer
            {
                Id = customerId,
                Age = age,
                Job = "management",
                Marital = "married",
                Education = "tertiary",
                Balance = balance,
                Housing = "yes",
                Loan = "no",
                Duration = 150,
                Campaign = 1,
                Previous = 0,
                Default = "no",
                Contact = "cellular",
                Day = 15,
                Month = "mar",
                PDays = -1,
                POutcome = "unknown"
            };

        #region Scenario A: ML Predicts Success but Business Rules Override with REJECT

        /// <summary>
        /// Scenario A - Test Case 1:
        /// ML Prediction: SUCCESS (Approve) with 85% confidence
        /// Business Rules: REJECT (customer fails the Minimum Balance Rule)
        /// Expected: Final Decision = REJECT, WasOverridden = true
        /// XAI Requirement: AuditTrail contains override reason
        /// </summary>
        [Fact]
        public async Task Scenario_A_MLApproveButRuleReject_ShouldOverrideWithReject()
        {
            // Arrange
            var customer = BuildCustomer(1001);
            var mlResult = new MLPredictionResult
            {
                Id = 101,
                CustomerId = customer.Id,
                PredictedLabel = true, // ML Says: APPROVE
                Score = 0.85f,
                Probability = 0.85f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleResult
            {
                IsApproved = false,
                FailedRules = new() { "Minimum Balance Rule" },
                RiskLevel = "High"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);
            await _decisionEngine.SaveDecisionAsync(decision);

            // Assert
            decision.MLPredicted.Should().BeTrue("ML predicted APPROVE");
            decision.FinalDecision.Should().BeFalse("business rules rejected the decision");
            decision.WasOverridden.Should().BeTrue("decision was overridden by business rules");
            decision.AuditTrail.Should().Contain("OVERRIDE APPLIED", "audit trail must document override");
            decision.AuditTrail.Should().Contain("Minimum Balance Rule", "audit trail must contain failed rule");
            decision.OverrideReason.Should().Contain("Minimum Balance Rule");
            decision.ApprovedInterestRate.Should().BeInRange(0.02m, 0.12m, "interest rate is always clamped to this range");

            // XAI Verification
            decision.AuditTrail.Should().Contain("ML Prediction: APPROVE", "XAI requires ML decision to be documented");
            decision.AuditTrail.Should().Contain("Business Rules Evaluation: FAIL", "XAI requires business rule result");

            _mockDecisionRepository.Verify(m => m.SaveDecisionAsync(It.IsAny<HybridDecision>()), Times.Once);
        }

        /// <summary>
        /// Scenario A - Test Case 2:
        /// ML Prediction: SUCCESS with 92% confidence
        /// Business Rules: FAIL on Age Eligibility (customer too young)
        /// Expected: Final Decision = REJECT, audit trail shows age rule violation
        /// </summary>
        [Fact]
        public async Task Scenario_A_MLApproveButAgeRuleFails_ShouldOverrideWithReject()
        {
            // Arrange
            var customer = BuildCustomer(1002, age: 18, balance: 50000m);

            var mlResult = new MLPredictionResult
            {
                Id = 102,
                CustomerId = customer.Id,
                PredictedLabel = true, // ML Says: APPROVE
                Score = 0.92f,
                Probability = 0.92f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleResult
            {
                IsApproved = false,
                FailedRules = new() { "Age Eligibility Rule" },
                RiskLevel = "Medium"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert
            decision.MLPredicted.Should().BeTrue();
            decision.FinalDecision.Should().BeFalse();
            decision.WasOverridden.Should().BeTrue();
            decision.RulesApplied.Should().BeEmpty("no rules passed in this scenario");
            decision.AuditTrail.Should().Contain("Age Eligibility Rule");
        }

        #endregion

        #region Scenario B: ML Predicts Failure - System Logs Observation Phase Correctly

        /// <summary>
        /// Scenario B - Test Case 1:
        /// ML Prediction: FAILURE (Reject) with 78% confidence
        /// Business Rules: PASS (all rules satisfied)
        /// Expected: Final Decision = REJECT (ML prediction trumps when rules pass)
        /// XAI Requirement: Observation phase logged with alignment message
        /// </summary>
        [Fact]
        public async Task Scenario_B_MLRejectButRulesPass_ShouldLogObservationPhase()
        {
            // Arrange
            var customer = BuildCustomer(2001, age: 45, balance: 2000m);

            var mlResult = new MLPredictionResult
            {
                Id = 201,
                CustomerId = customer.Id,
                PredictedLabel = false, // ML Says: REJECT
                Score = 0.22f,
                Probability = 0.22f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleResult
            {
                IsApproved = true,
                AppliedRules = new() { "Minimum Balance Rule", "No Default History" },
                RiskLevel = "Low"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);
            await _decisionEngine.SaveDecisionAsync(decision);

            // Assert - ML prediction drives final decision when rules pass
            decision.MLPredicted.Should().BeFalse("ML predicted REJECT");
            decision.FinalDecision.Should().BeFalse("final decision should be REJECT");
            decision.WasOverridden.Should().BeFalse("rules agree with the ML rejection, so nothing was overridden");

            // XAI Observation Phase Verification
            decision.AuditTrail.Should().Contain("ML Prediction: REJECT", "observation: ML prediction phase documented");
            decision.AuditTrail.Should().Contain("Business Rules Evaluation: PASS", "observation: business rule phase documented");
            decision.AuditTrail.Should().Contain("Confidence: " + mlResult.Probability.ToString("P2"), "observation: ML confidence logged");

            _mockDecisionRepository.Verify(m => m.SaveDecisionAsync(It.IsAny<HybridDecision>()), Times.Once);
        }

        /// <summary>
        /// Scenario B - Test Case 2:
        /// ML Prediction: FAILURE with 88% confidence
        /// Business Rules: PASS
        /// Expected: Final Decision = REJECT, audit trail shows both ML and rules evaluation
        /// </summary>
        [Fact]
        public async Task Scenario_B_MLRejectHighConfidenceRulesPass_ShouldLogCompleteObservation()
        {
            // Arrange
            var customer = BuildCustomer(2002, age: 60, balance: 100m);

            var mlResult = new MLPredictionResult
            {
                Id = 202,
                CustomerId = customer.Id,
                PredictedLabel = false, // ML Says: REJECT (high confidence)
                Score = 0.88f,
                Probability = 0.88f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleResult
            {
                IsApproved = true,
                AppliedRules = new() { "Minimum Balance Rule" },
                RiskLevel = "Medium"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert
            decision.MLPredicted.Should().BeFalse();
            decision.FinalDecision.Should().BeFalse();
            decision.MLConfidence.Should().Be(0.88f);

            // Complete observation logging
            decision.AuditTrail.Should().Contain("88", "observation must include ML confidence percentage");
            decision.AuditTrail.Should().Contain("Interest Rate", "observation must include interest rate calculation");
        }

        #endregion

        #region Cross-Scenario Tests: Business Logic Alignment

        /// <summary>
        /// Test that aligned decisions (no override) have correct audit trail
        /// </summary>
        [Fact]
        public async Task BothApprove_ShouldNotOverrideAndLogAlignment()
        {
            // Arrange
            var customer = BuildCustomer(3001, age: 40, balance: 100000m);

            var mlResult = new MLPredictionResult
            {
                Id = 301,
                CustomerId = customer.Id,
                PredictedLabel = true, // ML Approves
                Score = 0.95f,
                Probability = 0.95f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleResult
            {
                IsApproved = true, // Rules Approve
                AppliedRules = new() { "Minimum Balance Rule", "Age Eligibility Rule", "No Default History" },
                RiskLevel = "Low"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert
            decision.MLPredicted.Should().BeTrue();
            decision.FinalDecision.Should().BeTrue();
            decision.WasOverridden.Should().BeFalse("no override needed when both approve");
            decision.AuditTrail.Should().Contain("ML prediction and business rules are aligned");
        }

        /// <summary>
        /// Test that both rejecting shows no override but logged correctly
        /// </summary>
        [Fact]
        public async Task BothReject_ShouldNotOverrideAndLogBothFail()
        {
            // Arrange
            var customer = BuildCustomer(3002, age: 25, balance: 0m);

            var mlResult = new MLPredictionResult
            {
                Id = 302,
                CustomerId = customer.Id,
                PredictedLabel = false, // ML Rejects
                Score = 0.15f,
                Probability = 0.15f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleResult
            {
                IsApproved = false, // Rules Reject
                FailedRules = new() { "Minimum Balance Rule", "No Default History" },
                RiskLevel = "High"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert
            decision.MLPredicted.Should().BeFalse();
            decision.FinalDecision.Should().BeFalse();
            decision.WasOverridden.Should().BeFalse("no override when both reject");
            decision.OverrideReason.Should().BeEmpty("nothing was overridden, so there is no override reason");
            decision.AuditTrail.Should().Contain("ML Prediction: REJECT");
            decision.AuditTrail.Should().Contain("Business Rules Evaluation: FAIL");
            decision.AuditTrail.Should().Contain("ML prediction and business rules are aligned");
        }

        #endregion

        #region XAI (Explainable AI) Audit Trail Tests

        /// <summary>
        /// Verify that all XAI requirements are met:
        /// - Decision timestamp is recorded
        /// - ML prediction with confidence is documented
        /// - All applied/failed rules are listed
        /// - Interest rate is documented
        /// </summary>
        [Fact]
        public async Task XAIAuditTrail_ShouldContainAllRequiredElements()
        {
            // Arrange
            var customer = BuildCustomer(4001, age: 50, balance: 75000m);

            var mlResult = new MLPredictionResult
            {
                Id = 401,
                CustomerId = customer.Id,
                PredictedLabel = true,
                Score = 0.80f,
                Probability = 0.80f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleResult
            {
                IsApproved = true,
                AppliedRules = new() { "Age Eligibility Rule", "Minimum Balance Rule", "No Default History" },
                RiskLevel = "Low"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert XAI Requirements
            // 1. Decision timestamp
            decision.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // 2. ML Prediction with confidence
            decision.AuditTrail.Should().Contain("ML Prediction");
            decision.AuditTrail.Should().Contain(mlResult.Probability.ToString("P2"));

            // 3. Applied rules
            decision.RulesApplied.Should().NotBeEmpty();
            decision.RulesApplied.Should().Contain("Age Eligibility Rule");
            decision.RulesApplied.Should().Contain("Minimum Balance Rule");

            // 4. Business rules evaluation result
            decision.AuditTrail.Should().Contain("Business Rules Evaluation");

            // 5. Interest rate calculation
            decision.AuditTrail.Should().Contain("Interest Rate");
            decision.ApprovedInterestRate.Should().BeGreaterThan(0);
            decision.ApprovedInterestRate.Should().BeLessThan(1); // Should be a percentage
        }

        #endregion
    }
}
