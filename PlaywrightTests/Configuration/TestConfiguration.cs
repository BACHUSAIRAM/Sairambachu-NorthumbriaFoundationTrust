/*
 * ============================================================================
 * Project: Northumbria Healthcare NHS Foundation Trust - Test Automation
 * File: TestConfiguration.cs
 * Purpose: Singleton configuration manager for test environment settings
 * 
 * Description:
 * This class provides centralised configuration management for the test
 * automation framework, following UK GDS (Government Digital Service)
 * standards and NHS Digital guidelines. It implements the Singleton pattern
 * to ensure consistent configuration throughout the test run.
 * 
 * Configuration is loaded from a single appsettings.json file containing:
 * - Environment-specific settings in the "Environments" section
 * - Common settings for Playwright, Reporting, Accessibility, Performance, etc.
 * - Environment variables for runtime overrides (highest priority)
 * 
 * Environment Selection Priority:
 * 1. DOTNET_ENVIRONMENT environment variable
 * 2. TEST_ENVIRONMENT environment variable
 * 3. "ActiveEnvironment" setting in appsettings.json
 * 4. Default: "local"
 * 
 * Standards Compliance:
 * - UK GDS Digital Service Standard
 * - NHS Digital Technology Standards
 * - ISO/IEC 25010:2011 (Software Quality)
 * 
 * Author: SAIRAM BACHU
 * Organisation: Northumbria Healthcare NHS Foundation Trust
 * Last Modified: January 2025
 * Version: 3.0.0
 * ============================================================================
 */

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PlaywrightTests.Configuration
{
    /// <summary>
    /// Manages test configuration - singleton pattern
    /// Based on Microsoft.Extensions.Configuration for flexibility
    /// </summary>
    public class TestConfiguration
    {
        private static IConfiguration? _configuration;
        private static TestConfiguration? _instance;
        private static readonly object _lock = new object();

        #region Core Test Environment Settings

        public string EnvironmentName { get; private set; } = "local";
        public string BaseUrl { get; private set; } = "https://www.northumbria.nhs.uk/";
        public string Browser { get; private set; } = "chrome";
        public bool Headless { get; private set; } = false;
        public int Timeout { get; private set; } = 30000;
        public int ImplicitWait { get; private set; } = 10000;

        #endregion

        #region Playwright Settings

        public int SlowMo { get; private set; } = 50;
        public bool DevTools { get; private set; } = false;
        public int ViewportWidth { get; private set; } = 1920;
        public int ViewportHeight { get; private set; } = 1080;
        public bool UseFullWindow { get; private set; } = true;

        #endregion

        #region Multi-Browser Settings

        public bool MultibrowserEnabled { get; private set; } = false;
        public List<string> Browsers { get; private set; } = new() { "chrome", "firefox", "msedge" };

        #endregion

        #region Reporting Settings

        public bool ScreenshotOnFailure { get; private set; } = true;
        public bool ScreenshotOnSuccess { get; private set; } = false;
        public bool AttachArtifactsOnSuccess { get; private set; } = false;
        public bool AttachThumbnails { get; private set; } = true;
        public int ThumbnailMaxWidth { get; private set; } = 400;
        public int ThumbnailMaxHeight { get; private set; } = 300;
        public string ReportTitle { get; private set; } = "Northumbria NHS - Test Execution Report";

        #endregion

        #region Test Data Settings

        public List<string> SearchTerms { get; private set; } = new();
        public List<string> InvalidSearchTerms { get; private set; } = new();

        #endregion

        #region Feature Flags

        public bool AccessibilityEnabled { get; private set; } = true;
        public bool PerformanceEnabled { get; private set; } = true;
        public bool ContrastEnabled { get; private set; } = true;

        #endregion

        #region Performance Thresholds

        public double MaxPageLoadTimeSeconds { get; private set; } = 3.0;
        public double MaxSearchResponseTimeSeconds { get; private set; } = 2.0;
        public double MaxTimeToInteractiveSeconds { get; private set; } = 5.0;

        #endregion

        #region Retry Settings

        public int MaxRetryAttempts { get; private set; } = 3;
        public int RetryDelayMs { get; private set; } = 10000;

        #endregion

        #region Xray Integration

        public bool XrayEnabled { get; private set; } = false;
        public string XrayBaseUrl { get; private set; } = string.Empty;
        public string XrayProjectKey { get; private set; } = string.Empty;

        #endregion

        /// <summary>
        /// Singleton instance - thread-safe using double-check locking
        /// </summary>
        public static TestConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TestConfiguration();
                            _instance.LoadConfiguration();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Load configuration from the consolidated appsettings.json file
        /// Priority: env vars > environment-specific section > base settings
        /// </summary>
        private void LoadConfiguration()
        {
            // Determine active environment
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                           ?? Environment.GetEnvironmentVariable("TEST_ENVIRONMENT")
                           ?? null;

            // Build configuration from single file
            var builder = new ConfigurationBuilder()
                .SetBasePath(GetBasePath())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Environment variables take precedence - prefix with NTEST_ to avoid conflicts
            builder.AddEnvironmentVariables("NTEST_");

            _configuration = builder.Build();

            // If environment not set via env var, check the config file
            if (string.IsNullOrEmpty(environment))
            {
                environment = _configuration["ActiveEnvironment"] ?? "local";
            }

            // Load all values
            LoadConfigurationValues(environment);

            // Apply Playwright-specific env var overrides
            ApplyEnvironmentVariableOverrides();
        }

        private void LoadConfigurationValues(string environment)
        {
            EnvironmentName = environment;

            // Try to load environment-specific settings from "Environments" section
            var envSection = _configuration!.GetSection($"Environments:{environment}");
            
            if (envSection.Exists())
            {
                // Load from environment-specific section
                BaseUrl = envSection["BaseUrl"] ?? _configuration["TestEnvironment:BaseUrl"] ?? BaseUrl;
                Browser = envSection["Browser"] ?? _configuration["TestEnvironment:Browser"] ?? Browser;
                Headless = bool.Parse(envSection["Headless"] ?? _configuration["TestEnvironment:Headless"] ?? Headless.ToString());
                Timeout = int.Parse(envSection["Timeout"] ?? _configuration["TestEnvironment:Timeout"] ?? Timeout.ToString());
                ImplicitWait = int.Parse(envSection["ImplicitWait"] ?? _configuration["TestEnvironment:ImplicitWait"] ?? ImplicitWait.ToString());
                
                // Multi-browser settings from environment
                if (bool.TryParse(envSection["MultibrowserEnabled"], out var mbEnabled))
                {
                    MultibrowserEnabled = mbEnabled;
                }
                
                var envBrowsers = envSection.GetSection("Browsers").Get<string[]>();
                if (envBrowsers != null && envBrowsers.Length > 0)
                {
                    Browsers = new List<string>(envBrowsers);
                }
            }
            else
            {
                // Fallback to TestEnvironment section
                BaseUrl = _configuration["TestEnvironment:BaseUrl"] ?? BaseUrl;
                Browser = _configuration["TestEnvironment:Browser"] ?? Browser;
                Headless = bool.Parse(_configuration["TestEnvironment:Headless"] ?? Headless.ToString());
                Timeout = int.Parse(_configuration["TestEnvironment:Timeout"] ?? Timeout.ToString());
                ImplicitWait = int.Parse(_configuration["TestEnvironment:ImplicitWait"] ?? ImplicitWait.ToString());
            }

            // Load common Playwright settings
            LoadPlaywrightSettings();

            // Load reporting settings
            LoadReportingSettings();

            // Load test data
            LoadTestDataSettings();

            // Load feature flags
            LoadFeatureFlags();

            // Load performance thresholds
            LoadPerformanceSettings();

            // Load retry settings
            LoadRetrySettings();

            // Load Xray settings
            LoadXraySettings();
        }

        private void LoadPlaywrightSettings()
        {
            SlowMo = int.Parse(_configuration!["Playwright:SlowMo"] ?? SlowMo.ToString());
            DevTools = bool.Parse(_configuration["Playwright:DevTools"] ?? DevTools.ToString());
            ViewportWidth = int.Parse(_configuration["Playwright:ViewportWidth"] ?? ViewportWidth.ToString());
            ViewportHeight = int.Parse(_configuration["Playwright:ViewportHeight"] ?? ViewportHeight.ToString());
            UseFullWindow = bool.Parse(_configuration["Playwright:UseFullWindow"] ?? UseFullWindow.ToString());
        }

        private void LoadReportingSettings()
        {
            ScreenshotOnFailure = bool.Parse(_configuration!["Reporting:ScreenshotOnFailure"] ?? ScreenshotOnFailure.ToString());
            ScreenshotOnSuccess = bool.Parse(_configuration["Reporting:ScreenshotOnSuccess"] ?? ScreenshotOnSuccess.ToString());
            AttachArtifactsOnSuccess = bool.Parse(_configuration["Reporting:AttachArtifactsOnSuccess"] ?? AttachArtifactsOnSuccess.ToString());
            AttachThumbnails = bool.Parse(_configuration["Reporting:AttachThumbnails"] ?? AttachThumbnails.ToString());
            ThumbnailMaxWidth = int.Parse(_configuration["Reporting:ThumbnailMaxWidth"] ?? ThumbnailMaxWidth.ToString());
            ThumbnailMaxHeight = int.Parse(_configuration["Reporting:ThumbnailMaxHeight"] ?? ThumbnailMaxHeight.ToString());
            ReportTitle = _configuration["Reporting:ReportTitle"] ?? ReportTitle;
        }

        private void LoadTestDataSettings()
        {
            var searchTerms = _configuration!.GetSection("TestData:SearchTerms").Get<List<string>>();
            SearchTerms = searchTerms ?? new List<string> { "covid", "appointments" };

            var invalidTerms = _configuration.GetSection("TestData:InvalidSearchTerms").Get<List<string>>();
            InvalidSearchTerms = invalidTerms ?? new List<string>();
        }

        private void LoadFeatureFlags()
        {
            AccessibilityEnabled = bool.Parse(_configuration!["Accessibility:Enabled"] ?? AccessibilityEnabled.ToString());
            PerformanceEnabled = bool.Parse(_configuration["Performance:Enabled"] ?? PerformanceEnabled.ToString());
            ContrastEnabled = bool.Parse(_configuration["Contrast:Enabled"] ?? ContrastEnabled.ToString());
        }

        private void LoadPerformanceSettings()
        {
            if (double.TryParse(_configuration!["Performance:MaxPageLoadTimeSeconds"], out var pageLoad))
                MaxPageLoadTimeSeconds = pageLoad;
            if (double.TryParse(_configuration["Performance:MaxSearchResponseTimeSeconds"], out var searchResponse))
                MaxSearchResponseTimeSeconds = searchResponse;
            if (double.TryParse(_configuration["Performance:MaxTimeToInteractiveSeconds"], out var tti))
                MaxTimeToInteractiveSeconds = tti;
        }

        private void LoadRetrySettings()
        {
            if (int.TryParse(_configuration!["Retry:MaxAttempts"], out var maxAttempts))
                MaxRetryAttempts = maxAttempts;
            if (int.TryParse(_configuration["Retry:DelayBetweenAttemptsMs"], out var delay))
                RetryDelayMs = delay;
        }

        private void LoadXraySettings()
        {
            XrayEnabled = bool.Parse(_configuration!["Xray:Enabled"] ?? XrayEnabled.ToString());
            XrayBaseUrl = _configuration["Xray:BaseUrl"] ?? XrayBaseUrl;
            XrayProjectKey = _configuration["Xray:ProjectKey"] ?? XrayProjectKey;
        }

        /// <summary>
        /// Apply environment variable overrides for Playwright settings
        /// These don't use the NTEST_ prefix for convenience
        /// </summary>
        private void ApplyEnvironmentVariableOverrides()
        {
            // Quick browser switch via env var
            var envSingleBrowser = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSER");
            if (!string.IsNullOrEmpty(envSingleBrowser))
            {
                Browser = envSingleBrowser;
            }

            // Headless mode override
            var envHeadless = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADLESS");
            if (!string.IsNullOrEmpty(envHeadless))
            {
                Headless = envHeadless == "1" || envHeadless.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            // Enable multi-browser mode
            var envMultibrowser = Environment.GetEnvironmentVariable("PLAYWRIGHT_MULTIBROWSER");
            if (!string.IsNullOrEmpty(envMultibrowser) &&
                (envMultibrowser == "1" || envMultibrowser.Equals("true", StringComparison.OrdinalIgnoreCase)))
            {
                MultibrowserEnabled = true;
            }

            // Comma-separated browser list
            var envBrowsers = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS");
            if (!string.IsNullOrEmpty(envBrowsers))
            {
                var list = envBrowsers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (list.Length > 0)
                {
                    Browsers = new List<string>(list);
                }
            }

            // Base URL override
            var envBaseUrl = Environment.GetEnvironmentVariable("TEST_BASE_URL");
            if (!string.IsNullOrEmpty(envBaseUrl))
            {
                BaseUrl = envBaseUrl;
            }
        }

        private string GetBasePath()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assemblyLocation) ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Reset singleton - mainly for testing, don't use in production
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
        /// Get the raw IConfiguration object for advanced scenarios
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
        /// Check if a specific feature is enabled
        /// </summary>
        public bool IsFeatureEnabled(string featureName)
        {
            return featureName.ToLowerInvariant() switch
            {
                "accessibility" => AccessibilityEnabled,
                "performance" => PerformanceEnabled,
                "contrast" => ContrastEnabled,
                "xray" => XrayEnabled,
                _ => false
            };
        }

        public override string ToString()
        {
            return $"Env: {EnvironmentName}, URL: {BaseUrl}, Browser: {Browser}, Headless: {Headless}, MultiBrowser: {MultibrowserEnabled}";
        }
    }
}
