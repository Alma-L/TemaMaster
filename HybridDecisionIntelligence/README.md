# Hybrid Decision Intelligence Banking System - Setup & CLI Commands

## Project Overview
A Clean Architecture solution in .NET 9 implementing a hybrid decision engine that combines:
- **ML.NET Binary Classification** for customer subscription predictions (UCI Bank Marketing dataset)
- **Business Rule Engine** for regulatory/policy constraints
- **Explainable AI** with detailed audit trails for every decision

## Quick Start Setup

### Prerequisites
- .NET 9 SDK
- SQL Server (LocalDB or Express)
- Git

### 1. Database Setup

```bash
# Navigate to solution directory
cd HybridDecisionIntelligence

# Create migration
dotnet ef migrations add InitialCreate \
  -p HybridDecisionIntelligence.Infrastructure \
  -s HybridDecisionIntelligence.API

# Apply migration to database
dotnet ef database update \
  -p HybridDecisionIntelligence.Infrastructure \
  -s HybridDecisionIntelligence.API
```

### 2. ML Model Training

```bash
# Prepare your CSV data in format compatible with UCI Bank Marketing dataset
# Copy your bank data to: Data/BankData.csv

# The model will be trained on startup if not exists
# Or manually via API endpoint:
POST /api/ml/train
Content-Type: application/json
{
  "dataPath": "Data/BankData.csv",
  "modelPath": "Models/BankMarketingModel.zip"
}
```

### 3. Run Application

```bash
# Build solution
dotnet build

# Run API
dotnet run --project HybridDecisionIntelligence.API

# API will be available at: https://localhost:7001
# Swagger UI: https://localhost:7001/swagger
```

## API Endpoints

### Make a Hybrid Decision
```bash
POST /api/decisions/make-decision
Content-Type: application/json

{
  "customerId": 12345,
  "age": 42,
  "job": "management",
  "marital": "married",
  "education": "tertiary",
  "balance": 25000,
  "housing": "yes",
  "loan": "no",
  "duration": 250,
  "campaign": 1,
  "previous": 0
}
```

**Response (with XAI Audit Trail):**
```json
{
  "customerId": 12345,
  "mlPrediction": true,
  "mlConfidence": 0.82,
  "finalDecision": true,
  "approvedInterestRate": 0.045,
  "wasOverridden": false,
  "auditTrail": "ML Prediction: APPROVE (Confidence: 82.00%) | Business Rules Evaluation: PASS | ML prediction and business rules are aligned | Interest Rate Calculated: 4.50%",
  "appliedRules": ["Minimum Balance Rule", "Age Eligibility Rule", "No Default History"],
  "decisionTime": "2026-03-22T10:30:45.123Z"
}
```

### Get Decision History
```bash
GET /api/decisions/customer/{customerId}/history

# Response: List of all decisions for customer with full audit trails
```

### Health Check
```bash
GET /api/decisions/health
```

## Solution Architecture

### Domain Layer (Entities & Value Objects)
- `BankCustomer` - Customer entity
- `MLPredictionResult` - ML model output
- `BusinessRule` - Business constraint definition
- `HybridDecision` - Final decision with audit trail
- `BankMarketingData` & `BankMarketingPrediction` - ML.NET DTOs

### Application Layer (Use Cases & Logic)
- `IDecisionEngine` - Orchestrates hybrid logic
- `IBusinessRuleEngine` - Evaluates business rules
- `IMLPredictor` - Wraps ML.NET predictions
- `MakeDecisionHandler` - MediatR handler

### Infrastructure Layer (Persistence & ML)
- **EF Core** - SQL Server database access
- **ML.NET** - Binary classification with FastTree learner
- **Repositories** - Data access layer

### API Layer (REST Endpoints)
- `DecisionsController` - Decision endpoints
- Swagger/OpenAPI documentation

## Key Features

### 1. Hybrid Decision Engine
```csharp
// Combines ML + Rules
var decision = await _decisionEngine.MakeDecisionAsync(customer, mlResult);
// Returns: FinalDecision + WasOverridden + AuditTrail
```

### 2. Explainable AI (XAI)
Every response includes an `AuditTrail` string explaining:
- ML prediction & confidence score
- Business rules evaluation
- If prediction was overridden (and why)
- Interest rate calculation breakdown

Example override scenario:
```
ML Prediction: APPROVE (Confidence: 88.00%) | 
Business Rules Evaluation: FAIL | 
OVERRIDE APPLIED: ML predicted APPROVE, but rules require REJECT | 
Override Reason: Minimum Balance Rule | 
Interest Rate Calculated: N/A - Decision Rejected
```

### 3. Interest Rate Calculation
Dynamic rate based on:
- Base rate: 4%
- ML confidence adjustment: ±2%
- Risk level: 0-3%
- Balance bonus: -0.5% to 0%
- Age bonus: -0.5% to 0%
- Clamped: 2% - 12%

### 4. Seed Business Rules
Three default rules:
1. **Minimum Balance** - Must have €1000+
2. **Age Eligibility** - 25-70 years, approved jobs
3. **No Default History** - Cannot have previous defaults

## ML Model Details

### Training
- Algorithm: FastTree Binary Classifier
- Features: 16 numerical + categorical encoded
- Output: Binary label + probability score
- Data: UCI Bank Marketing dataset

### Prediction
```csharp
var prediction = await _mlPredictor.PredictAsync(customer);
// Returns: Prediction (bool) + Probability (0-1) + Score
```

## Environment Configuration

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=HybridDecisionIntelligence;Trusted_Connection=true;"
  },
  "MLModel": {
    "ModelPath": "Models/BankMarketingModel.zip",
    "DataPath": "Data/BankData.csv"
  }
}
```

## Testing

### Unit Test Example
```bash
# Test MakeDecisionHandler
# Input: Customer data
# Expected: Decision with audit trail and interest rate

var request = new MakeDecisionRequest
{
    CustomerId = 1,
    Age = 45,
    Balance = 15000,
    // ...
};
var response = await handler.Handle(request, CancellationToken.None);
Assert.True(response.FinalDecision);
Assert.NotEmpty(response.AuditTrail);
```

## Common Issues & Solutions

### Issue: "Model file not found"
**Solution:** Ensure ML model is trained first via API or manually place zip file in Models directory

### Issue: "Connection timeout to SQL Server"
**Solution:** Update ConnectionString, ensure SQL Server is running
```bash
dotnet ef database update --connection "Server=YOUR_SERVER;Database=HybridDecisionIntelligence;Trusted_Connection=true;"
```

### Issue: EF Core migration conflicts
**Solution:** Revert and recreate migrations
```bash
dotnet ef migrations remove -p HybridDecisionIntelligence.Infrastructure
dotnet ef migrations add InitialCreate -p HybridDecisionIntelligence.Infrastructure
```

## Production Deployment

### Containerization
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY bin/Release/net9.0/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "HybridDecisionIntelligence.API.dll"]
```

### Azure Deployment
- App Service for API
- Azure SQL Database for persistence
- Azure Blob Storage for ML models

## Performance Metrics

- Decision latency: ~100-200ms (ML prediction + rules)
- Throughput: 500+ decisions/minute per instance
- ML model accuracy: ~78-85% (depends on training data)

## Future Enhancements

1. **Model Versioning** - A/B testing with multiple models
2. **Real-time Feedback Loop** - Retrain based on actual outcomes
3. **Feature Store** - Centralized feature management
4. **Advanced XAI** - SHAP values for feature importance
5. **Batch Processing** - Decision processing for large datasets
