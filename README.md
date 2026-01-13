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
- **Accessibility Testing** (WCAG 2.1 Level AA)
- **Performance Testing** (SLA compliance)

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
??? appsettings.json                     # Base configuration
??? appsettings.dev.json                 # Development environment
??? appsettings.staging.json             # Staging environment
??? appsettings.prod.json                # Production environment
??? appsettings.xray.json                # Xray integration config
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

Configuration is loaded from multiple sources (in priority order):

1. **Environment Variables** (highest priority)
2. **appsettings.{environment}.json** (environment-specific)
3. **appsettings.json** (base configuration)

### Environment Selection

```powershell
$env:DOTNET_ENVIRONMENT = "staging"  # dev, staging, prod
dotnet test
```

### Key Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `TestEnvironment:BaseUrl` | Target website URL | `https://www.northumbria.nhs.uk/` |
| `TestEnvironment:Browser` | Default browser | `chrome` |
| `TestEnvironment:Headless` | Run headless | `false` |
| `TestEnvironment:Timeout` | Operation timeout (ms) | `30000` |
| `Reporting:ScreenshotOnFailure` | Capture screenshots | `true` |

### Browser Environment Variables

```powershell
$env:PLAYWRIGHT_BROWSER = "chrome"      # chrome, firefox, msedge
$env:PLAYWRIGHT_MULTIBROWSER = "1"      # Enable multi-browser
```

---

## Running Tests

### All Tests

```powershell
dotnet test
```

### By Category

```powershell
dotnet test --filter "Category=Smoke"
dotnet test --filter "Category=Functional"
dotnet test --filter "Category=Accessibility"
dotnet test --filter "Category=Regression"
```

### Single Browser

```powershell
$env:PLAYWRIGHT_BROWSER = "firefox"
dotnet test
```

### Multi-Browser

```powershell
$env:PLAYWRIGHT_MULTIBROWSER = "1"
dotnet test
```

---

## Test Reports

### Report Location

`PlaywrightTests/TestResults/Run-YYYYMMDD-HHMMSS/`

### Report Types

- **Extent HTML Report** - `ExtentReport.html`
- **TRX Report** - `--logger "trx"`
- **Playwright Traces** - `traces/*.zip`

### View Reports

```powershell
# Open latest Extent report
$report = Get-ChildItem -Path .\TestResults -Recurse -Filter "ExtentReport.html" |
  Sort-Object LastWriteTime -Descending | Select-Object -First 1
Start-Process $report.FullName
```

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
