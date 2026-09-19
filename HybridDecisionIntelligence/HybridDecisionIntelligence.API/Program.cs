using MediatR;
using Microsoft.EntityFrameworkCore;
using HybridDecisionIntelligence.Infrastructure.Data;
using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Application.Repositories;
using HybridDecisionIntelligence.Infrastructure.Repositories;
using HybridDecisionIntelligence.Infrastructure.ML;
using HybridDecisionIntelligence.Infrastructure.Reports;
using HybridDecisionIntelligence.Application.Requests;
using HybridDecisionIntelligence.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<HybridDecisionContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        opt => opt.MigrationsAssembly("HybridDecisionIntelligence.Infrastructure")
    )
);

// MediatR
builder.Services.AddMediatR(config =>
    config.RegisterServicesFromAssembly(typeof(MakeDecisionRequest).Assembly)
);

// Application Services
builder.Services.AddScoped<IDecisionEngine, DecisionEngine>();
builder.Services.AddScoped<IBusinessRuleEngine, BusinessRuleEngine>();
builder.Services.AddScoped<IBusinessRuleService, BusinessRuleService>();
builder.Services.AddScoped<IHybridDecisionService, HybridDecisionService>();
builder.Services.AddScoped<IMLPredictor, MLPredictor>();

// Infrastructure Services
builder.Services.AddScoped<IMLModelService, MLNetModelService>();
builder.Services.AddScoped<IDecisionReportGenerator, QuestPdfDecisionReportGenerator>();

// Repositories
builder.Services.AddScoped<IDecisionRepository, DecisionRepository>();
builder.Services.AddScoped<IBusinessRuleRepository, BusinessRuleRepository>();
builder.Services.AddScoped<IBankCustomerRepository, BankCustomerRepository>();
builder.Services.AddScoped<IMLPredictionRepository, MLPredictionRepository>();

// Logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDevClients", builder =>
    {
        builder.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://localhost:3001"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowDevClients");
app.UseAuthorization();
app.MapControllers();

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HybridDecisionContext>();
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var connectionSummary = connectionString is null
        ? "(missing DefaultConnection)"
        : string.Join(';', connectionString.Split(';')
            .Where(p => p.StartsWith("Server=", StringComparison.OrdinalIgnoreCase)
                     || p.StartsWith("Database=", StringComparison.OrdinalIgnoreCase)
                     || p.StartsWith("Trusted_Connection=", StringComparison.OrdinalIgnoreCase)));

    app.Logger.LogInformation("Connecting to SQL Server: {ConnectionSummary}", connectionSummary);

    bool created;
    try
    {
        // EnsureCreated creates the database if missing; CanConnectAsync would fail before that.
        created = db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex,
            "Cannot reach SQL Server or create the database. Ensure SQL Server is running and ConnectionStrings:DefaultConnection is correct ({ConnectionSummary}).",
            connectionSummary);
        throw;
    }

    app.Logger.LogInformation(created
        ? "Database schema created (HybridDecisionIntelligenceDb)"
        : "Database already exists — schema left as-is (EnsureCreated does not alter existing tables)");

    if (!db.HybridDecisions.Any())
    {
        var sampleCustomer = new BankCustomer
        {
            Id = 1,
            Age = 38,
            Job = "technician",
            Marital = "married",
            Education = "tertiary",
            Default = "no",
            Balance = 32000m,
            Housing = "yes",
            Loan = "no",
            Contact = "cellular",
            Day = 12,
            Month = "may",
            Duration = 210,
            Campaign = 2,
            PDays = 999,
            Previous = 0,
            POutcome = "unknown",
            SubscribedToTerm = false,
            CreatedAt = DateTime.UtcNow
        };

        var samplePrediction = new MLPredictionResult
        {
            Id = 1,
            CustomerId = 1,
            PredictedLabel = true,
            Score = 0.82f,
            Probability = 0.82f,
            CreatedAt = DateTime.UtcNow
        };

        var sampleDecision = new HybridDecision
        {
            Id = 1,
            CustomerId = 1,
            MLPredictionResultId = 1,
            MLPredicted = true,
            MLConfidence = 0.82f,
            FinalDecision = true,
            AuditTrail = "ML predicted APPROVE with 82% confidence | Business rules passed | Final decision approved",
            ApprovedInterestRate = 0.045m,
            RulesApplied = "Minimum Balance Rule, Age Eligibility Rule",
            CreatedAt = DateTime.UtcNow,
            WasOverridden = false,
            OverrideReason = string.Empty
        };

        db.BankCustomers.Add(sampleCustomer);
        db.MLPredictionResults.Add(samplePrediction);
        db.HybridDecisions.Add(sampleDecision);
        db.SaveChanges();

        app.Logger.LogInformation("Seeded sample hybrid decision data");
    }

    app.Logger.LogInformation(
        "DB ready — Customers: {Customers}, Predictions: {Predictions}, Decisions: {Decisions}, Rules: {Rules}",
        db.BankCustomers.Count(),
        db.MLPredictionResults.Count(),
        db.HybridDecisions.Count(),
        db.BusinessRules.Count());
}

// ML model bootstrap: train from the UCI Bank Marketing dataset on first run
using (var scope = app.Services.CreateScope())
{
    var modelPath = builder.Configuration["MLModel:ModelPath"] ?? "Models/BankMarketingModel.zip";
    var dataPath = builder.Configuration["MLModel:DataPath"] ?? "Data/BankData.csv";

    if (!File.Exists(modelPath))
    {
        if (File.Exists(dataPath))
        {
            app.Logger.LogInformation("No trained model found at {ModelPath}. Training from {DataPath}...", modelPath, dataPath);
            var modelService = scope.ServiceProvider.GetRequiredService<IMLModelService>();
            var trained = await modelService.TrainModelAsync(dataPath);
            if (trained)
                app.Logger.LogInformation("Model training complete and saved to {ModelPath}", modelPath);
            else
                app.Logger.LogWarning("Model training failed - see previous log entries for details");
        }
        else
        {
            app.Logger.LogWarning(
                "No trained model found at {ModelPath} and no training data found at {DataPath}. " +
                "ML predictions will fail until a model is trained.", modelPath, dataPath);
        }
    }
    else
    {
        app.Logger.LogInformation("Found existing trained model at {ModelPath}", modelPath);
    }
}

app.Run();
