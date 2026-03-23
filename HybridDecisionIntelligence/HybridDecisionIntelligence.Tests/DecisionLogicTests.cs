using Xunit;
using Moq;
using FluentAssertions;
using HybridDecisionIntelligence.Domain.Entities;
using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Application.Repositories;
using HybridDecisionIntelligence.Application.Handlers;
using HybridDecisionIntelligence.Application.Requests;
using Microsoft.Extensions.Logging;

namespace HybridDecisionIntelligence.Tests
{
    /// <summary>
    /// Phase 5 Integration Tests - Validation Phase
    /// Tests hybrid decision logic with ML predictions and business rule overrides
    /// Includes XAI audit trail validation and performance metrics
    /// </summary>
    public class DecisionLogicTests : IDisposable
    {
        private readonly Mock<IDecisionRepository> _mockDecisionRepository;
        private readonly Mock<IBusinessRuleRepository> _mockRuleRepository;
        private readonly Mock<IBusinessRuleEngine> _mockRuleEngine;
        private readonly Mock<IMLPredictor> _mockMLPredictor;
        private readonly Mock<ILogger<DecisionEngine>> _mockLogger;
        private readonly DecisionEngine _decisionEngine;

        public DecisionLogicTests()
        {
            _mockDecisionRepository = new Mock<IDecisionRepository>();
            _mockRuleRepository = new Mock<IBusinessRuleRepository>();
            _mockRuleEngine = new Mock<IBusinessRuleEngine>();
            _mockMLPredictor = new Mock<IMLPredictor>();
            _mockLogger = new Mock<ILogger<DecisionEngine>>();

            _decisionEngine = new DecisionEngine(
                _mockRuleEngine.Object,
                _mockDecisionRepository.Object,
                _mockLogger.Object
            );
        }

        #region Scenario A: ML Predicts Success but Business Rules Override with REJECT

        /// <summary>
        /// Scenario A - Test Case 1:
        /// ML Prediction: SUCCESS (Approve) with 85% confidence
        /// Business Rules: REJECT (Interest Rate < 2.5%)
        /// Expected: Final Decision = REJECT, WasOverridden = true
        /// XAI Requirement: AuditTrail contains override reason
        /// </summary>
        [Fact]
        public async Task Scenario_A_MLApproveButRuleReject_InterestRateBelow_ShouldOverrideWithReject()
        {
            // Arrange - Setup data
            var customerId = 1001;
            var customer = new BankCustomer
            {
                Id = customerId,
                Age = 35,
                Job = "management",
                Marital = "married",
                Education = "tertiary",
                Balance = 5000m,
                Housing = "yes",
                Loan = "no",
                Duration = 150,
                Campaign = 1,
                Previous = 0,
                Default = "no",
                Contact = "cellular",
                Day = 15,
                Month = "Mar",
                PDays = -1,
                POutcome = "unknown"
            };

            var mlResult = new MLPredictionResult
            {
                Id = 101,
                CustomerId = customerId,
                PredictedLabel = true,  // ML Says: APPROVE
                Score = 0.85f,
                Probability = 0.85f,
                CreatedAt = DateTime.UtcNow
            };

            // Mock: ML predicts APPROVE
            _mockMLPredictor.Setup(m => m.PredictAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(mlResult);

            // Mock: Business Rules REJECT due to low interest rate
            var ruleResult = new BusinessRuleEngineResult
            {
                IsApproved = false,
                FailedRules = new() { "Low Interest Rate Rule" },
                RiskLevel = "HIGH",
                OverrideReason = "Computed interest rate 2.3% is below minimum threshold of 2.5%"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            _mockDecisionRepository.Setup(m => m.SaveAsync(It.IsAny<HybridDecision>()))
                .Returns(Task.CompletedTask);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert
            decision.MLPredicted.Should().BeTrue("ML predicted APPROVE");
            decision.FinalDecision.Should().BeFalse("Business rules rejected the decision");
            decision.WasOverridden.Should().BeTrue("Decision was overridden by business rules");
            decision.AuditTrail.Should().Contain("OVERRIDE APPLIED", "audit trail must document override");
            decision.AuditTrail.Should().Contain("Low Interest Rate Rule", "audit trail must contain failed rule");
            decision.ApprovedInterestRate.Should().BeLessThan(0.025m, "interest rate should be below threshold");

            // XAI Verification
            decision.AuditTrail.Should().Contain("ML Prediction: APPROVE", "XAI requires ML decision to be documented");
            decision.AuditTrail.Should().Contain("Business Rules Evaluation: FAIL", "XAI requires business rule result");
            decision.RulesApplied.Should().Contain("Low Interest Rate Rule");

            _mockDecisionRepository.Verify(m => m.SaveAsync(It.IsAny<HybridDecision>()), Times.Once);
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
            var customerId = 1002;
            var customer = new BankCustomer
            {
                Id = customerId,
                Age = 18,  // Below typical minimum age requirement
                Job = "technician",
                Marital = "single",
                Education = "secondary",
                Balance = 50000m,
                Housing = "no",
                Loan = "no",
                Duration = 300,
                Campaign = 2,
                Previous = 1,
                Default = "no",
                Contact = "cellular",
                Day = 20,
                Month = "May",
                PDays = 999,
                POutcome = "success"
            };

            var mlResult = new MLPredictionResult
            {
                Id = 102,
                CustomerId = customerId,
                PredictedLabel = true,  // ML Says: APPROVE
                Score = 0.92f,
                Probability = 0.92f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleEngineResult
            {
                IsApproved = false,
                FailedRules = new() { "Age Eligibility Rule" },
                RiskLevel = "MEDIUM"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            _mockDecisionRepository.Setup(m => m.SaveAsync(It.IsAny<HybridDecision>()))
                .Returns(Task.CompletedTask);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert
            decision.MLPredicted.Should().BeTrue();
            decision.FinalDecision.Should().BeFalse();
            decision.WasOverridden.Should().BeTrue();
            decision.RulesApplied.Should().Contain("Age Eligibility Rule");
            decision.AuditTrail.Should().Contain("Age Eligibility Rule");
        }

        #endregion

        #region Scenario B: ML Predicts Failure but System Logs Observation Phase Correctly

        /// <summary>
        /// Scenario B - Test Case 1:
        /// ML Prediction: FAILURE (Reject) with 78% confidence
        /// Business Rules: PASS (all rules satisfied)
        /// Expected: Final Decision = REJECT (ML prediction trumps)
        /// XAI Requirement: Observation phase logged with alignment message
        /// </summary>
        [Fact]
        public async Task Scenario_B_MLRejectButRulesPass_ShouldLogObservationPhase()
        {
            // Arrange
            var customerId = 2001;
            var customer = new BankCustomer
            {
                Id = customerId,
                Age = 45,
                Job = "services",
                Marital = "single",
                Education = "primary",
                Balance = 2000m,
                Housing = "yes",
                Loan = "yes",
                Duration = 200,
                Campaign = 3,
                Previous = 2,
                Default = "yes",  // Previous default
                Contact = "cellular",
                Day = 10,
                Month = "Jan",
                PDays = 3,
                POutcome = "failure"
            };

            var mlResult = new MLPredictionResult
            {
                Id = 201,
                CustomerId = customerId,
                PredictedLabel = false,  // ML Says: REJECT
                Score = 0.22f,
                Probability = 0.22f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleEngineResult
            {
                IsApproved = true,
                FailedRules = new(),
                AppliedRules = new() { "Minimum Balance Rule", "Default History Rule" },
                RiskLevel = "LOW"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            _mockDecisionRepository.Setup(m => m.SaveAsync(It.IsAny<HybridDecision>()))
                .Returns(Task.CompletedTask);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert - ML prediction drives final decision
            decision.MLPredicted.Should().BeFalse("ML predicted REJECT");
            decision.FinalDecision.Should().BeFalse("Final decision should be REJECT");
            decision.WasOverridden.Should().BeTrue("Decision was overridden (rules would approve)");

            // XAI Observation Phase Verification
            decision.AuditTrail.Should().Contain("ML Prediction: REJECT", "Observation: ML prediction phase documented");
            decision.AuditTrail.Should().Contain("Business Rules Evaluation: PASS", "Observation: Business rule phase documented");
            decision.AuditTrail.Should().Contain("Confidence: " + mlResult.Probability.ToString("P0"), "Observation: ML confidence logged");

            _mockDecisionRepository.Verify(m => m.SaveAsync(It.IsAny<HybridDecision>()), Times.Once);
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
            var customerId = 2002;
            var customer = new BankCustomer
            {
                Id = customerId,
                Age = 60,
                Job = "housemaid",
                Marital = "married",
                Education = "illiterate",
                Balance = 100m,
                Housing = "yes",
                Loan = "yes",
                Duration = 50,
                Campaign = 10,
                Previous = 7,
                Default = "yes",
                Contact = "telephone",
                Day = 5,
                Month = "Dec",
                PDays = 14,
                POutcome = "failure"
            };

            var mlResult = new MLPredictionResult
            {
                Id = 202,
                CustomerId = customerId,
                PredictedLabel = false,  // ML Says: REJECT (high confidence)
                Score = 0.88f,
                Probability = 0.88f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleEngineResult
            {
                IsApproved = true,
                FailedRules = new(),
                AppliedRules = new() { "Minimum Balance Rule", "Employment Rule" },
                RiskLevel = "MEDIUM"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            _mockDecisionRepository.Setup(m => m.SaveAsync(It.IsAny<HybridDecision>()))
                .Returns(Task.CompletedTask);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert
            decision.MLPredicted.Should().BeFalse();
            decision.FinalDecision.Should().BeFalse();
            decision.MLConfidence.Should().Be(0.88f);

            // Complete observation logging
            decision.AuditTrail.Should().Contain("88", "Observation must include ML confidence percentage");
            decision.AuditTrail.Should().Contain("Interest Rate", "Observation must include interest rate calculation");
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
            var customerId = 3001;
            var customer = new BankCustomer
            {
                Id = customerId,
                Age = 40,
                Job = "management",
                Marital = "married",
                Education = "tertiary",
                Balance = 100000m,
                Housing = "no",
                Loan = "no",
                Duration = 200,
                Campaign = 1,
                Previous = 0,
                Default = "no",
                Contact = "cellular",
                Day = 25,
                Month = "Jul",
                PDays = -1,
                POutcome = "unknown"
            };

            var mlResult = new MLPredictionResult
            {
                Id = 301,
                CustomerId = customerId,
                PredictedLabel = true,  // ML Approves
                Score = 0.95f,
                Probability = 0.95f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleEngineResult
            {
                IsApproved = true,  // Rules Approve
                FailedRules = new(),
                AppliedRules = new() { "All Rules Pass" },
                RiskLevel = "LOW"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            _mockDecisionRepository.Setup(m => m.SaveAsync(It.IsAny<HybridDecision>()))
                .Returns(Task.CompletedTask);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert
            decision.MLPredicted.Should().BeTrue();
            decision.FinalDecision.Should().BeTrue();
            decision.WasOverridden.Should().BeFalse("No override needed when both approve");
            decision.AuditTrail.Should().Contain("ML prediction and business rules are aligned");
        }

        /// <summary>
        /// Test that both rejecting shows no override but logged correctly
        /// </summary>
        [Fact]
        public async Task BothReject_ShouldNotOverrideAndLogBothFail()
        {
            // Arrange
            var customerId = 3002;
            var customer = new BankCustomer
            {
                Id = customerId,
                Age = 25,
                Job = "unemployed",
                Marital = "single",
                Education = "illiterate",
                Balance = 0m,
                Housing = "no",
                Loan = "yes",
                Duration = 10,
                Campaign = 20,
                Previous = 10,
                Default = "yes",
                Contact = "cellular",
                Day = 1,
                Month = "Jan",
                PDays = 1,
                POutcome = "failure"
            };

            var mlResult = new MLPredictionResult
            {
                Id = 302,
                CustomerId = customerId,
                PredictedLabel = false,  // ML Rejects
                Score = 0.15f,
                Probability = 0.15f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleEngineResult
            {
                IsApproved = false,  // Rules Reject
                FailedRules = new() { "Minimum Balance", "Employment Status", "Debt Ratio" },
                RiskLevel = "CRITICAL"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            _mockDecisionRepository.Setup(m => m.SaveAsync(It.IsAny<HybridDecision>()))
                .Returns(Task.CompletedTask);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert
            decision.MLPredicted.Should().BeFalse();
            decision.FinalDecision.Should().BeFalse();
            decision.WasOverridden.Should().BeFalse("No override when both reject");
            decision.AuditTrail.Should().Contain("Minimum Balance");
            decision.AuditTrail.Should().Contain("Employment Status");
        }

        #endregion

        #region XAI (Explainable AI) Audit Trail Tests

        /// <summary>
        /// Verify that all XAI requirements are met:
        /// - Decision timestamp is recorded
        /// - ML prediction with confidence is documented
        /// - All applied/failed rules are listed
        /// - Override reason is documented
        /// </summary>
        [Fact]
        public async Task XAIAuditTrail_ShouldContainAllRequiredElements()
        {
            // Arrange
            var customerId = 4001;
            var customer = new BankCustomer
            {
                Id = customerId,
                Age = 50,
                Job = "admin",
                Marital = "divorced",
                Education = "secondary",
                Balance = 75000m,
                Housing = "yes",
                Loan = "yes",
                Duration = 180,
                Campaign = 2,
                Previous = 1,
                Default = "no",
                Contact = "cellular",
                Day = 15,
                Month = "Aug",
                PDays = -1,
                POutcome = "unknown"
            };

            var mlResult = new MLPredictionResult
            {
                Id = 401,
                CustomerId = customerId,
                PredictedLabel = true,
                Score = 0.80f,
                Probability = 0.80f,
                CreatedAt = DateTime.UtcNow
            };

            var ruleResult = new BusinessRuleEngineResult
            {
                IsApproved = true,
                FailedRules = new(),
                AppliedRules = new() { "Age Eligibility", "Minimum Balance", "Employment Verification" },
                RiskLevel = "LOW"
            };
            _mockRuleEngine.Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(ruleResult);

            _mockDecisionRepository.Setup(m => m.SaveAsync(It.IsAny<HybridDecision>()))
                .Returns(Task.CompletedTask);

            // Act
            var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);

            // Assert XAI Requirements
            // 1. Decision timestamp
            decision.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // 2. ML Prediction with confidence
            decision.AuditTrail.Should().Contain("ML Prediction");
            decision.AuditTrail.Should().Contain(mlResult.Probability.ToString("P0"));

            // 3. Applied rules
            decision.RulesApplied.Should().NotBeEmpty();
            decision.RulesApplied.Should().Contain("Age Eligibility");
            decision.RulesApplied.Should().Contain("Minimum Balance");

            // 4. Business rules evaluation result
            decision.AuditTrail.Should().Contain("Business Rules Evaluation");

            // 5. Interest rate calculation
            decision.AuditTrail.Should().Contain("Interest Rate");
            decision.ApprovedInterestRate.Should().BeGreaterThan(0);
            decision.ApprovedInterestRate.Should().BeLessThan(1);  // Should be a percentage
        }

        #endregion

        public void Dispose()
        {
            // Cleanup if needed
        }
    }

    /// <summary>
    /// Supporting class for business rule engine result
    /// </summary>
    public class BusinessRuleEngineResult
    {
        public bool IsApproved { get; set; }
        public List<string> FailedRules { get; set; } = new();
        public List<string> AppliedRules { get; set; } = new();
        public string RiskLevel { get; set; } = "MEDIUM";
        public string OverrideReason { get; set; } = string.Empty;
    }
}
