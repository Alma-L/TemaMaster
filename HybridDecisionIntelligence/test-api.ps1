# Test script for Hybrid Decision Intelligence API
Write-Host "Testing Hybrid Decision Intelligence API..." -ForegroundColor Green

# Test health endpoint
Write-Host "Testing health endpoint..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-WebRequest -Uri "http://localhost:5050/health" -ErrorAction Stop
    Write-Host "Health check: $($healthResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Swagger endpoint
Write-Host "Testing Swagger endpoint..." -ForegroundColor Yellow
try {
    $swaggerResponse = Invoke-WebRequest -Uri "http://localhost:5050/swagger" -ErrorAction Stop
    Write-Host "Swagger UI: $($swaggerResponse.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "Swagger failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test decision endpoint with sample data
Write-Host "Testing decision endpoint..." -ForegroundColor Yellow
$sampleCustomer = @{
    age = 35
    job = "technician"
    marital = "married"
    education = "university.degree"
    balance = 1500
    housing = "yes"
    loan = "no"
    contact = "cellular"
    month = "may"
    poutcome = "success"
} | ConvertTo-Json

try {
    $decisionResponse = Invoke-WebRequest -Uri "http://localhost:5050/api/decisions/make-decision" -Method POST -Body $sampleCustomer -ContentType "application/json" -ErrorAction Stop
    Write-Host "Decision API: $($decisionResponse.StatusCode)" -ForegroundColor Green
    Write-Host "Response: $($decisionResponse.Content)" -ForegroundColor Cyan
} catch {
    Write-Host "Decision API failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "API testing complete!" -ForegroundColor Green