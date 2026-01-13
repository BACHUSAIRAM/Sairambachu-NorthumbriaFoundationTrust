/*
 * ============================================================================
 * Project: Reusable Test Automation Framework
 * File: ApplicationConfiguration.cs
 * Purpose: Application-agnostic configuration for any web application
 * 
 * Description:
 * This configuration class is designed to work with ANY web application.
 * Simply update appsettings.json with your application details and the
 * framework will automatically adapt.
 * 
 * Key Features:
 * - Application-agnostic design
 * - Configurable branding
 * - Multi-application support
 * - Environment-based settings
 * - Dynamic feature enablement
 * 
 * Usage:
 * 1. Update appsettings.json with your application details
 * 2. Create application-specific page objects
 * 3. Write BDD scenarios
 * 4. Run tests!
 * 
 * Standards Compliance:
 * - UK GDS Digital Service Standard
 * - ISO/IEC 25010:2011 (Software Quality)
 * - WCAG 2.1 Level AA (Accessibility)
 * 
 * Author: SAIRAM BACHU
 * Organisation: Northumbria Healthcare NHS Foundation Trust
 * Version: 3.0.0 - Reusable Architecture
 * Last Modified: January 2025
 * ============================================================================
 */

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PlaywrightTests.Core.Configuration
{
    /// <summary>
    /// Application-agnostic configuration manager for test automation framework.
    /// </summary>
    /// <remarks>
    /// This class manages configuration for ANY web application. Simply update
    /// your appsettings.json file to point to a different application and the
    /// framework will automatically adapt.
    /// </remarks>
    public class ApplicationConfiguration
    {
        #region Private Fields

        private static IConfiguration? _configuration;
        private static ApplicationConfiguration? _instance;
        private static readonly object _lock = new object();

        #endregion

        #region Application Settings

        /// <summary>
        /// Gets the application name being tested.
        /// </summary>
        /// <remarks>
        /// Examples: "Northumbria NHS", "E-Commerce Platform", "Banking Portal"
        /// </remarks>
        public string ApplicationName { get; private set; } = "Web Application";

        /// <summary>
        /// Gets the application type/category.
        /// </summary>
        /// <remarks>
        /// Examples: "Healthcare", "E-Commerce", "Banking", "Government", "SaaS"
        /// </remarks>
        public string ApplicationType { get; private set; } = "WebApplication";

        /// <summary>
        /// Gets the base URL for the application under test.
        /// </summary>
        /// <remarks>
        /// This URL is used as the starting point for all navigation actions.
        /// Can be changed per environment (local, dev, staging, prod).
        /// </remarks>
        public string BaseUrl { get; private set; } = "https://example.com/";

        /// <summary>
        /// Gets the application version being tested.
        /// </summary>
        public string ApplicationVersion { get; private set; } = "1.0.0";

        #endregion

        #region Organisation/Branding Settings

        /// <summary>
        /// Gets the organisation name for reporting and branding.
        /// </summary>
        /// <remarks>
        /// This appears in test reports and evidence logs.
        /// </remarks>
        public string OrganisationName { get; private set; } = "Test Organisation";

        /// <summary>
        /// Gets the report title for test execution reports.
        /// </summary>
        public string ReportTitle { get; private set; } = "Test Automation Report";

        /// <summary>
        /// Gets the theme colour for reports (hex format).
        /// </summary>
        /// <remarks>
        /// Examples: "#005eb8" (NHS Blue), "#0078d4" (Azure Blue), "#00a300" (Green)
        /// </remarks>
        public string ThemeColour { get; private set; } = "#005eb8";

        /// <summary>
        /// Gets the logo URL for reports.
        /// </summary>
        public string LogoUrl { get; private set; } = string.Empty;

        #endregion

        #region Test Environment Settings

        /// <summary>
        /// Gets the current test environment name.
        /// </summary>
        /// <remarks>
        /// Standard values: local, dev, qa, uat, staging, prod
        /// </remarks>
        public string EnvironmentName { get; private set; } = "local";

        /// <summary>
        /// Gets the default browser to use for testing.
        /// </summary>
        /// <remarks>
        /// Supported: chrome, firefox, msedge, webkit
        /// </remarks>
        public string Browser { get; private set; } = "chrome";

        /// <summary>
        /// Gets whether browsers should run in headless mode.
        /// </summary>
        public bool Headless { get; private set; } = true;

        /// <summary>
        /// Gets the default timeout for page operations in milliseconds.
        /// </summary>
        public int Timeout { get; private set; } = 30000;

        /// <summary>
        /// Gets the implicit wait time for element visibility in milliseconds.
        /// </summary>
        public int ImplicitWait { get; private set; } = 10000;

        /// <summary>
        /// Gets the slow motion delay in milliseconds between actions.
        /// </summary>
        public int SlowMo { get; private set; } = 0;

        #endregion

        #region Feature Flags

        /// <summary>
        /// Gets whether accessibility testing is enabled.
        /// </summary>
        /// <remarks>
        /// When enabled, WCAG 2.1 AA compliance tests will run automatically.
        /// </remarks>
        public bool EnableAccessibilityTests { get; private set; } = true;

        /// <summary>
        /// Gets whether performance testing is enabled.
        /// </summary>
        /// <remarks>
        /// When enabled, page load times and performance metrics will be measured.
        /// </remarks>
        public bool EnablePerformanceTests { get; private set; } = true;

        /// <summary>
        /// Gets whether contrast testing is enabled.
        /// </summary>
        /// <remarks>
        /// When enabled, colour contrast ratios will be validated against WCAG standards.
        /// </remarks>
        public bool EnableContrastTests { get; private set; } = true;

        /// <summary>
        /// Gets whether security testing is enabled.
        /// </summary>
        /// <remarks>
        /// When enabled, security headers and SSL certificates will be validated.
        /// </remarks>
        public bool EnableSecurityTests { get; private set; } = false;

        /// <summary>
        /// Gets whether API testing is enabled.
        /// </summary>
        /// <remarks>
        /// When enabled, backend API endpoints will be tested alongside UI tests.
        /// </remarks>
        public bool EnableApiTests { get; private set; } = false;

        #endregion

        #region Multi-Browser Settings

        /// <summary>
        /// Gets whether multi-browser testing is enabled.
        /// </summary>
        public bool MultibrowserEnabled { get; private set; } = false;

        /// <summary>
        /// Gets the list of browsers to use when multi-browser mode is enabled.
        /// </summary>
        public List<string> Browsers { get; private set; } = new() { "chrome", "firefox", "msedge" };

        #endregion

        #region Reporting Settings

        /// <summary>
        /// Gets whether screenshots should be captured on test failure.
        /// </summary>
        public bool ScreenshotOnFailure { get; private set; } = true;

        /// <summary>
        /// Gets whether artifacts should be attached on successful tests.
        /// </summary>
        /// <remarks>
        /// Recommended for audit and compliance purposes (UK GDS standards).
        /// </remarks>
        public bool AttachArtifactsOnSuccess { get; private set; } = false;

        /// <summary>
        /// Gets whether video recording is enabled.
        /// </summary>
        public bool EnableVideoRecording { get; private set; } = true;

        /// <summary>
        /// Gets whether trace recording is enabled.
        /// </summary>
        public bool EnableTraceRecording { get; private set; } = true;

        #endregion

        #region Test Data Settings

        /// <summary>
        /// Gets the list of search terms to use in test scenarios.
        /// </summary>
        /// <remarks>
        /// These can be application-specific test data values.
        /// </remarks>
        public List<string> TestSearchTerms { get; private set; } = new();

        /// <summary>
        /// Gets additional test data as key-value pairs.
        /// </summary>
        /// <remarks>
        /// Use this for application-specific test data that doesn't fit standard properties.
        /// </remarks>
        public Dictionary<string, string> TestData { get; private set; } = new();

        #endregion

        #region Singleton Instance

        /// <summary>
        /// Gets the singleton instance of ApplicationConfiguration.
        /// </summary>
        public static ApplicationConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ApplicationConfiguration();
                            _instance.LoadConfiguration();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads configuration from JSON files and environment variables.
        /// </summary>
        private void LoadConfiguration()
        {
            // Determine environment
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") 
                           ?? Environment.GetEnvironmentVariable("TEST_ENVIRONMENT")
                           ?? "local";

            // Build configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(GetBasePath())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Add environment-specific config
            if (!environment.Equals("local", StringComparison.OrdinalIgnoreCase))
            {
                builder.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
            }

            // Add environment variables
            builder.AddEnvironmentVariables("TEST_");

            _configuration = builder.Build();

            // Load all values
            LoadConfigurationValues(environment);
        }

        /// <summary>
        /// Loads configuration values from the IConfiguration object.
        /// </summary>
        private void LoadConfigurationValues(string environment)
        {
            // Application settings
            ApplicationName = _configuration["Application:Name"] ?? ApplicationName;
            ApplicationType = _configuration["Application:Type"] ?? ApplicationType;
            BaseUrl = _configuration["Application:BaseUrl"] ?? BaseUrl;
            ApplicationVersion = _configuration["Application:Version"] ?? ApplicationVersion;

            // Organisation/Branding
            OrganisationName = _configuration["Branding:OrganisationName"] ?? OrganisationName;
            ReportTitle = _configuration["Branding:ReportTitle"] ?? ReportTitle;
            ThemeColour = _configuration["Branding:ThemeColour"] ?? ThemeColour;
            LogoUrl = _configuration["Branding:LogoUrl"] ?? LogoUrl;

            // Test Environment
            EnvironmentName = environment;
            Browser = _configuration["Testing:Browser"] ?? Browser;
            Headless = bool.Parse(_configuration["Testing:Headless"] ?? Headless.ToString());
            Timeout = int.Parse(_configuration["Testing:Timeout"] ?? Timeout.ToString());
            ImplicitWait = int.Parse(_configuration["Testing:ImplicitWait"] ?? ImplicitWait.ToString());
            SlowMo = int.Parse(_configuration["Testing:SlowMo"] ?? SlowMo.ToString());

            // Feature Flags
            EnableAccessibilityTests = bool.Parse(_configuration["Features:EnableAccessibilityTests"] ?? EnableAccessibilityTests.ToString());
            EnablePerformanceTests = bool.Parse(_configuration["Features:EnablePerformanceTests"] ?? EnablePerformanceTests.ToString());
            EnableContrastTests = bool.Parse(_configuration["Features:EnableContrastTests"] ?? EnableContrastTests.ToString());
            EnableSecurityTests = bool.Parse(_configuration["Features:EnableSecurityTests"] ?? EnableSecurityTests.ToString());
            EnableApiTests = bool.Parse(_configuration["Features:EnableApiTests"] ?? EnableApiTests.ToString());

            // Multi-browser
            MultibrowserEnabled = bool.Parse(_configuration["Testing:MultibrowserEnabled"] ?? MultibrowserEnabled.ToString());
            var browsers = _configuration.GetSection("Testing:Browsers").Get<string[]>();
            if (browsers != null && browsers.Length > 0)
            {
                Browsers = new List<string>(browsers);
            }

            // Reporting
            ScreenshotOnFailure = bool.Parse(_configuration["Reporting:ScreenshotOnFailure"] ?? ScreenshotOnFailure.ToString());
            AttachArtifactsOnSuccess = bool.Parse(_configuration["Reporting:AttachArtifactsOnSuccess"] ?? AttachArtifactsOnSuccess.ToString());
            EnableVideoRecording = bool.Parse(_configuration["Reporting:EnableVideoRecording"] ?? EnableVideoRecording.ToString());
            EnableTraceRecording = bool.Parse(_configuration["Reporting:EnableTraceRecording"] ?? EnableTraceRecording.ToString());

            // Test Data
            var searchTerms = _configuration.GetSection("TestData:SearchTerms").Get<List<string>>();
            TestSearchTerms = searchTerms ?? new List<string>();

            // Additional test data
            var testDataSection = _configuration.GetSection("TestData:Additional");
            if (testDataSection.Exists())
            {
                TestData = testDataSection.Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
            }

            // Apply environment variable overrides
            ApplyEnvironmentVariableOverrides();
        }

        /// <summary>
        /// Applies environment variable overrides.
        /// </summary>
        private void ApplyEnvironmentVariableOverrides()
        {
            var envBrowser = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSER");
            if (!string.IsNullOrEmpty(envBrowser))
            {
                Browser = envBrowser;
            }

            var envMultibrowser = Environment.GetEnvironmentVariable("PLAYWRIGHT_MULTIBROWSER");
            if (!string.IsNullOrEmpty(envMultibrowser) && 
                (envMultibrowser == "1" || envMultibrowser.Equals("true", StringComparison.OrdinalIgnoreCase)))
            {
                MultibrowserEnabled = true;
            }

            var envBaseUrl = Environment.GetEnvironmentVariable("TEST_BASE_URL");
            if (!string.IsNullOrEmpty(envBaseUrl))
            {
                BaseUrl = envBaseUrl;
            }
        }

        /// <summary>
        /// Gets the base path for configuration files.
        /// </summary>
        private string GetBasePath()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assemblyLocation) ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the singleton instance (for testing purposes).
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _configuration = null;
                _instance = null;
            }
        }

        /// <summary>
        /// Gets the raw IConfiguration object.
        /// </summary>
        public static IConfiguration GetConfiguration()
        {
            if (_configuration == null)
            {
                var _ = Instance;
            }
            return _configuration!;
        }

        /// <summary>
        /// Returns a string representation of the current configuration.
        /// </summary>
        public override string ToString()
        {
            return $"Application: {ApplicationName}, " +
                   $"Environment: {EnvironmentName}, " +
                   $"BaseUrl: {BaseUrl}, " +
                   $"Browser: {Browser}, " +
                   $"Features: A11y={EnableAccessibilityTests}, Perf={EnablePerformanceTests}, Contrast={EnableContrastTests}";
        }

        /// <summary>
        /// Checks if a specific feature is enabled.
        /// </summary>
        public bool IsFeatureEnabled(string featureName)
        {
            return featureName.ToLowerInvariant() switch
            {
                "accessibility" => EnableAccessibilityTests,
                "performance" => EnablePerformanceTests,
                "contrast" => EnableContrastTests,
                "security" => EnableSecurityTests,
                "api" => EnableApiTests,
                _ => false
            };
        }

        #endregion
    }
}
