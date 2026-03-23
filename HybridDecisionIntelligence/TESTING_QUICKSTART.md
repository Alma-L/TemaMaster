# XUnit Testing - Quick Reference Guide

## Quick Start (2 Minutes)

### Option 1: Automated Setup
```powershell
cd "c:\Users\Alma\Desktop\TemaMaster\HybridDecisionIntelligence"
.\Setup-TestProject.ps1
```

### Option 2: Manual Setup
```powershell
cd "c:\Users\Alma\Desktop\TemaMaster\HybridDecisionIntelligence"

# Add test project to solution
dotnet sln add HybridDecisionIntelligence.Tests/HybridDecisionIntelligence.Tests.csproj

# Restore and build
dotnet restore
dotnet build
```

---

## 📋 Run Tests

### All Tests
```powershell
dotnet test
```

### With Detailed Output
```powershell
dotnet test --verbosity detailed
```

### Decision Logic Tests Only
```powershell
dotnet test --filter "FullyQualifiedName~DecisionLogicTests"
```

### Scenario A Only
```powershell
dotnet test --filter "Name~Scenario_A"
```

### Scenario B Only
```powershell
dotnet test --filter "Name~Scenario_B"
```

### Performance Metrics Tests
```powershell
dotnet test --filter "FullyQualifiedName~PerformanceMetricsTests"
```

### Database Validation Tests
```powershell
dotnet test --filter "FullyQualifiedName~DatabaseValidationTests"
```

---

## 📊 Test Coverage Summary

| Test Class | Tests | Purpose |
|-----------|-------|---------|
| **DecisionLogicTests** | 7 | Phase 5 Integration - ML + Business Rules |
| **PerformanceMetricsTests** | 10 | ML.NET Metrics: Accuracy, Precision, Recall |
| **DatabaseValidationTests** | 8 | XAI Audit Trail Persistence |
| **TOTAL** | **25** | Complete Phase 5 Validation |

---

## 🎯 Test Scenarios

### Scenario A: ML Success → Rules Reject
- ✅ Low Interest Rate Override (< 2.5%)
- ✅ Age Eligibility Rule Violation
- ✅ XAI Audit Trail Documentation

### Scenario B: ML Failure → Rules Pass
- ✅ Observation Phase Logging
- ✅ High Confidence Rejection Logging
- ✅ Complete XAI Explanation

### Cross-Scenarios
- ✅ Both Approve (No Override)
- ✅ Both Reject (No Override)
- ✅ XAI Audit Trail Requirements

---

## 📈 Performance Metrics Tested

```
Accuracy    = (TP + TN) / Total         → ✓ Target: > 75%
Precision   = TP / (TP + FP)            → ✓ Target: > 80%
Recall      = TP / (TP + FN)            → ✓ Target: > 75%
F1 Score    = 2*(P*R)/(P+R)             → ✓ Balanced metric
AUC         = Area Under ROC Curve       → ✓ Target: > 80%
```

---

## 💾 Database Tests

| Test | Validates |
|------|-----------|
| SaveHybridDecision | Complete audit trail persistence |
| AuditTrail | XAI explanation components |
| MultipleDecisions | Decision history per customer |
| Override Info | WasOverridden flag & reason |
| InterestRate | Decimal precision storage |
| Timestamp | Decision creation time |
| LargeAuditTrail | No string truncation |
| QueryHistory | Retrieval for compliance |

---

## 🔧 Advanced Commands

### Generate XML Test Report
```powershell
dotnet test --logger "trx;LogFileName=test-results.trx"
```

### Generate Code Coverage
```powershell
dotnet add package coverlet.collector
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Run in Release Mode
```powershell
dotnet test -c Release
```

### Verbose Logging
```powershell
dotnet test --verbosity diagnostic
```

### Run Parallel
```powershell
dotnet test -- RunConfiguration.MaxCpuCount=4
```

---

## 📝 View Test Details

### See DecisionLogicTests.cs
```powershell
notepad HybridDecisionIntelligence.Tests\DecisionLogicTests.cs
```

### See Performance Metrics
```powershell
notepad HybridDecisionIntelligence.Tests\PerformanceMetricsTests.cs
```

### See Database Tests
```powershell
notepad HybridDecisionIntelligence.Tests\DatabaseValidationTests.cs
```

### See Full Documentation
```powershell
notepad HybridDecisionIntelligence.Tests\README.md
```

---

## ✅ Expected Results

### Successful Test Run Output
```
Test Run Summary
  Total tests: 25
  Passed: 25
  Failed: 0
  Skipped: 0
Duration: 4.523 seconds
```

### Performance Metrics Example
```
Accuracy:       85.23%
Precision:      88.45%
Recall:         82.10%
F1 Score:       85.18%
AUC:            88.88%
```

### Audit Trail Example
```
ML Prediction: APPROVE (Confidence: 85%) | 
Business Rules Evaluation: FAIL | 
Override Reason: Interest Rate < 2.5% | 
Interest Rate Calculated: 2.30%
```

---

## 🐛 Troubleshooting

| Issue | Solution |
|-------|----------|
| Moq not found | `dotnet add package Moq` |
| DbContext error | InMemory SQLite used automatically |
| Tests timeout | Run with `-c Release` |
| Build fails | Run `dotnet restore` first |
| Permission denied | Run PowerShell as Administrator |

---

## 🔗 Project Structure

```
HybridDecisionIntelligence.Tests/
├── HybridDecisionIntelligence.Tests.csproj
├── DecisionLogicTests.cs              # Scenario A & B tests
├── PerformanceMetricsTests.cs         # ML metrics tests
├── DatabaseValidationTests.cs          # Audit trail persistence
├── Utilities/
│   ├── MLPerformanceEvaluator.cs      # Metrics calculator
│   └── TestDataBuilder.cs              # Mock data generator
├── README.md                           # Full documentation
└── .gitignore
```

---

## 📞 Next Steps

1. **Run Setup**: `.\Setup-TestProject.ps1`
2. **See Results**: `dotnet test`
3. **Review Coverage**: Check test output
4. **Generate Report**: `dotnet test --logger "trx;LogFileName=results.trx"`
5. **Integrate CI/CD**: Add to GitHub Actions/Azure Pipelines

---

**Created**: Phase 5 Validation Testing  
**Framework**: XUnit (.NET 9)  
**Coverage**: 25 Test Cases  
**Status**: ✅ Ready to Run
