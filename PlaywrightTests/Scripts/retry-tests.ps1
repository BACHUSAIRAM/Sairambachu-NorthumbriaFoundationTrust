# retry-tests.ps1
# Northumbria NHS Foundation Trust - Test Retry Script
# Retries failed tests up to a maximum number of attempts

param(
    [int]$MaxRetries = 3,
    [string]$Filter = "",
    [string]$ResultsDir = "TestResults",
    [string]$Configuration = "Debug",
    [switch]$Headless = $false
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Northumbria NHS - Test Retry Runner" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Set environment variables
if ($Headless) {
    $env:PLAYWRIGHT_HEADLESS = "true"
}

$attempt = 1
$success = $false
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$runDir = Join-Path $ResultsDir "Run_$timestamp"

# Create results directory
New-Item -ItemType Directory -Force -Path $runDir | Out-Null

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Max Retries: $MaxRetries"
Write-Host "  Filter: $(if ($Filter) { $Filter } else { '(all tests)' })"
Write-Host "  Results Dir: $runDir"
Write-Host "  Headless: $Headless"
Write-Host ""

while ($attempt -le $MaxRetries -and -not $success) {
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "Test Attempt $attempt of $MaxRetries" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""
    
    $trxFile = Join-Path $runDir "TestResults_Attempt$attempt.trx"
    
    $testArgs = @(
        "test"
        "PlaywrightTests/PlaywrightTests.csproj"
        "--configuration"
        $Configuration
        "--logger"
        "trx;LogFileName=$trxFile"
        "--logger"
        "console;verbosity=detailed"
        "--results-directory"
        $runDir
    )
    
    if ($Filter) {
        $testArgs += "--filter"
        $testArgs += $Filter
    }
    
    & dotnet @testArgs
    
    if ($LASTEXITCODE -eq 0) {
        $success = $true
        Write-Host ""
        Write-Host "======================================" -ForegroundColor Green
        Write-Host "All tests PASSED on attempt $attempt" -ForegroundColor Green
        Write-Host "======================================" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "======================================" -ForegroundColor Yellow
        Write-Host "Some tests FAILED on attempt $attempt" -ForegroundColor Yellow
        Write-Host "======================================" -ForegroundColor Yellow
        
        $attempt++
        
        if ($attempt -le $MaxRetries) {
            Write-Host ""
            Write-Host "Waiting 10 seconds before retry..." -ForegroundColor Cyan
            Start-Sleep -Seconds 10
        }
    }
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Test Run Summary" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Total Attempts: $($attempt - 1)"
Write-Host "Results Directory: $runDir"
Write-Host ""

if (-not $success) {
    Write-Host "======================================" -ForegroundColor Red
    Write-Host "Tests FAILED after $MaxRetries attempts" -ForegroundColor Red
    Write-Host "======================================" -ForegroundColor Red
    exit 1
} else {
    Write-Host "======================================" -ForegroundColor Green
    Write-Host "Test run completed successfully" -ForegroundColor Green
    Write-Host "======================================" -ForegroundColor Green
    exit 0
}
