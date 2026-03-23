using MediatR;
using Microsoft.EntityFrameworkCore;
using HybridDecisionIntelligence.Infrastructure.Data;
using HybridDecisionIntelligence.Application.Services;
using HybridDecisionIntelligence.Application.Repositories;
using HybridDecisionIntelligence.Infrastructure.Repositories;
using HybridDecisionIntelligence.Infrastructure.ML;
using HybridDecisionIntelligence.Application.Requests;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<HybridDecisionContext>(options =>
    options.UseSqlite(
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
    options.AddPolicy("AllowViteDev", builder =>
    {
        builder.WithOrigins("http://localhost:5173")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
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
app.UseCors("AllowViteDev");
app.UseAuthorization();
app.MapControllers();

// Database initialization
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HybridDecisionContext>();
    db.Database.Migrate();
    app.Logger.LogInformation("Database migrations applied successfully");
}

app.Run();
