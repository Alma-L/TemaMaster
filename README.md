# Hybrid Decision Intelligence Banking System

Master's thesis project: *Design and Implementation of an AI-Driven Hybrid Decision Intelligence Architecture for Digital Banking Systems Using .NET* (Alma Latifi, University of Prishtina).

The system decides whether a bank customer should receive a term-deposit offer. Every decision combines:

- **An ML.NET model** (FastTree binary classifier trained on the UCI Bank Marketing dataset) that predicts whether the customer will subscribe.
- **A business rule engine** (rules stored in SQL Server) that can override the model.
- **An explainable audit trail** that records each step of the decision as a Thought → Action → Observation cycle.

Inference runs in-process inside the ASP.NET Core API; there is no separate model service.

## Repository layout

```
TemaMaster/
├── HybridDecisionIntelligence/                  .NET 9 solution (Clean Architecture)
│   ├── HybridDecisionIntelligence.Domain/        Entities and ML.NET input/output classes
│   ├── HybridDecisionIntelligence.Application/   DecisionEngine, BusinessRuleEngine, MLPredictor, MediatR handlers
│   ├── HybridDecisionIntelligence.Infrastructure/ EF Core (SQL Server), ML.NET model service, QuestPDF reports, CSV import
│   ├── HybridDecisionIntelligence.API/           REST API, Swagger, dataset (Data/BankData.csv) and trained model (Models/)
│   ├── HybridDecisionIntelligence.Tests/         xUnit tests (34)
│   └── seed-demo-data.py                        Sends random dataset rows through the live API
├── HybridDecisionIntelligence.MLTraining/       Reproducible experiments behind Chapter 5 of the thesis (results/)
└── HybridDecisionIntelligence.Frontend/         React 18 + TypeScript + Tailwind dashboard
```

## Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 9, ASP.NET Core Web API, MediatR 14, Swashbuckle (Swagger) |
| Machine learning | ML.NET 5.0 (`FastTree`, `PredictionEngine`, in-process) |
| Persistence | Entity Framework Core 9, SQL Server |
| Reports | QuestPDF (one PDF per decision) |
| Frontend | React 18, TypeScript, Tailwind CSS, Radix UI (Create React App) |
| Tests | xUnit, Moq, FluentAssertions, EF Core InMemory/SQLite |

## Getting started

### Prerequisites

- .NET 9 SDK
- SQL Server (local instance; the default connection uses Windows authentication)
- Node.js 18+ (for the dashboard)
- Python 3 (optional, only for `seed-demo-data.py`)

All commands below start from the repository root (`TemaMaster/`).

### 1. Configure the database

`HybridDecisionIntelligence/HybridDecisionIntelligence.API/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=HybridDecisionIntelligenceDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

No migrations are needed. On startup the API calls `EnsureCreated()`, which creates the database and seeds the three default business rules. If the database already exists, the API applies its schema upgrade checks instead.

### 2. Run the API

```bash
cd HybridDecisionIntelligence
dotnet run --project HybridDecisionIntelligence.API --launch-profile http
```

- **API:** `http://localhost:5050`
- **Swagger UI:** `http://localhost:5050/swagger`

If `HybridDecisionIntelligence.API/Models/BankMarketingModel.zip` is missing, the API trains the model from `HybridDecisionIntelligence.API/Data/BankData.csv` on first start. It also writes the held-out metrics to `Models/BankMarketingModel.metrics.json`.

> Use the `http` profile for local development. The `https` profile (`https://localhost:7074`) redirects all traffic to HTTPS and requires a trusted ASP.NET Core dev certificate (`dotnet dev-certs https --trust`). Without it, `/api/v1/model/benchmark` fails, because its internal HTTP call rejects the certificate.

### 3. Run the dashboard

```bash
cd HybridDecisionIntelligence.Frontend
npm install
npm start
```

The dashboard opens at `http://localhost:3000`. It reads the API address from `.env` (`REACT_APP_API_URL=http://localhost:5050/api`). CORS allows `localhost:3000`, `3001` and `5173`.

### 4. (Optional) Load decisions

Choose one:

```bash
# Run the whole dataset (45,211 rows) through the pipeline and store every decision (takes a few minutes; re-runs skip imported rows)
curl -X POST "http://localhost:5050/api/v1/dataset/import"

# Or store a random sample of N decisions via the public endpoint
python HybridDecisionIntelligence/seed-demo-data.py 500
```

### 5. Run the tests

```bash
cd HybridDecisionIntelligence
dotnet test
```

### 6. Reproduce the thesis experiments (optional)

`HybridDecisionIntelligence.MLTraining` uses the same 80/20 split (seed 0) as the API's model and writes
`results/experiment_results.json` and `results/pr_curve.csv`: the comparison with logistic regression, random
forest and LightGBM (5-fold cross-validation), a FastTree grid search, threshold analysis and confusion
matrices, permutation feature importance, the hybrid system evaluated on the held-out 20%, and the
reference-rate simulation. It takes about 15–30 minutes and must be run from its own folder:

```bash
cd HybridDecisionIntelligence.MLTraining
dotnet run -c Release
```

## API

Every controller answers on both `/api/...` and `/api/v1/...`; Swagger documents the `v1` routes.

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/v1/decisions/make-decision` | Score a customer, apply the rules, store and return the decision |
| `GET` | `/api/v1/decisions` | Paginated decisions (`page`, `pageSize`, filters: `customerId`, `finalDecision`, `wasOverridden`, probability and interest-rate ranges) |
| `GET` | `/api/v1/decisions/stats` | Totals and rates over all stored decisions (same filters) |
| `GET` | `/api/v1/decisions/{id}/customer` | Customer profile snapshot as it was at decision time |
| `GET` | `/api/v1/decisions/{id}/report` | PDF report with the profile and full audit trail |
| `GET` | `/api/v1/decisions/customer/{customerId}/history` | All decisions for one customer |
| `GET` | `/api/v1/decisions/health` | Health check |
| `GET` | `/api/v1/customers/{id}` | Stored customer profile |
| `GET` | `/api/v1/businessrules` | Active business rules |
| `GET` | `/api/v1/model/metrics` | Held-out evaluation of the trained model |
| `POST` | `/api/v1/model/predict` | Model only: score one row (no rules, nothing stored) |
| `GET` | `/api/v1/model/benchmark?samples=1000` | In-process vs HTTP inference latency |
| `POST` | `/api/v1/dataset/import?limit=N` | Run the CSV through the pipeline and store decisions |
| `GET` | `/api/v1/dataset/simulate?referenceRates=0,0.04,0.08` | What-if analysis of the reference rate (nothing stored) |

### Example: make a decision

```http
POST /api/v1/decisions/make-decision
Content-Type: application/json

{
  "customerId": 12345,
  "age": 42,
  "job": "management",
  "marital": "married",
  "education": "tertiary",
  "default": "no",
  "balance": 25000,
  "housing": "yes",
  "loan": "no",
  "contact": "cellular",
  "day": 15,
  "month": "may",
  "campaign": 1,
  "pDays": -1,
  "previous": 0,
  "pOutcome": "unknown"
}
```

The response contains `mlPrediction`, `mlConfidence`, `finalDecision`, `wasOverridden`, `approvedInterestRate`, `appliedRules` and `auditTrail`. Example of an audit trail with an override:

```
[Thought] ML Prediction: APPROVE (Confidence: 81.30%) |
[Action] Business Rules Evaluation: FAIL (Risk: Medium) |
[Action] Interest Rate Calculated: 5.87% (reference 4.00% + spread 1.87%) |
[Action] Interest Rate Policy: PASS (within 2.00%–12.00%) |
[Observation] OVERRIDE APPLIED: ML predicted APPROVE, but rules require REJECT |
[Observation] Override Reason: Minimum Balance Rule |
[Observation] Final Decision: REJECTED
```

## How a decision is made

1. **Thought:** the ML model predicts subscribe / not subscribe and returns a probability.
2. **Action:** the rule engine evaluates every active rule and assigns a risk level. The offer's interest rate is then calculated and checked against the rules' rate corridor.
3. **Observation:** the final decision is `ML approves AND all rules pass`. A mismatch with the model is stored as a named override. The customer profile snapshot, prediction and decision are saved together.

The rules have the power to veto the model: they can turn an ML approval into a rejection, but they never approve a customer the model rejected.

### Default business rules

| Rule | Condition |
|---|---|
| Minimum Balance Rule | balance ≥ €1,000 |
| Age Eligibility Rule | 25 ≤ age ≤ 70 |
| No Default History | no credit in default |

All three rules allow an interest-rate corridor of 2%–12%.

### Risk level

A risk score is built from these points:

| Factor | Points |
|---|---|
| Balance < 0 | +3 |
| Balance < 5,000 | +2 |
| Balance < 50,000 | +1 |
| Age < 25 | +2 |
| Age > 65 | +1 |
| Personal loan | +1 |
| Each failed rule | +2 |

A score of ≤ 2 is **Low**, ≤ 5 is **Medium**, and anything higher is **High**.

### Interest rate

```
rate = reference rate (DecisionPolicy:ReferenceRate, default 4%)
     + (1 − ML confidence) × 2%
     + risk: Low 0% | Medium 1.5% | High 3%
     − balance bonus: 0.5% if > 50,000, 0.2% if > 10,000
     − age bonus:     0.5% if > 55,     0.2% if > 45
```

The rate is not clamped. If it falls outside the 2%–12% corridor, the *Interest Rate Policy* rule rejects the offer, and the rejection is recorded in the audit trail.

## ML model

- **Dataset:** UCI Bank Marketing (`bank-full.csv`), 45,211 customers, 11.7% subscribed.
- **Features:** 15 attributes. `duration` is deliberately excluded: the call length is only known after the contact, so using it would leak the target into the prediction.
- **Pipeline:** one-hot encoding of the categorical columns, then min-max normalisation, then `FastTree` (100 trees, 10 leaves).
- **Split:** 80/20 train/test, `seed = 0` (reproducible).

Held-out metrics (`GET /api/v1/model/metrics`):

| Metric | Value |
|---|---|
| Accuracy | 89.3% (always predicting "no" would reach ≈ 88%) |
| AUC-ROC | 79.4% |
| Precision (positive class) | 64.3% |
| Recall (positive class) | 25.1% |
| F1-score | 36.1% |

## Results (from the thesis)

- **Whole dataset:** across all 45,211 customers, the hybrid system raised the subscription rate among selected customers from 11.7% to 72.6%.
- **Reference-rate simulation** (`/dataset/simulate`): the ML predictions stay identical in every scenario, with 2,106 approvals from the AI; only the decision layer changes.
  - At reference rates of 4%–8%, 911 customers are approved.
  - At 12%, approvals fall to 58.
  - At 0%, only 8 are approved.
- **In-process inference:** about two orders of magnitude faster than calling the same model over HTTP.
  - In-process: ~0.005–0.01 ms per prediction.
  - Over HTTP: ~0.6–0.9 ms, over loopback without TLS, which is a lower bound for a real remote service.
  - Measure it yourself with `GET /api/v1/model/benchmark?samples=5000`. Run it several times, because the first calls include JIT warm-up.

## Known limitations

- `PredictionEngine` is not thread-safe, so predictions are serialised with a lock. For high-concurrency production use, `PredictionEnginePool` (Microsoft.Extensions.ML) is the recommended alternative.
- The benchmark runs client and server in the same process on one machine. It shows the order of magnitude, not a production latency figure.
