using FluentAssertions;
using HybridDecisionIntelligence.Application.Handlers;
using HybridDecisionIntelligence.Application.Repositories;
using HybridDecisionIntelligence.Application.Requests;
using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HybridDecisionIntelligence.Tests
{
    /// <summary>
    /// The Thought–Action–Observation structure of the audit trail and the dynamic
    /// interest-rate policy (reference rate + the rules' rate corridor).
    /// </summary>
    public class DecisionPolicyTests
    {
        private static BankCustomer Customer(int age = 35, decimal balance = 5000m) => new()
        {
            Id = 1,
            Age = age,
            Balance = balance,
            Job = "management",
            Default = "no",
            Housing = "no",
            Loan = "no"
        };

        private static MLPredictionResult Approve(float probability) =>
            new() { Id = 7, CustomerId = 1, PredictedLabel = true, Probability = probability };

        /// <summary>Rules pass, Low risk, corridor 2%–12% (the seeded rules' bounds)</summary>
        private static DecisionEngine Engine(decimal referenceRate)
        {
            var ruleEngine = new Mock<IBusinessRuleEngine>();
            ruleEngine
                .Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
                .ReturnsAsync(new BusinessRuleResult
                {
                    IsApproved = true,
                    RiskLevel = "Low",
                    AppliedRules = new() { "Minimum Balance Rule" },
                    MinInterestRate = 0.02m,
                    MaxInterestRate = 0.12m
                });

            return new DecisionEngine(
                ruleEngine.Object,
                Mock.Of<IDecisionRepository>(),
                NullLogger<DecisionEngine>.Instance,
                new DecisionPolicyOptions { ReferenceRate = referenceRate });
        }

        [Fact]
        public async Task AuditTrail_TagsEveryEntryWithItsTaoPhase_InOrder()
        {
            var decision = await Engine(0.04m).MakeDecisionAsync(Customer(), Approve(0.8f));

            var entries = decision.AuditTrail.Split(" | ");
            entries.Should().OnlyContain(e =>
                e.StartsWith("[Thought] ") || e.StartsWith("[Action] ") || e.StartsWith("[Observation] "));
            entries.First().Should().StartWith("[Thought] ML Prediction");
            entries.Last().Should().StartWith("[Observation] Final Decision");
            entries.Should().Contain(e => e.StartsWith("[Action] Interest Rate Policy: PASS"));
        }

        [Fact]
        public async Task DefaultReferenceRate_OfferInsideCorridor_IsApproved()
        {
            var decision = await Engine(0.04m).MakeDecisionAsync(Customer(), Approve(0.8f));

            decision.FinalDecision.Should().BeTrue();
            decision.WasOverridden.Should().BeFalse();
            decision.ApprovedInterestRate.Should().Be(0.04m + 0.2m * 0.02m); // reference + confidence spread
        }

        [Fact]
        public async Task HighReferenceRate_OfferAboveCorridor_OverridesApproval()
        {
            // 12% reference + 0.4% confidence spread = 12.4%, above the 12% maximum
            var decision = await Engine(0.12m).MakeDecisionAsync(Customer(), Approve(0.8f));

            decision.FinalDecision.Should().BeFalse();
            decision.WasOverridden.Should().BeTrue();
            decision.OverrideReason.Should().Contain("Interest Rate Policy");
            decision.AuditTrail.Should().Contain("above the policy maximum");
        }

        [Fact]
        public async Task LowReferenceRate_OfferBelowCorridor_OverridesApproval()
        {
            // 0% reference, very confident model, wealthy mature customer: spread goes negative
            var decision = await Engine(0m).MakeDecisionAsync(Customer(age: 60, balance: 80000m), Approve(0.95f));

            decision.ApprovedInterestRate.Should().BeLessThan(0.02m);
            decision.FinalDecision.Should().BeFalse();
            decision.OverrideReason.Should().Contain("Interest Rate Policy");
            decision.AuditTrail.Should().Contain("below the policy minimum");
        }

        [Fact]
        public async Task ReferenceRate_DoesNotApproveWhatTheModelRejects()
        {
            var rejected = new MLPredictionResult { Id = 7, CustomerId = 1, PredictedLabel = false, Probability = 0.2f };

            var decision = await Engine(0.12m).MakeDecisionAsync(Customer(), rejected);

            decision.FinalDecision.Should().BeFalse();
            decision.WasOverridden.Should().BeFalse(); // nothing to override: the model already said no
        }

        [Fact]
        public async Task RuleEngine_UsesStrictestRateCorridorOfActiveRules()
        {
            var repository = new Mock<IBusinessRuleRepository>();
            repository.Setup(m => m.GetActiveRulesAsync()).ReturnsAsync(new List<BusinessRule>
            {
                new() { Name = "A", MinAge = 18, MaxAge = 100, MinInterestRate = 0.02m, MaxInterestRate = 0.12m },
                new() { Name = "B", MinAge = 18, MaxAge = 100, MinInterestRate = 0.03m, MaxInterestRate = 0.10m }
            });
            var engine = new BusinessRuleEngine(repository.Object, NullLogger<BusinessRuleEngine>.Instance);

            var result = await engine.EvaluateAsync(Customer());

            result.MinInterestRate.Should().Be(0.03m);
            result.MaxInterestRate.Should().Be(0.10m);
        }

        /// <summary>The three seeded rules, each owning exactly one condition</summary>
        private static BusinessRuleEngine SeededRuleEngine()
        {
            var repository = new Mock<IBusinessRuleRepository>();
            repository.Setup(m => m.GetActiveRulesAsync()).ReturnsAsync(new List<BusinessRule>
            {
                new() { Name = "Minimum Balance Rule", MinBalance = 1000, MinInterestRate = 0.02m, MaxInterestRate = 0.12m },
                new() { Name = "Age Eligibility Rule", MinAge = 25, MaxAge = 70, MinInterestRate = 0.02m, MaxInterestRate = 0.12m },
                new() { Name = "No Default History", RequireNoDefault = true, MinInterestRate = 0.02m, MaxInterestRate = 0.12m }
            });
            return new BusinessRuleEngine(repository.Object, NullLogger<BusinessRuleEngine>.Instance);
        }

        [Fact]
        public async Task RuleEngine_CustomerInDefault_FailsOnlyTheDefaultRule()
        {
            var customer = Customer(age: 40, balance: 5000m);
            customer.Default = "yes";

            var result = await SeededRuleEngine().EvaluateAsync(customer);

            result.FailedRules.Should().Equal("No Default History");
            result.AppliedRules.Should().Equal("Minimum Balance Rule", "Age Eligibility Rule");
        }

        [Fact]
        public async Task RuleEngine_LowBalance_FailsOnlyTheBalanceRule()
        {
            var result = await SeededRuleEngine().EvaluateAsync(Customer(age: 40, balance: 400m));

            result.FailedRules.Should().Equal("Minimum Balance Rule");
            result.IsApproved.Should().BeFalse();
        }

        [Fact]
        public async Task CustomerHistoryHandler_ReturnsRepositoryDecisions()
        {
            var decisions = new List<HybridDecision> { new() { Id = 2, CustomerId = 5 }, new() { Id = 1, CustomerId = 5 } };
            var repository = new Mock<IDecisionRepository>();
            repository.Setup(m => m.GetCustomerDecisionsAsync(5)).ReturnsAsync(decisions);

            var result = await new GetCustomerDecisionHistoryHandler(repository.Object)
                .Handle(new GetCustomerDecisionHistoryRequest { CustomerId = 5 }, CancellationToken.None);

            result.Should().BeSameAs(decisions);
        }
    }
}
