# Northumbria Healthcare NHS Foundation Trust - Test Automation Framework

[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![Playwright](https://img.shields.io/badge/Playwright-1.48-green)](https://playwright.dev/)
[![UK GDS](https://img.shields.io/badge/UK%20GDS-Compliant-blue)](https://www.gov.uk/service-manual/service-standard)
[![WCAG 2.1 AA](https://img.shields.io/badge/WCAG%202.1-Level%20AA-orange)](https://www.w3.org/WAI/WCAG21/quickref/)

BDD (Cucumber-style) functional acceptance automation for validating the **public website search** functionality.

**Target Website:** `https://www.northumbria.nhs.uk/`

---

## Table of Contents

- [Overview](#overview)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Running Tests](#running-tests)
- [Test Reports](#test-reports)
- [Standards Compliance](#standards-compliance)

---

## Overview

This repository contains the automated test framework for the Northumbria Healthcare NHS Foundation Trust public website. The framework implements:

- **BDD Testing** using Reqnroll (SpecFlow successor)
- **Browser Automation** using Microsoft Playwright
- **Multi-browser Support** (Chrome, Firefox, Edge)
- **Functional Acceptance Testing** (End-to-end user journey validation)
- **Accessibility Testing** (WCAG 2.1 Level AA)
- **Performance Testing** (SLA compliance)

### Test Coverage

| Test Type | Description | Tags |
|-----------|-------------|------|
| **Smoke Tests** | Critical path validation ensuring core functionality works | `@Smoke`, `@CriticalPath` |
| **Functional Acceptance Tests** | End-to-end user scenarios validating business requirements | `@Functional`, `@NHS-101` |
| **Regression Tests** | Comprehensive test suite covering all features | `@Regression` |
| **Accessibility Tests** | WCAG 2.1 AA compliance and screen reader validation | `@Accessibility`, `@NHS-102` |
| **Usability Tests** | Keyboard navigation and mouse interaction testing | `@Usability`, `@Keyboard`, `@Mouse` |
| **Performance Tests** | Page load time and response time validation | `@Performance`, `@NHS-105` |
| **Contrast Tests** | WCAG colour contrast ratio compliance | `@Contrast`, `@WCAG`, `@NHS-106` |
| **Data-Driven Tests** | Parameterised test scenarios from external datasets | `@DataDriven` |
| **Negative Tests** | Edge case and error handling validation | `@Negative` |

### Project Information

| Property | Value |
|----------|-------|
| Organisation | Northumbria Healthcare NHS Foundation Trust |
| Framework | Playwright + Reqnroll + NUnit + .NET 8 |
| Language | C# 12.0 |
| Standards | UK GDS, NHS Digital, WCAG 2.1 AA, ISTQB |

---

## Project Structure

The project follows UK software testing best practices (GDS, NHS Digital, ISTQB):

```
PlaywrightTests/
?
??? Configuration/                       # Configuration Management
?   ??? TestConfiguration.cs             # Main test configuration singleton
?
??? Context/                             # Test Context (Dependency Injection)
?   ??? SpecFlowContext.cs               # Scenario-scoped Playwright context
?
??? Core/                                # Framework Core Components
?   ??? BasePages/                       # Base page classes
?   ?   ??? BaseWebPage.cs               # Reusable base page object
?   ??? Configuration/                   # Application configuration
?       ??? ApplicationConfiguration.cs  # App-agnostic config manager
?
??? Data/                                # Test Data Files
?   ??? search-datasets.json             # JSON test data
?   ??? search-datasets.csv              # CSV test data
?
??? Features/                            # BDD Feature Files (Gherkin)
?   ??? Suites/                          # Test suites by category
?       ??? SmokeSearch.feature          # Critical path smoke tests
?       ??? FunctionalSearch.feature     # Core functional tests
?       ??? RegressionSearch.feature     # Full regression suite
?       ??? DataDrivenSearch.feature     # Data-driven scenarios
?       ??? NonFunctionalAndUsabilitySearch.feature
?
??? Fixtures/                            # Test Fixtures
?   ??? TestSetup.cs                     # Global test setup/teardown
?
??? Hooks/                               # Test Lifecycle Hooks
?   ??? PlaywrightHooks.cs               # Browser setup/teardown
?   ??? XrayIntegrationHooks.cs          # Jira Xray integration
?
??? Integrations/                        # External Integrations
?   ??? Xray/                            # Jira Xray integration
?       ??? XrayClient.cs                # API client
?       ??? XrayModels.cs                # Data models
?
??? Models/                              # Data Models / DTOs
?   ??? SearchDataModels.cs              # Search test data models
?
??? PageObjects/                         # Page Object Model (POM)
?   ??? BasePage.cs                      # Abstract base page
?   ??? HomePage.cs                      # Homepage interactions
?   ??? SearchResultsPage.cs             # Search results interactions
?
??? Resources/                           # Static Resources
?   ??? Accessibility/                   # Accessibility assets
?       ??? cc-contrast-fix.css          # WCAG contrast fixes
?
??? Steps/                               # Step Definitions
?   ??? FunctionalSearchSteps.cs         # Functional step bindings
?   ??? SearchSuiteSteps.cs              # Shared search steps
?   ??? AccessibilitySteps.cs            # Accessibility steps
?   ??? PerformanceSteps.cs              # Performance steps
?   ??? ContrastSteps.cs                 # Contrast validation steps
?
??? tools/                               # External Tools
?   ??? axe/                             # Accessibility testing
?       ??? axe.min.js                   # axe-core library
?
??? Utilities/                           # Helper Utilities
?   ??? AxeHelper.cs                     # Accessibility helper
?   ??? ExtentReportHelper.cs            # Report generation
?   ??? SiteInteractionHelper.cs         # Common interactions
?   ??? TestResultsStorage.cs            # Results persistence
?
??? TestResults/                         # Test Output (gitignored)
?   ??? Run-YYYYMMDD-HHMMSS/             # Timestamped run folders
?
??? appsettings.json                     # Consolidated configuration (all environments)
??? PlaywrightTests.csproj               # Project file
??? README.md                            # This file
```

---

## Prerequisites

- **.NET SDK 8.x**
- **PowerShell 7** (`pwsh`)
- **Git**

---

## Installation

```powershell
# Clone the repository
git clone https://github.com/BACHUSAIRAM/Sairambachu-NorthumbriaFoundationTrust.git
cd Sairambachu-NorthumbriaFoundationTrust/PlaywrightTests

# Restore packages
dotnet restore

# Build the project
dotnet build

# Install Playwright browsers
pwsh ./bin/Debug/net8.0/playwright.ps1 install
```

---

## Configuration

All configuration is managed through a **single consolidated `appsettings.json`** file containing environment-specific settings.

### Configuration Structure

```json
{
  "ActiveEnvironment": "local",
  
  "Environments": {
    "local": { "BaseUrl": "https://www.northumbria.nhs.uk/", "Headless": false, ... },
    "dev": { "BaseUrl": "https://dev.northumbria.nhs.uk/", "Headless": true, ... },
    "staging": { "BaseUrl": "https://staging.northumbria.nhs.uk/", "Headless": true, ... },
    "prod": { "BaseUrl": "https://www.northumbria.nhs.uk/", "Headless": true, ... }
  },
  
  "Playwright": { ... },
  "Reporting": { ... },
  "Accessibility": { ... },
  "Performance": { ... },
  "Xray": { ... }
}
```

### Environment Selection (Priority Order)

1. **Environment Variables** (highest priority)
2. **`ActiveEnvironment`** setting in `appsettings.json`
3. **Default:** `local`

### Switching Environments

```powershell
# Via environment variable (recommended for CI/CD)
$env:DOTNET_ENVIRONMENT = "staging"
dotnet test

# Or use TEST_ENVIRONMENT
$env:TEST_ENVIRONMENT = "prod"
dotnet test
```

### Key Configuration Sections

| Section | Description |
|---------|-------------|
| `Environments` | Environment-specific settings (local, dev, staging, prod) |
| `Playwright` | Browser settings (SlowMo, Viewport, DevTools) |
| `Reporting` | Screenshot and artifact settings |
| `Accessibility` | WCAG compliance settings |
| `Performance` | SLA thresholds (page load, response time) |
| `Xray` | Jira Xray integration settings |
| `Retry` | Test retry configuration |

### Browser Environment Variables

```powershell
# Select browser
$env:PLAYWRIGHT_BROWSER = "chrome"      # chrome, firefox, msedge, webkit

# Enable headless mode
$env:PLAYWRIGHT_HEADLESS = "true"

# Enable multi-browser testing
$env:PLAYWRIGHT_MULTIBROWSER = "1"

# Override base URL
$env:TEST_BASE_URL = "https://custom.url.com/"
```

---

## Running Tests

### Basic Test Execution

```powershell
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run with TRX report
dotnet test --logger "trx;LogFileName=TestResults.trx"
```

### Run by Feature File

```powershell
dotnet test --filter "FullyQualifiedName~SmokeSearch"
dotnet test --filter "FullyQualifiedName~FunctionalSearch"
dotnet test --filter "FullyQualifiedName~RegressionSearch"
```

### Run by Tags

```powershell
# Test types
dotnet test --filter "Category=Smoke"
dotnet test --filter "Category=Functional"
dotnet test --filter "Category=Regression"
dotnet test --filter "Category=Accessibility"
dotnet test --filter "Category=Performance"

# Combine tags
dotnet test --filter "Category=Smoke|Category=Functional"
dotnet test --filter "Category!=Regression"
```

### Run by NHS Requirements

```powershell
dotnet test --filter "Category=NHS-101"  # Functional search
dotnet test --filter "Category=NHS-102"  # Accessibility
dotnet test --filter "Category=NHS-105"  # Performance
dotnet test --filter "Category=NHS-106"  # Contrast/WCAG
```

### Browser Configuration

```powershell
# Run on specific browser
$env:PLAYWRIGHT_BROWSER = "firefox"
dotnet test

# Run on all browsers (Chrome, Firefox, Edge)
$env:PLAYWRIGHT_MULTIBROWSER = "1"
dotnet test

# Run headless
$env:PLAYWRIGHT_HEADLESS = "true"
dotnet test
```

### Environment-Specific Runs

```powershell
# Run against staging
$env:DOTNET_ENVIRONMENT = "staging"
dotnet test --filter "Category=Smoke"

# Run against production
$env:DOTNET_ENVIRONMENT = "prod"
dotnet test --filter "Category=Smoke"
```

---

## Test Reports

### Report Location

`PlaywrightTests/TestResults/Run-YYYYMMDD-HHMMSS/`

### Report Types

| Report | Description |
|--------|-------------|
| `ExtentReport.html` | Rich HTML report with screenshots and logs |
| `*.trx` | Visual Studio test results format |
| `traces/*.zip` | Playwright trace files for debugging |
| `screenshots/` | Test screenshots (failure and success) |
| `videos/` | Test execution recordings |
| `accessibility-reports/` | Axe-core accessibility reports |

### View Reports

```powershell
# Open latest Extent report
$report = Get-ChildItem -Path .\TestResults -Recurse -Filter "ExtentReport.html" |
  Sort-Object LastWriteTime -Descending | Select-Object -First 1
Start-Process $report.FullName
```

---

## Quick Reference - Common Commands

| Task | Command |
|------|---------|
| Run all tests | `dotnet test` |
| Run smoke tests | `dotnet test --filter "Category=Smoke"` |
| Run functional tests | `dotnet test --filter "Category=Functional"` |
| Run accessibility tests | `dotnet test --filter "Category=Accessibility"` |
| Run on Firefox | `$env:PLAYWRIGHT_BROWSER="firefox"; dotnet test` |
| Run headless | `$env:PLAYWRIGHT_HEADLESS="true"; dotnet test` |
| Run multi-browser | `$env:PLAYWRIGHT_MULTIBROWSER="1"; dotnet test` |
| Run on staging | `$env:DOTNET_ENVIRONMENT="staging"; dotnet test` |

---

## Standards Compliance

| Standard | Description |
|----------|-------------|
| **UK GDS** | Government Digital Service Standard |
| **NHS Digital** | NHS Technology Standards |
| **WCAG 2.1 AA** | Web Content Accessibility Guidelines |
| **ISTQB** | Test Automation Best Practices |
| **ISO 25010** | Software Quality Standards |

---

## Author

**SAIRAM BACHU**  
Northumbria Healthcare NHS Foundation Trust

© 2025 All Rights Reserved
