# Hybrid Decision Intelligence - Phase 5 Testing Guide

## Overview
This XUnit test project validates the HybridDecisionIntelligence solution during Phase 5 (Validation).

### Test Coverage
- ✅ Integration Tests for HybridDecisionService
- ✅ Scenario A: ML Success overridden by Business Rules (Low Interest Rate)
- ✅ Scenario B: ML Failure with logging of Observation Phase
- ✅ Business Rule Overrides with XAI audit trails
- ✅ Performance Metrics: Accuracy, Precision, Recall, F1 Score, AUC
- ✅ Database Validation: AuditLog storage for XAI requirements
- ✅ Mocking: BankData.csv streams without physical file dependencies

---

## Project Setup Commands

### 1. Add Test Project to Solution

```powershell
# Navigate to solution root
cd "c:\Users\Alma\Desktop\TemaMaster\HybridDecisionIntelligence"

# Create test project directory
mkdir HybridDecisionIntelligence.Tests

# Create XUnit test project
dotnet new xunit -n HybridDecisionIntelligence.Tests -o HybridDecisionIntelligence.Tests

# Add to solution
dotnet sln add HybridDecisionIntelligence.Tests\HybridDecisionIntelligence.Tests.csproj

# Add project references
cd HybridDecisionIntelligence.Tests
dotnet add reference ../HybridDecisionIntelligence.Domain/HybridDecisionIntelligence.Domain.csproj
dotnet add reference ../HybridDecisionIntelligence.Application/HybridDecisionIntelligence.Application.csproj
dotnet add reference ../HybridDecisionIntelligence.Infrastructure/HybridDecisionIntelligence.Infrastructure.csproj

# Add required NuGet packages
dotnet add package Moq --version 4.20.70
dotnet add package FluentAssertions --version 6.12.0
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 9.0.5
```

### 2. Restore and Build

```powershell
# Restore all dependencies
dotnet restore

# Build solution
dotnet build
```

---

## Running Tests

### Run All Tests
```powershell
dotnet test --verbosity normal
```

### Run Specific Test Class
```powershell
# Integration/Decision Logic Tests
dotnet test --filter "FullyQualifiedName~DecisionLogicTests"

# Performance Metrics Tests
dotnet test --filter "FullyQualifiedName~PerformanceMetricsTests"

# Database Validation Tests
dotnet test --filter "FullyQualifiedName~DatabaseValidationTests"
```

### Run Specific Test Scenario
```powershell
# Scenario A - ML Approve but Rules Reject
dotnet test --filter "Name~Scenario_A_MLApproveButRuleReject_InterestRateBelow_ShouldOverrideWithReject"

# Scenario B - ML Reject but Business Rules Pass
dotnet test --filter "Name~Scenario_B_MLRejectButRulesPass_ShouldLogObservationPhase"
```

### Run with Detailed Output
```powershell
dotnet test --verbosity detailed --logger "console;verbosity=detailed"
```

### Run and Generate XML Report
```powershell
dotnet test --logger "trx;LogFileName=test-results.trx"
```

### Run with Code Coverage (requires coverlet)
```powershell
dotnet add package coverlet.collector --version 6.0.0

dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Test Structure

### DecisionLogicTests.cs
**Phase 5 Integration Tests** - Validates hybrid decision logic

#### Scenario A: ML Success Overridden by Business Rules
```
✓ Scenario_A_MLApproveButRuleReject_InterestRateBelow_ShouldOverrideWithReject
  - ML predicts: APPROVE (85% confidence)
  - Business Rules: REJECT (Interest Rate < 2.5%)
  - Expected: Final Decision = REJECT, WasOverridden = true
  - Validates XAI audit trail contains override reason

✓ Scenario_A_MLApproveButAgeRuleFails_ShouldOverrideWithReject
  - ML predicts: APPROVE (92% confidence)
  - Business Rules: FAIL on Age Eligibility
  - Expected: Final Decision = REJECT, audit trail contains rule violation
```

#### Scenario B: ML Failure with Observation Phase Logging
```
✓ Scenario_B_MLRejectButRulesPass_ShouldLogObservationPhase
  - ML predicts: REJECT (78% confidence)
  - Business Rules: PASS
  - Expected: Final Decision = REJECT
  - Validates observation phase logging with alignment message

✓ Scenario_B_MLRejectHighConfidenceRulesPass_ShouldLogCompleteObservation
  - ML predicts: REJECT (88% confidence)
  - Business Rules: PASS
  - Expected: Complete observation logged with confidence percentage
```

#### Cross-Scenario Tests
```
✓ BothApprove_ShouldNotOverrideAndLogAlignment
✓ BothReject_ShouldNotOverrideAndLogBothFail
```

#### XAI Audit Trail Validation
```
✓ XAIAuditTrail_ShouldContainAllRequiredElements
  - Decision timestamp recorded
  - ML prediction with confidence documented
  - All applied/failed rules listed
  - Override reason documented
  - Interest rate calculation logged
```

---

### PerformanceMetricsTests.cs
**ML.NET Evaluation Metrics** - Calculates model performance

#### Metrics Calculated
```
✓ Accuracy: (TP + TN) / Total = Percentage of correct predictions
✓ Precision: TP / (TP + FP) = Quality of positive predictions
✓ Recall: TP / (TP + FN) = Coverage of actual positives
✓ Sensitivity: TP / (TP + FN) = True positive rate
✓ Specificity: TN / (TN + FP) = True negative rate
✓ F1 Score: 2 * (Precision * Recall) / (Precision + Recall)
✓ AUC: Area Under ROC Curve = Model discrimination ability
✓ Confusion Matrix: TP, FP, TN, FN
```

#### Test Cases
```
✓ CalculateAccuracy_ShouldMeetThreshold (> 70%)
✓ CalculatePrecision_ShouldIndicateQualityOfPositivePredictions (> 70%)
✓ CalculateRecall_ShouldIndicateCoverageOfActualPositives (> 70%)
✓ CalculateF1Score_ShouldBalancePrecisionAndRecall
✓ CalculateROC_AUC_ShouldMeasureClassSeparation (> 80%)
✓ ConfusionMatrix_ShouldCalculateAllComponents
✓ SensitivityAndSpecificity_ShouldMeasureClassPerformance
✓ PerformanceMetrics_LargeDataset_ShouldCalculateQuickly (< 1 second for 10K records)
✓ AllPredictionsCorrect_ShouldReturnPerfectScores
✓ AllPredictionsIncorrect_ShouldReturnZeroAccuracy
```

---

### DatabaseValidationTests.cs
**XAI Audit Trail Persistence** - Validates decision history storage

```
✓ SaveHybridDecision_WithCompleteAuditTrail_ShouldPersistCorrectly
  - Verifies all audit trail fields are stored
  - Confirms XAI components saved to database

✓ AuditTrail_ShouldContainCompleteXAI_Explanation
  - Validates Thought, Action, Observation phases logged
  - Confirms final decision documented

✓ MultipleDecisions_SameCoreCustomer_ShouldMaintainSeparateAuditTrails
  - Tests decision history for individual customer
  - Validates audit trail per decision

✓ Override_Information_ShouldBePersisted
  - Confirms WasOverridden flag stored
  - Validates override reason persisted

✓ ApprovedInterestRate_ShouldBeStoredWithPrecision
  - Tests decimal precision (e.g., 0.025m, 0.0345m)

✓ DecisionTimestamp_ShouldBeRecordedAccurately
  - Validates CreatedAt field

✓ LargeAuditTrail_ShouldBePersisted_WithoutTruncation
  - Tests large explanation strings not truncated

✓ QueryDecisionHistory_ByCustomer_ShouldReturnAuditTrails
  - Verifies retrieval of historical decisions for compliance
```

---

## Utilities

### MLPerformanceEvaluator.cs
Calculates ML.NET performance metrics without requiring trained models:

```csharp
var evaluator = new MLPerformanceEvaluator();
var predictions = new[] { 
    (predicted: true, actual: true),
    (predicted: true, actual: false),
    (predicted: false, actual: false)
};
var metrics = evaluator.CalculateMetrics(predictions);

Console.WriteLine(metrics); // Pretty-printed report
```

### TestDataBuilder.cs
Creates test data and mocks without physical file dependencies:

```csharp
// Create test customer
var customer = TestDataBuilder.CreateValidBankCustomer(customerId: 1001);

// Create high/low risk customers
var highRisk = TestDataBuilder.CreateHighRiskBankCustomer();
var lowRisk = TestDataBuilder.CreateLowRiskBankCustomer();

// Create batch of customers
var customers = TestDataBuilder.CreateBatchOfCustomers(count: 100);

// Create mock CSV stream without file I/O
var csvStream = TestDataBuilder.CreateMockBankDataStream(recordCount: 1000);

// Create test partition for ML evaluation
var (predicted, actual) = TestDataBuilder.CreateMockTestPartition(sampleSize: 1000);
```

---

## Mocking Strategy

All tests use **Moq** for dependency injection without physical dependencies:

```csharp
// Mock repository
var mockRepository = new Mock<IDecisionRepository>();
mockRepository
    .Setup(m => m.SaveAsync(It.IsAny<HybridDecision>()))
    .Returns(Task.CompletedTask);

// Mock service
var mockRuleEngine = new Mock<IBusinessRuleEngine>();
mockRuleEngine
    .Setup(m => m.EvaluateAsync(It.IsAny<BankCustomer>()))
    .ReturnsAsync(new BusinessRuleEngineResult { IsApproved = true });

// Create SUT with mocks
var decisionEngine = new DecisionEngine(
    mockRuleEngine.Object,
    mockRepository.Object,
    mockLogger.Object
);
```

---

## Test Output Examples

### Successful Test Run
```
Test Run Summary
  Total tests: 21
  Passed: 21
  Failed: 0
  Skipped: 0
Duration: 4.523 seconds

PASSED DecisionLogicTests.Scenario_A_MLApproveButRuleReject_InterestRateBelow_ShouldOverrideWithReject
PASSED DecisionLogicTests.Scenario_B_MLRejectButRulesPass_ShouldLogObservationPhase
PASSED PerformanceMetricsTests.CalculateAccuracy_ShouldMeetThreshold
PASSED DatabaseValidationTests.SaveHybridDecision_WithCompleteAuditTrail_ShouldPersistCorrectly
...
```

### Performance Metrics Report
```
=== ML Model Performance Metrics ===
Accuracy:       85.23%
Precision:      88.45%
Recall:         82.10%
Sensitivity:    82.10%
Specificity:    87.65%
F1 Score:       85.18%
AUC:            88.88%

--- Confusion Matrix ---
True Positives:  410
False Positives: 53
True Negatives:  487
False Negatives: 50

Total Predictions: 1000
```

---

## Continuous Integration

### GitHub Actions Example
```yaml
name: Run Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - run: dotnet restore
      - run: dotnet build
      - run: dotnet test --verbosity normal --logger "trx;LogFileName=test-results.trx"
      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: test-results
          path: '**/test-results.trx'
```

---

## Troubleshooting

### Test Fails: "Cannot find MockRepository"
**Solution**: Ensure Moq NuGet package is installed:
```powershell
dotnet add package Moq
```

### Test Fails: "DbContext Configuration Error"
**Solution**: Use in-memory database for unit tests:
```csharp
var options = new DbContextOptionsBuilder<HybridDecisionContext>()
    .UseSqlite("Data Source=:memory:")
    .Options;
```

### Performance Metrics Show Low Accuracy
**Solution**: This is expected with random test data. Real data yields better results.

### Test Timeout Issues
**Solution**: Increase timeout for slow systems:
```powershell
dotnet test --configuration Release --timeout 60000
```

---

## Next Steps

1. ✅ Run all tests: `dotnet test`
2. ✅ Generate coverage report
3. ✅ Integrate with CI/CD pipeline
4. ✅ Monitor test coverage (aim for > 80%)
5. ✅ Add additional test cases as needed

---

## Contact & Support
For questions about test implementation, refer to the DecisionLogicTests.cs file documentation.
