using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PlaywrightTests.Integrations.Xray;
using Reqnroll;

namespace PlaywrightTests.Hooks
{
    [Binding]
    public class XrayIntegrationHooks
    {
        private static XrayClient? _xrayClient;
        private static XrayConfiguration? _xrayConfig;
        private static XrayTestExecution? _currentExecution;
        private static readonly Dictionary<string, XrayTestResult> _testResults = new Dictionary<string, XrayTestResult>();

        private readonly ScenarioContext _scenarioContext;
        private XrayTestResult? _currentTestResult;
        private DateTime _testStartTime;

        public XrayIntegrationHooks(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [BeforeTestRun]
        public static void InitializeXrayIntegration()
        {
            try
            {
                // Load Xray configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile("appsettings.xray.json", optional: true)
                    .AddEnvironmentVariables("XRAY_")
                    .Build();

                _xrayConfig = new XrayConfiguration();
                configuration.GetSection("Xray").Bind(_xrayConfig);

                if (_xrayConfig.Enabled)
                {
                    Console.WriteLine("[Xray] Initializing Xray integration...");
                    _xrayClient = new XrayClient(_xrayConfig);

                    // Initialize test execution
                    _currentExecution = new XrayTestExecution
                    {
                        Key = _xrayConfig.TestExecutionKey,
                        Summary = $"{_xrayConfig.TestExecutionSummaryPrefix} {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                        Description = $"Automated test run executed on {Environment.MachineName} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                    };

                    Console.WriteLine($"[Xray] ? Xray integration initialized");
                    Console.WriteLine($"[Xray] Project: {_xrayConfig.ProjectKey}");
                    Console.WriteLine($"[Xray] Test Execution: {_xrayConfig.TestExecutionKey ?? "Auto-create"}");
                }
                else
                {
                    Console.WriteLine("[Xray] Xray integration is disabled in configuration");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Xray] ?? Failed to initialize Xray integration: {ex.Message}");
            }
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            if (_xrayConfig?.Enabled != true) return;

            try
            {
                _testStartTime = DateTime.Now;

                // Extract Xray test key from scenario tags
                var xrayTestKey = ExtractXrayTestKey();

                if (!string.IsNullOrEmpty(xrayTestKey))
                {
                    Console.WriteLine($"[Xray] Starting test: {xrayTestKey}");

                    _currentTestResult = new XrayTestResult
                    {
                        TestKey = xrayTestKey,
                        Start = _testStartTime,
                        Status = "EXECUTING"
                    };

                    _testResults[xrayTestKey] = _currentTestResult;
                }
                else
                {
                    Console.WriteLine($"[Xray] ?? No Xray test key found in scenario tags: {_scenarioContext.ScenarioInfo.Title}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Xray] ?? Error in BeforeScenario: {ex.Message}");
            }
        }

        [AfterScenario]
        public void AfterScenario()
        {
            if (_xrayConfig?.Enabled != true || _currentTestResult == null) return;

            try
            {
                _currentTestResult.Finish = DateTime.Now;

                // Determine status
                if (_scenarioContext.TestError != null)
                {
                    _currentTestResult.Status = "FAIL";
                    _currentTestResult.Comment = $"Test failed: {_scenarioContext.TestError.Message}\n\nStack Trace:\n{_scenarioContext.TestError.StackTrace}";
                }
                else if (_scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.StepDefinitionPending)
                {
                    _currentTestResult.Status = "TODO";
                    _currentTestResult.Comment = "Test has pending step definitions";
                }
                else if (_scenarioContext.ScenarioExecutionStatus == ScenarioExecutionStatus.Skipped)
                {
                    _currentTestResult.Status = "ABORTED";
                    _currentTestResult.Comment = "Test was skipped";
                }
                else
                {
                    _currentTestResult.Status = "PASS";
                    _currentTestResult.Comment = $"Test passed successfully. Duration: {(DateTime.Now - _testStartTime).TotalSeconds:F2}s";
                }

                // Collect evidence files
                CollectEvidence();

                Console.WriteLine($"[Xray] Test completed: {_currentTestResult.TestKey} - {_currentTestResult.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Xray] ?? Error in AfterScenario: {ex.Message}");
            }
        }

        [AfterTestRun]
        public static void PublishResultsToXray()
        {
            if (_xrayConfig?.Enabled != true || _xrayClient == null || _currentExecution == null)
            {
                return;
            }

            try
            {
                Console.WriteLine($"[Xray] Publishing {_testResults.Count} test results to Xray...");

                // Add all test results to execution
                _currentExecution.TestResults = _testResults.Values.ToList();

                // Create Test Execution if needed
                if (string.IsNullOrEmpty(_currentExecution.Key) && _xrayConfig.AutoCreateTestExecution)
                {
                    Console.WriteLine("[Xray] Creating new Test Execution...");
                    var testExecKey = _xrayClient.CreateTestExecutionAsync(
                        _currentExecution.Summary,
                        _currentExecution.Description,
                        _testResults.Keys.ToList()
                    ).GetAwaiter().GetResult();

                    if (!string.IsNullOrEmpty(testExecKey))
                    {
                        _currentExecution.Key = testExecKey;
                        Console.WriteLine($"[Xray] ? Created Test Execution: {testExecKey}");
                    }
                }

                // Import test results
                var importResponse = _xrayClient.ImportTestExecutionAsync(_currentExecution).GetAwaiter().GetResult();

                if (importResponse.Success)
                {
                    Console.WriteLine($"[Xray] ? Successfully published results to Xray");
                    Console.WriteLine($"[Xray] Test Execution: {importResponse.TestExecIssue}");
                    Console.WriteLine($"[Xray] View results: {_xrayConfig.BaseUrl}/browse/{importResponse.TestExecIssue}");

                    // Print summary
                    var passed = _testResults.Values.Count(t => t.Status == "PASS");
                    var failed = _testResults.Values.Count(t => t.Status == "FAIL");
                    var other = _testResults.Values.Count - passed - failed;

                    Console.WriteLine($"[Xray] Summary: {passed} passed, {failed} failed, {other} other");
                }
                else
                {
                    Console.WriteLine($"[Xray] ? Failed to publish results: {importResponse.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Xray] ? Error publishing results: {ex.Message}");
            }
        }

        private string? ExtractXrayTestKey()
        {
            // Look for Xray test key in tags: @TEST-123, @PROJ-456, etc.
            var tags = _scenarioContext.ScenarioInfo.Tags;

            // Pattern: @{PROJECT_KEY}-{NUMBER}
            var xrayTag = tags.FirstOrDefault(t => 
                t.Contains("-") && 
                t.Split('-').Length == 2 && 
                int.TryParse(t.Split('-')[1], out _));

            return xrayTag?.TrimStart('@');
        }

        private void CollectEvidence()
        {
            if (_currentTestResult == null) return;

            try
            {
                // Collect screenshots
                var screenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "screenshots");
                if (Directory.Exists(screenshotsDir))
                {
                    var screenshots = Directory.GetFiles(screenshotsDir, $"*{_scenarioContext.ScenarioInfo.Title.Replace(" ", "_")}*.png")
                        .OrderByDescending(File.GetCreationTime)
                        .Take(5); // Latest 5 screenshots

                    foreach (var screenshot in screenshots)
                    {
                        _currentTestResult.Evidences.Add(screenshot);
                        Console.WriteLine($"[Xray] Added evidence: {Path.GetFileName(screenshot)}");
                    }
                }

                // Collect videos
                var videosDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "videos");
                if (Directory.Exists(videosDir))
                {
                    var videos = Directory.GetFiles(videosDir, $"*.webm")
                        .OrderByDescending(File.GetCreationTime)
                        .Take(1); // Latest video

                    foreach (var video in videos)
                    {
                        _currentTestResult.Evidences.Add(video);
                        Console.WriteLine($"[Xray] Added video evidence: {Path.GetFileName(video)}");
                    }
                }

                // Collect traces
                var tracesDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "traces");
                if (Directory.Exists(tracesDir))
                {
                    var traces = Directory.GetFiles(tracesDir, $"*.zip")
                        .OrderByDescending(File.GetCreationTime)
                        .Take(1); // Latest trace

                    foreach (var trace in traces)
                    {
                        _currentTestResult.Evidences.Add(trace);
                        Console.WriteLine($"[Xray] Added trace evidence: {Path.GetFileName(trace)}");
                    }
                }

                // Collect logs
                var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "logs");
                if (Directory.Exists(logsDir))
                {
                    var logs = Directory.GetFiles(logsDir, $"*.log")
                        .OrderByDescending(File.GetCreationTime)
                        .Take(1); // Latest log

                    foreach (var log in logs)
                    {
                        _currentTestResult.Evidences.Add(log);
                        Console.WriteLine($"[Xray] Added log evidence: {Path.GetFileName(log)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Xray] ?? Error collecting evidence: {ex.Message}");
            }
        }
    }
}
