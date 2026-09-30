using MediatR;
using Microsoft.EntityFrameworkCore;
using HybridDecisionIntelligence.Infrastructure.Data;
using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Application.Repositories;
using HybridDecisionIntelligence.Infrastructure.Repositories;
using HybridDecisionIntelligence.Infrastructure.ML;
using HybridDecisionIntelligence.Infrastructure.Reports;
using HybridDecisionIntelligence.Infrastructure.Import;
using HybridDecisionIntelligence.Application.Requests;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Every controller also answers on the unversioned /api/... route; document only v1
    c.DocInclusionPredicate((_, api) => api.RelativePath?.StartsWith("api/v1/") == true);
});

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

// Decision policy: dynamic (macro-economic) parameters, e.g. the reference rate
builder.Services.AddSingleton(
    builder.Configuration.GetSection(DecisionPolicyOptions.SectionName).Get<DecisionPolicyOptions>()
    ?? new DecisionPolicyOptions());

// Application Services
builder.Services.AddScoped<IDecisionEngine, DecisionEngine>();
builder.Services.AddScoped<IBusinessRuleEngine, BusinessRuleEngine>();
builder.Services.AddScoped<IMLPredictor, MLPredictor>();

// Infrastructure Services
// Singleton: the trained model is loaded from disk once and shared by all requests
builder.Services.AddSingleton<IMLModelService, MLNetModelService>();
builder.Services.AddScoped<IDecisionReportGenerator, QuestPdfDecisionReportGenerator>();
builder.Services.AddScoped<IDatasetService, CsvDatasetService>();
builder.Services.AddHttpClient(); // used by the in-process vs HTTP inference benchmark

// Repositories
builder.Services.AddScoped<IDecisionRepository, DecisionRepository>();
builder.Services.AddScoped<IBusinessRuleRepository, BusinessRuleRepository>();
builder.Services.AddScoped<IBankCustomerRepository, BankCustomerRepository>();
builder.Services.AddScoped<IMLPredictionRepository, MLPredictionRepository>();
builder.Services.AddScoped<IUnitOfWork, EFUnitOfWork>();

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

    if (created)
    {
        // Sample customer/prediction/decision and the default rules come from
        // HasData in HybridDecisionContext, inserted by EnsureCreated.
        app.Logger.LogInformation("Database schema created (HybridDecisionIntelligenceDb) with seed data");
    }
    else
    {
        SqlServerSchemaUpgrader.Apply(db);
        app.Logger.LogInformation("Database already exists — schema upgrade checks applied");
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
