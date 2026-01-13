# run-tests-by-suite.ps1
# Northumbria NHS Foundation Trust - Run Tests by Suite
# Convenient script to run tests by test suite with retry logic

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Smoke", "Functional", "Regression", "Accessibility", "Performance", "All")]
    [string]$Suite,
    
    [int]$MaxRetries = 3,
    [string]$Browser = "chrome",
    [switch]$Headless = $false,
    [string]$Configuration = "Debug"
)

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Northumbria NHS - Test Suite Runner" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Set environment variables
$env:PLAYWRIGHT_BROWSER = $Browser
if ($Headless) {
    $env:PLAYWRIGHT_HEADLESS = "true"
} else {
    $env:PLAYWRIGHT_HEADLESS = "false"
}

# Map suite to filter
$filter = switch ($Suite) {
    "Smoke"         { "Category=Smoke" }
    "Functional"    { "Category=Functional" }
    "Regression"    { "Category=Regression" }
    "Accessibility" { "Category=Accessibility" }
    "Performance"   { "Category=Performance" }
    "All"           { "" }
}

Write-Host "Suite: $Suite" -ForegroundColor Yellow
Write-Host "Browser: $Browser" -ForegroundColor Yellow
Write-Host "Headless: $Headless" -ForegroundColor Yellow
Write-Host "Max Retries: $MaxRetries" -ForegroundColor Yellow
Write-Host "Filter: $(if ($filter) { $filter } else { '(all tests)' })" -ForegroundColor Yellow
Write-Host ""

# Call retry-tests.ps1
$scriptPath = Join-Path $PSScriptRoot "retry-tests.ps1"

$retryArgs = @{
    MaxRetries = $MaxRetries
    Configuration = $Configuration
}

if ($filter) {
    $retryArgs.Filter = $filter
}

if ($Headless) {
    $retryArgs.Headless = $true
}

& $scriptPath @retryArgs
exit $LASTEXITCODE
