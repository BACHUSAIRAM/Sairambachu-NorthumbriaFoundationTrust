# rerun-failed.ps1
# Northumbria NHS Foundation Trust - Rerun Failed Tests Script
# Parses TRX file and reruns only the failed tests

param(
    [Parameter(Mandatory=$true)]
    [string]$TrxFile,
    [int]$MaxRetries = 2,
    [string]$Configuration = "Debug"
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Northumbria NHS - Rerun Failed Tests" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Validate TRX file exists
if (-not (Test-Path $TrxFile)) {
    Write-Host "ERROR: TRX file not found: $TrxFile" -ForegroundColor Red
    exit 1
}

Write-Host "Parsing TRX file: $TrxFile" -ForegroundColor Yellow
Write-Host ""

# Parse TRX file and get failed test names
try {
    [xml]$trx = Get-Content $TrxFile
    
    $failedTests = $trx.TestRun.Results.UnitTestResult | 
        Where-Object { $_.outcome -eq "Failed" } | 
        Select-Object -ExpandProperty testName
    
    if ($null -eq $failedTests -or $failedTests.Count -eq 0) {
        Write-Host "======================================" -ForegroundColor Green
        Write-Host "No failed tests found - nothing to rerun" -ForegroundColor Green
        Write-Host "======================================" -ForegroundColor Green
        exit 0
    }
    
    Write-Host "Found $($failedTests.Count) failed test(s):" -ForegroundColor Yellow
    foreach ($test in $failedTests) {
        Write-Host "  - $test" -ForegroundColor Yellow
    }
    Write-Host ""
    
} catch {
    Write-Host "ERROR: Failed to parse TRX file: $_" -ForegroundColor Red
    exit 1
}

# Build filter for failed tests
$filterParts = $failedTests | ForEach-Object { "Name~$_" }
$filter = $filterParts -join "|"

Write-Host "Test filter: $filter" -ForegroundColor Cyan
Write-Host ""

# Rerun failed tests
$resultsDir = Split-Path $TrxFile -Parent
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

for ($i = 1; $i -le $MaxRetries; $i++) {
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "Rerun Attempt $i of $MaxRetries" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""
    
    $rerunTrxFile = Join-Path $resultsDir "Rerun_Attempt${i}_$timestamp.trx"
    
    dotnet test PlaywrightTests/PlaywrightTests.csproj `
        --configuration $Configuration `
        --filter "$filter" `
        --logger "trx;LogFileName=$rerunTrxFile" `
        --logger "console;verbosity=detailed" `
        --results-directory $resultsDir
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "======================================" -ForegroundColor Green
        Write-Host "All failed tests passed on rerun attempt $i" -ForegroundColor Green
        Write-Host "======================================" -ForegroundColor Green
        exit 0
    } else {
        Write-Host ""
        Write-Host "Some tests still failing on rerun attempt $i" -ForegroundColor Yellow
        
        if ($i -lt $MaxRetries) {
            Write-Host "Waiting 10 seconds before next retry..." -ForegroundColor Cyan
            Start-Sleep -Seconds 10
        }
    }
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Red
Write-Host "Failed tests did not pass after $MaxRetries rerun attempts" -ForegroundColor Red
Write-Host "======================================" -ForegroundColor Red
exit 1
