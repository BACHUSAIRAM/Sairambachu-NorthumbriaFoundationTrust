================================================================================
FOLDER STRUCTURE - UK SOFTWARE TESTING STANDARDS
================================================================================
Project: Northumbria Healthcare NHS Foundation Trust - Test Automation Framework
Standards: UK GDS Digital Service Standard, NHS Digital, ISTQB Best Practices
Author: SAIRAM BACHU
Version: 3.0.0
Last Updated: January 2025
================================================================================

PlaywrightTests/
?
??? ?? Configuration/                    # Configuration Management
?   ??? TestConfiguration.cs             # Main test configuration singleton
?
??? ?? Context/                          # Test Context (Dependency Injection)
?   ??? SpecFlowContext.cs               # Scenario-scoped Playwright context
?
??? ?? Core/                             # Framework Core Components
?   ??? ?? BasePages/                    # Base page classes
?   ?   ??? BaseWebPage.cs               # Reusable base page object
?   ??? ?? Configuration/                # Application configuration
?       ??? ApplicationConfiguration.cs  # App-agnostic config manager
?
??? ?? Data/                             # Test Data Files
?   ??? search-datasets.json             # JSON test data
?   ??? search-datasets.csv              # CSV test data
?
??? ?? Features/                         # BDD Feature Files (Gherkin)
?   ??? ?? Suites/                       # Test suites by category
?       ??? SmokeSearch.feature          # Critical path smoke tests
?       ??? FunctionalSearch.feature     # Core functional tests
?       ??? RegressionSearch.feature     # Full regression suite
?       ??? DataDrivenSearch.feature     # Data-driven scenarios
?       ??? NonFunctionalAndUsabilitySearch.feature
?
??? ?? Fixtures/                         # Test Fixtures
?   ??? TestSetup.cs                     # Global test setup/teardown
?
??? ?? Hooks/                            # Test Lifecycle Hooks
?   ??? PlaywrightHooks.cs               # Browser setup/teardown
?   ??? XrayIntegrationHooks.cs          # Jira Xray integration
?
??? ?? Integrations/                     # External Integrations
?   ??? ?? Xray/                         # Jira Xray integration
?       ??? XrayClient.cs                # API client
?       ??? XrayModels.cs                # Data models
?
??? ?? Models/                           # Data Models / DTOs
?   ??? SearchDataModels.cs              # Search test data models
?
??? ?? PageObjects/                      # Page Object Model (POM)
?   ??? BasePage.cs                      # Abstract base page
?   ??? HomePage.cs                      # Homepage interactions
?   ??? SearchResultsPage.cs             # Search results interactions
?
??? ?? Resources/                        # Static Resources
?   ??? ?? Accessibility/                # Accessibility assets
?       ??? cc-contrast-fix.css          # WCAG contrast fixes
?
??? ?? Steps/                            # Step Definitions
?   ??? FunctionalSearchSteps.cs         # Functional step bindings
?   ??? SearchSuiteSteps.cs              # Shared search steps
?   ??? AccessibilitySteps.cs            # Accessibility steps
?   ??? PerformanceSteps.cs              # Performance steps
?   ??? ContrastSteps.cs                 # Contrast validation steps
?
??? ?? tools/                            # External Tools
?   ??? ?? axe/                          # Accessibility testing
?       ??? axe.min.js                   # axe-core library (WCAG 2.1)
?
??? ?? Utilities/                        # Helper Utilities
?   ??? AxeHelper.cs                     # Accessibility helper
?   ??? ExtentReportHelper.cs            # Report generation
?   ??? SiteInteractionHelper.cs         # Common interactions
?   ??? TestResultsStorage.cs            # Results persistence
?
??? ?? TestResults/                      # Test Output (Git Ignored)
?   ??? Run-YYYYMMDD-HHMMSS/             # Timestamped run folders
?       ??? screenshots/                 # Failure screenshots
?       ??? videos/                      # Test recordings
?       ??? traces/                      # Playwright traces
?       ??? logs/                        # Execution logs
?       ??? evidence/                    # Test evidence
?       ??? accessibility-reports/       # axe results
?       ??? performance-reports/         # Performance metrics
?       ??? ExtentReport.html            # HTML report
?
??? ?? appsettings.json                  # Base configuration
??? ?? appsettings.dev.json              # Development environment
??? ?? appsettings.staging.json          # Staging environment
??? ?? appsettings.prod.json             # Production environment
??? ?? appsettings.xray.json             # Xray integration config
??? ?? PlaywrightTests.csproj            # Project file
??? ?? README.md                         # Project documentation
??? ?? FOLDER_STRUCTURE.md               # This file

================================================================================
SOLUTION ROOT (Parent Directory)
================================================================================

sairamBachu-NorthumbriaFoundationTrust/
?
??? ?? .github/                          # GitHub Configuration
?   ??? ?? workflows/                    # GitHub Actions Workflows
?       ??? tests.yml                    # Main test workflow
?       ??? playwright-matrix.yml        # Multi-browser matrix tests
?
??? ?? ci/                               # CI/CD Configuration
?   ??? Jenkinsfile                      # Jenkins pipeline definition
?   ??? azure-pipelines.yml              # Azure DevOps pipeline
?
??? ?? PlaywrightTests/                  # Main Test Project
?   ??? (see structure above)
?
??? .gitignore                           # Git ignore rules
??? README.md                            # Solution documentation

================================================================================
STANDARDS COMPLIANCE
================================================================================

1. UK GDS (Government Digital Service) Standards:
   - Consistent folder naming conventions
   - Clear separation of concerns
   - Evidence and audit trail capabilities
   - Accessibility testing integration (WCAG 2.1 AA)

2. NHS Digital Technology Standards:
   - Healthcare data handling compliance
   - Security-first approach
   - Comprehensive logging and reporting

3. ISTQB Best Practices:
   - Test pyramid structure (Smoke ? Functional ? Regression)
   - Clear test categorisation
   - Reusable test components
   - Data-driven testing support

4. BDD (Behaviour-Driven Development):
   - Feature files in natural language (Gherkin)
   - Step definitions linked to features
   - Living documentation approach

5. Page Object Model (POM):
   - Separation of test logic from UI interactions
   - Reusable page components
   - Maintainable selectors

================================================================================
FOLDER DESCRIPTIONS
================================================================================

Configuration/
  Purpose: Centralised configuration management
  Contains: Singleton configuration class
  Standards: 12-factor app configuration principles

Context/
  Purpose: Test execution context management
  Contains: Dependency injection, scenario context
  Standards: SpecFlow/Reqnroll DI patterns

Core/
  Purpose: Framework foundation classes
  Contains: Base classes, application config
  Standards: DRY principle, inheritance hierarchy

Data/
  Purpose: Test data management
  Contains: JSON, CSV test data files
  Standards: External test data, data-driven testing

Features/
  Purpose: BDD feature files in Gherkin syntax
  Contains: .feature files organised by test type
  Standards: Given-When-Then format, scenario outlines

Fixtures/
  Purpose: Test setup and teardown
  Contains: NUnit fixtures, one-time setup
  Standards: Test isolation, clean state

Hooks/
  Purpose: Test lifecycle management
  Contains: Before/After hooks for scenarios
  Standards: Cross-cutting concerns, AOP

Integrations/
  Purpose: External tool connections
  Contains: Xray, ALM, CI/CD integrations
  Standards: Interface segregation, loose coupling

Models/
  Purpose: Data transfer objects and models
  Contains: POCO classes for test data
  Standards: Immutable DTOs where possible

PageObjects/
  Purpose: UI abstraction layer
  Contains: Page classes with element interactions
  Standards: POM pattern, fluent interfaces

Resources/
  Purpose: Static assets for testing
  Contains: CSS, accessibility fixes
  Standards: WCAG 2.1 compliance support

Steps/
  Purpose: BDD step implementations
  Contains: Step definition classes
  Standards: Single responsibility, reusability

tools/
  Purpose: External testing libraries
  Contains: axe-core for accessibility testing
  Standards: Third-party tool management

Utilities/
  Purpose: Helper classes and extensions
  Contains: Common utilities, helpers
  Standards: Static helpers, extension methods

TestResults/
  Purpose: Test execution output
  Contains: Reports, screenshots, logs
  Standards: UK GDS evidence requirements

================================================================================
