using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using PlaywrightTests.Configuration;
using PlaywrightTests.Utilities;
using Reqnroll;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PlaywrightTests.Hooks
{
    [Binding]
    public class PlaywrightHooks
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly PlaywrightTests.Context.SpecFlowContext _specFlowContext;
        private IPlaywright? _playwright;
        private readonly TestConfiguration _config = TestConfiguration.Instance;

        private static string? _runResultsDir;
        public PlaywrightHooks(ScenarioContext scenarioContext, PlaywrightTests.Context.SpecFlowContext specFlowContext)
        {
            _scenarioContext = scenarioContext;
            _specFlowContext = specFlowContext;

            if (_runResultsDir == null)
            {
                _runResultsDir = PlaywrightTests.Fixtures.TestSetup.RunResultsDir;
                if (string.IsNullOrEmpty(_runResultsDir))
                {
                    var dt = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    var testResultsRoot = ResolveProjectTestResultsRoot();
                    _runResultsDir = Path.Combine(testResultsRoot, $"Run-{dt}");
                    Directory.CreateDirectory(_runResultsDir);
                    Directory.CreateDirectory(Path.Combine(_runResultsDir, "traces"));
                    Directory.CreateDirectory(Path.Combine(_runResultsDir, "logs"));
                    Directory.CreateDirectory(Path.Combine(_runResultsDir, "screenshots"));
                    Directory.CreateDirectory(Path.Combine(_runResultsDir, "videos"));
                    Directory.CreateDirectory(Path.Combine(_runResultsDir, "evidence"));
                    Console.WriteLine($"[TestResults] All logs, traces, and reports will be saved to: {_runResultsDir}");
                    ExtentReportHelper.InitReport(_runResultsDir);
                }
                _specFlowContext.RunResultsDir = _runResultsDir;
            }
        }

        [BeforeScenario]
        public async Task BeforeScenarioAsync()
        {
            _scenarioContext["_scenarioStartTime"] = DateTime.Now;
            _scenarioContext["_stepResults"] = new List<StepResult>();

            var multibrowser = _config.MultibrowserEnabled;

            List<string> browsersToRun;
            if (multibrowser)
            {
                browsersToRun = _config.Browsers;
            }
            else
            {
                var browserEnv = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSER") ?? _config.Browser;
                browsersToRun = new List<string> { browserEnv };
            }

            bool headless = _config.Headless;

            var parentTestName = _scenarioContext.ScenarioInfo.Title;
            try
            {
                ExtentReportHelper.CreateTest(parentTestName);
                ExtentReportHelper.LogInfo($"Browsers: {string.Join(',', browsersToRun)}, Environment: {_config.EnvironmentName}");
            }
            catch { }

            _playwright = await Playwright.CreateAsync();

            var browsers = new List<IBrowser>();
            var contexts = new List<IBrowserContext>();
            var pages = new List<IPage>();

            for (int i = 0; i < browsersToRun.Count; i++)
            {
                var browserName = browsersToRun[i];
                IBrowserType browserType = browserName.Equals("firefox", StringComparison.OrdinalIgnoreCase)
                    ? _playwright.Firefox
                    : _playwright.Chromium;

                var launchArgs = new List<string> { "--enable-logging=stderr", "--v=1" };
                if (_config.UseFullWindow)
                {
                    launchArgs.AddRange(new[] { "--start-maximized", "--start-fullscreen", "--kiosk" });
                }

                var launchOptions = new BrowserTypeLaunchOptions
                {
                    Headless = headless,
                    SlowMo = _config.SlowMo,
                    Args = launchArgs.ToArray()
                };

                if (browserName.Equals("chrome", StringComparison.OrdinalIgnoreCase))
                    launchOptions.Channel = "chrome";
                else if (browserName.Equals("msedge", StringComparison.OrdinalIgnoreCase))
                    launchOptions.Channel = "msedge";

                var browser = await browserType.LaunchAsync(launchOptions);

                var contextOptions = new BrowserNewContextOptions
                {
                    RecordVideoDir = Path.Combine(_runResultsDir!, "videos"),
                    RecordVideoSize = new RecordVideoSize { Width = _config.ViewportWidth, Height = _config.ViewportHeight },
                    ViewportSize = _config.UseFullWindow ? null : new ViewportSize { Width = _config.ViewportWidth, Height = _config.ViewportHeight }
                };

                var context = await browser.NewContextAsync(contextOptions);
                var page = await context.NewPageAsync();

                // Extra safety: if browser still doesn't open full size (OS/window manager),
                // keep the viewport unconstrained (no explicit SetViewportSizeAsync call).

                await context.Tracing.StartAsync(new TracingStartOptions
                {
                    Screenshots = true,
                    Snapshots = true,
                    Sources = true
                });

                var browserLabel = GetLabelForBrowser(browserName, i);

                try
                {
                    ExtentReportHelper.CreateNode(parentTestName, browserLabel);
                    ExtentReportHelper.LogInfo($"Started browser node: {browserLabel} ({_config.ViewportWidth}x{_config.ViewportHeight}, FullWindow={_config.UseFullWindow})");
                }
                catch { }

                browsers.Add(browser);
                contexts.Add(context);
                pages.Add(page);
            }

            _specFlowContext.Playwright = _playwright;
            _specFlowContext.Playwrights = new List<IPlaywright> { _playwright };
            _specFlowContext.Browsers = browsers;
            _specFlowContext.BrowserContexts = contexts;
            _specFlowContext.Pages = pages;

            _specFlowContext.Browser = browsers.FirstOrDefault();
            _specFlowContext.BrowserContext = contexts.FirstOrDefault();
            _specFlowContext.Page = pages.FirstOrDefault();
        }

        [AfterScenario]
        public async Task AfterScenarioAsync()
        {
            var failed = _scenarioContext.TestError != null;
            string scenarioName = _scenarioContext.ScenarioInfo.Title.Replace(" ", "_");
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var scenarioStartTime = _scenarioContext.ContainsKey("_scenarioStartTime")
                ? (DateTime)_scenarioContext["_scenarioStartTime"]
                : DateTime.Now;
            var scenarioDuration = (DateTime.Now - scenarioStartTime).TotalSeconds;

            var pages = _specFlowContext.Pages;
            var contexts = _specFlowContext.BrowserContexts;

            var scenarioResult = new ScenarioResult
            {
                Name = _scenarioContext.ScenarioInfo.Title,
                Passed = !failed,
                Duration = scenarioDuration,
                Browser = _specFlowContext.Browsers?.FirstOrDefault()?.ToString() ?? "Unknown",
                ErrorMessage = failed ? _scenarioContext.TestError?.Message : null,
                Tags = _scenarioContext.ScenarioInfo.Tags?.ToList() ?? new List<string>(),
                Steps = new List<StepResult>()
            };

            if (_scenarioContext.ContainsKey("_stepResults"))
            {
                scenarioResult.Steps = (List<StepResult>)_scenarioContext["_stepResults"];
            }

            PlaywrightHooksAfterRun.RecordScenarioResult(scenarioResult);

            if (pages != null && contexts != null)
            {
                for (int i = 0; i < contexts.Count; i++)
                {
                    var context = contexts[i];
                    var page = pages.Count > i ? pages[i] : null;
                    var browserLabel = i < _specFlowContext.Browsers?.Count ? GetBrowserLabel(_specFlowContext.Browsers![i]) : i.ToString();

                    string tracePath = Path.Combine(_runResultsDir!, "traces", $"{scenarioName}_{browserLabel}_{timestamp}.zip");
                    string logPath = Path.Combine(_runResultsDir!, "logs", $"{scenarioName}_{browserLabel}_{timestamp}.log");
                    string screenshotPath = Path.Combine(_runResultsDir!, "screenshots", $"{scenarioName}_{browserLabel}_{timestamp}_final.png");
                    string pageSourcePath = Path.Combine(_runResultsDir!, "evidence", $"{scenarioName}_{browserLabel}_{timestamp}_source.html");

                    if (page != null)
                    {
                        try
                        {
                            ExtentReportHelper.SetCurrentTest(_scenarioContext.ScenarioInfo.Title, browserLabel);

                            await context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });

                            try
                            {
                                await page.ScreenshotAsync(new PageScreenshotOptions
                                {
                                    Path = screenshotPath,
                                    FullPage = true
                                });
                            }
                            catch { }

                            try
                            {
                                var content = await page.ContentAsync();
                                await File.WriteAllTextAsync(pageSourcePath, content);
                            }
                            catch { }

                            try
                            {
                                var video = page.Video;
                                if (video != null)
                                {
                                    var videoPath = await video.PathAsync();
                                    if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
                                    {
                                        var videoDestPath = Path.Combine(_runResultsDir!, "videos", $"{scenarioName}_{browserLabel}_{timestamp}.webm");
                                        File.Copy(videoPath, videoDestPath, true);
                                        ExtentReportHelper.AttachFile(videoDestPath, $"Video Recording ({browserLabel})");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Video:{browserLabel}] Could not attach video: {ex.Message}");
                            }

                            if (failed)
                            {
                                var error = _scenarioContext.TestError?.ToString() ?? "Unknown error";
                                var errorLog = BuildFailureLog(scenarioName, browserLabel, error);

                                ExtentReportHelper.LogError($"[{browserLabel}] Scenario Failed");
                                ExtentReportHelper.LogError($"Error Details: {error}");

                                await File.WriteAllTextAsync(logPath, errorLog);

                                if (File.Exists(tracePath)) ExtentReportHelper.AttachFile(tracePath, $"Playwright Trace ({browserLabel})");
                                if (File.Exists(logPath)) ExtentReportHelper.AttachFile(logPath, $"Error Log ({browserLabel})");
                                if (File.Exists(screenshotPath)) ExtentReportHelper.AttachFile(screenshotPath, $"Final Screenshot ({browserLabel})");
                                if (File.Exists(pageSourcePath)) ExtentReportHelper.AttachFile(pageSourcePath, $"Page Source ({browserLabel})");
                            }
                            else
                            {
                                var successLog = BuildSuccessLog(scenarioName, browserLabel);
                                await File.WriteAllTextAsync(logPath, successLog);

                                ExtentReportHelper.LogPass($"Scenario passed successfully on {browserLabel}");

                                if (File.Exists(tracePath)) ExtentReportHelper.AttachFile(tracePath, $"Execution Trace ({browserLabel})");
                                if (File.Exists(logPath)) ExtentReportHelper.AttachFile(logPath, $"Execution Log ({browserLabel})");
                                if (File.Exists(screenshotPath)) ExtentReportHelper.AttachFile(screenshotPath, $"Final State Screenshot ({browserLabel})");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[AfterScenario:{browserLabel}] Error capturing artifacts: {ex.Message}");
                        }

                        try { await context.CloseAsync(); } catch { }
                    }
                }
            }

            if (_specFlowContext.Browsers != null)
            {
                foreach (var b in _specFlowContext.Browsers)
                {
                    try { await b.CloseAsync(); } catch { }
                }
            }

            _specFlowContext.Page = null;
            _specFlowContext.BrowserContext = null;
            _specFlowContext.Browser = null;
            _specFlowContext.Browsers = null;
            _specFlowContext.BrowserContexts = null;
            _specFlowContext.Pages = null;
            _specFlowContext.Playwrights = null;
            _specFlowContext.Playwright = null;
        }

        private static string ResolveProjectTestResultsRoot()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var currentDir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 3 && currentDir.Parent != null; i++)
            {
                currentDir = currentDir.Parent;
            }

            var projectRoot = currentDir.FullName;
            var testResultsRoot = Path.Combine(projectRoot, "TestResults");
            Directory.CreateDirectory(testResultsRoot);
            return testResultsRoot;
        }

        private static string EnsureFallbackRunDirectory()
        {
            var fallbackRunDir = Path.Combine(ResolveProjectTestResultsRoot(), "Run-Fallback");
            Directory.CreateDirectory(fallbackRunDir);
            Directory.CreateDirectory(Path.Combine(fallbackRunDir, "logs"));
            Directory.CreateDirectory(Path.Combine(fallbackRunDir, "screenshots"));
            Directory.CreateDirectory(Path.Combine(fallbackRunDir, "evidence"));
            return fallbackRunDir;
        }

        private static string BuildFailureLog(string scenarioName, string browserLabel, string error)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=" + new string('=', 78));
            sb.AppendLine("TEST FAILURE LOG");
            sb.AppendLine("=" + new string('=', 78));
            sb.AppendLine($"Organisation: Northumbria Healthcare NHS Foundation Trust");
            sb.AppendLine($"Test Framework: Playwright + Reqnroll + .NET 8");
            sb.AppendLine($"Scenario: {scenarioName}");
            sb.AppendLine($"Browser: {browserLabel}");
            sb.AppendLine($"Status: FAILED");
            sb.AppendLine($"Timestamp: {DateTime.Now:dd/MM/yyyy HH:mm:ss} (UK Time)");
            sb.AppendLine($"Test Environment: {TestConfiguration.Instance.EnvironmentName}");
            sb.AppendLine($"Base URL: {TestConfiguration.Instance.BaseUrl}");
            sb.AppendLine("=" + new string('=', 78));
            sb.AppendLine();
            sb.AppendLine("ERROR DETAILS:");
            sb.AppendLine(new string('-', 78));
            sb.AppendLine(error);
            sb.AppendLine(new string('-', 78));
            sb.AppendLine();
            return sb.ToString();
        }

        private static string BuildSuccessLog(string scenarioName, string browserLabel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=" + new string('=', 78));
            sb.AppendLine("TEST SUCCESS LOG");
            sb.AppendLine("=" + new string('=', 78));
            sb.AppendLine($"Organisation: Northumbria Healthcare NHS Foundation Trust");
            sb.AppendLine($"Test Framework: Playwright + Reqnroll + .NET 8");
            sb.AppendLine($"Scenario: {scenarioName}");
            sb.AppendLine($"Browser: {browserLabel}");
            sb.AppendLine($"Status: PASSED");
            sb.AppendLine($"Timestamp: {DateTime.Now:dd/MM/yyyy HH:mm:ss} (UK Time)");
            sb.AppendLine($"Test Environment: {TestConfiguration.Instance.EnvironmentName}");
            sb.AppendLine($"Base URL: {TestConfiguration.Instance.BaseUrl}");
            sb.AppendLine("=" + new string('=', 78));
            sb.AppendLine();
            return sb.ToString();
        }

        [BeforeStep]
        public void BeforeStep()
        {
            try
            {
                _scenarioContext["_currentStepStart"] = DateTime.UtcNow;

                var stepInfo = _scenarioContext.StepContext?.StepInfo;
                if (stepInfo != null)
                {
                    var stepText = $"{stepInfo.StepDefinitionType} {stepInfo.Text}";
                    _scenarioContext["_currentStepText"] = stepText;

                    // Create a dedicated step node per browser (step-wise reporting).
                    var parentName = _scenarioContext.ScenarioInfo.Title;
                    var pages = _specFlowContext.Pages;
                    if (pages != null && pages.Count > 0)
                    {
                        for (int i = 0; i < pages.Count; i++)
                        {
                            var browserLabel = "browser" + i;
                            if (_specFlowContext.Browsers != null && i < _specFlowContext.Browsers.Count)
                            {
                                try { browserLabel = _specFlowContext.Browsers[i]?.ToString()?.Replace(' ', '_') ?? browserLabel; } catch { }
                            }

                            try
                            {
                                ExtentReportHelper.CreateNestedNode(parentName, browserLabel, stepText);
                                ExtentReportHelper.SetCurrentNestedTest(parentName, browserLabel, stepText);
                                ExtentReportHelper.LogInfo(stepText);
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        // Fallback: single browser
                        try
                        {
                            ExtentReportHelper.CreateNode(parentName, stepText);
                            ExtentReportHelper.SetCurrentTest(parentName, stepText);
                            ExtentReportHelper.LogInfo(stepText);
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        [AfterStep]
        public async Task AfterStepAsync()
        {
            try
            {
                DateTime? start = null;
                if (_scenarioContext.ContainsKey("_currentStepStart"))
                {
                    start = _scenarioContext["_currentStepStart"] as DateTime?;
                }
                var durationMs = start.HasValue ? (DateTime.UtcNow - start.Value).TotalMilliseconds : 0;

                var rawStepText = _scenarioContext.ContainsKey("_currentStepText")
                    ? _scenarioContext["_currentStepText"]?.ToString() ?? "Unknown Step"
                    : "Unknown Step";

                var stepResult = new StepResult
                {
                    StepText = rawStepText,
                    Passed = _scenarioContext.TestError == null,
                    Duration = durationMs / 1000.0,
                    ErrorMessage = _scenarioContext.TestError?.Message
                };

                if (_scenarioContext.ContainsKey("_stepResults"))
                {
                    var stepResults = (List<StepResult>)_scenarioContext["_stepResults"];
                    stepResults.Add(stepResult);
                }

                var parentName = _scenarioContext.ScenarioInfo.Title;
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

                var pages = _specFlowContext.Pages;
                if (pages != null && pages.Count > 0)
                {
                    for (int i = 0; i < pages.Count; i++)
                    {
                        var page = pages[i];
                        var browserLabel = "browser" + i;
                        if (_specFlowContext.Browsers != null && i < _specFlowContext.Browsers.Count)
                        {
                            try { browserLabel = _specFlowContext.Browsers[i]?.ToString()?.Replace(' ', '_') ?? browserLabel; } catch { }
                        }

                        // Switch report context to this step node
                        try
                        {
                            ExtentReportHelper.SetCurrentNestedTest(parentName, browserLabel, rawStepText);
                        }
                        catch { }

                        if (_scenarioContext.TestError != null)
                        {
                            ExtentReportHelper.LogError($"FAILED (Duration: {durationMs:F0}ms)\n{_scenarioContext.TestError.Message}");
                        }
                        else
                        {
                            ExtentReportHelper.LogPass($"PASSED (Duration: {durationMs:F0}ms)");
                        }

                        var runDir = _runResultsDir ?? _specFlowContext.RunResultsDir ?? EnsureFallbackRunDirectory();
                        Directory.CreateDirectory(Path.Combine(runDir, "logs"));

                        var logPath = Path.Combine(runDir, "logs", $"{SafeFileName(parentName)}_{browserLabel}_{SafeFileName(rawStepText)}_{timestamp}.log");

                        if (_scenarioContext.TestError != null)
                        {
                            var errorMessage = _scenarioContext.TestError.ToString();
                            try
                            {
                                await File.WriteAllTextAsync(logPath, $"{rawStepText}\nStatus: FAILED\nDurationMs: {durationMs:F0}\n\n{errorMessage}");
                                ExtentReportHelper.AttachFile(logPath, $"Step log ({browserLabel})");
                            }
                            catch { }

                            try { await ExtentReportHelper.AttachScreenshot(page, $"{SafeFileName(rawStepText)}_{browserLabel}_failure"); } catch { }
                            try { await ExtentReportHelper.AttachPageSource(page, $"{SafeFileName(rawStepText)}_{browserLabel}_source"); } catch { }
                        }
                        else
                        {
                            try
                            {
                                await File.WriteAllTextAsync(logPath, $"{rawStepText}\nStatus: PASSED\nDurationMs: {durationMs:F0}");
                                ExtentReportHelper.AttachFile(logPath, $"Step log ({browserLabel})");
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    // Single browser fallback
                    try { ExtentReportHelper.SetCurrentTest(parentName, rawStepText); } catch { }
                    if (_scenarioContext.TestError != null)
                        ExtentReportHelper.LogError($"FAILED (Duration: {durationMs:F0}ms)\n{_scenarioContext.TestError.Message}");
                    else
                        ExtentReportHelper.LogPass($"PASSED (Duration: {durationMs:F0}ms)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AfterStep] Error capturing step artifacts: {ex.Message}");
            }
        }

        private static string GetBrowserLabel(IBrowser browser)
        {
            try
            {
                var s = browser.ToString();
                if (!string.IsNullOrEmpty(s)) return s.Replace(" ", "_");
            }
            catch { }
            return "browser";
        }

        private static string GetLabelForBrowser(string browserName, int index)
        {
            if (string.IsNullOrEmpty(browserName)) return $"browser{index}";
            return browserName.Replace(' ', '_');
        }

        private static string SafeFileName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "unnamed";
            try
            {
                var invalids = Path.GetInvalidFileNameChars();
                var s = string.Concat(input.Split(invalids));
                s = s.Replace('\n', ' ').Replace('\r', ' ').Trim();
                s = System.Text.RegularExpressions.Regex.Replace(s, "\\s+", "_");
                if (s.Length > 150) s = s.Substring(0, 150);
                return s;
            }
            catch
            {
                return "unnamed";
            }
        }
    }

    [Binding]
    public static class PlaywrightHooksAfterRun
    {
        private static List<ScenarioResult> _scenarioResults = new List<ScenarioResult>();
        private static DateTime _testRunStartTime;

        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            _testRunStartTime = DateTime.Now;
            _scenarioResults = new List<ScenarioResult>();
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            try
            {
                ExtentReportHelper.FlushReport();

                var testRunEndTime = DateTime.Now;
                var duration = (testRunEndTime - _testRunStartTime).TotalSeconds;

                var totalTests = _scenarioResults.Count;
                var passedTests = _scenarioResults.Count(s => s.Passed);
                var failedTests = _scenarioResults.Count(s => !s.Passed);
                var skippedTests = 0;

                var passRate = totalTests > 0 ? (passedTests * 100.0 / totalTests) : 0;
                var status = failedTests == 0 ? "SUCCESS" : "FAILURE";

                var config = TestConfiguration.Instance;

                var artifactsPath = PlaywrightTests.Fixtures.TestSetup.RunResultsDir;
                var reportPath = Path.Combine(artifactsPath ?? "", "ExtentReport.html");

                var testRunResults = new TestRunResults
                {
                    RunId = Path.GetFileName(artifactsPath ?? DateTime.Now.ToString("yyyyMMdd-HHmmss")),
                    ExecutionDate = _testRunStartTime.Date,
                    ExecutionTime = _testRunStartTime,
                    Environment = config.EnvironmentName.ToUpper(),
                    Browser = string.Join(", ", config.Browsers),
                    TotalTests = totalTests,
                    PassedTests = passedTests,
                    FailedTests = failedTests,
                    SkippedTests = skippedTests,
                    Duration = duration,
                    PassRate = passRate,
                    Status = status,
                    ArtifactsPath = artifactsPath ?? "",
                    ReportPath = reportPath,
                    Scenarios = _scenarioResults,
                    SystemInfo = new Dictionary<string, string>
                    {
                        { "OS", RuntimeInformation.OSDescription },
                        { "MachineName", Environment.MachineName },
                        { "User", Environment.UserName },
                        { "DotNetVersion", Environment.Version.ToString() },
                        { "BaseUrl", config.BaseUrl },
                        { "Headless", config.Headless.ToString() }
                    }
                };

                TestResultsStorage.SaveTestRunResults(testRunResults);
                TestResultsStorage.GenerateTrendReport(30);
                DisplayTestRunSummary(testRunResults);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AfterTestRun] Error saving test results: {ex.Message}");
            }
        }

        public static void RecordScenarioResult(ScenarioResult scenarioResult)
        {
            _scenarioResults.Add(scenarioResult);
        }

        private static void DisplayTestRunSummary(TestRunResults results)
        {
            Console.WriteLine();
            Console.WriteLine("=" + new string('=', 78));
            Console.WriteLine("TEST RUN SUMMARY");
            Console.WriteLine("=" + new string('=', 78));
            Console.WriteLine($"Date: {results.ExecutionDate:dd/MM/yyyy}");
            Console.WriteLine($"Time: {results.ExecutionTime:HH:mm:ss} (UK Time)");
            Console.WriteLine($"Duration: {results.Duration:F2} seconds");
            Console.WriteLine($"Environment: {results.Environment}");
            Console.WriteLine($"Browser: {results.Browser}");
            Console.WriteLine("=" + new string('=', 78));
            Console.WriteLine($"Total Tests: {results.TotalTests}");
            Console.WriteLine($"Passed: {results.PassedTests}");
            Console.WriteLine($"Failed: {results.FailedTests}");
            Console.WriteLine($"Pass Rate: {results.PassRate:F2}%");
            Console.WriteLine($"Status: {results.Status}");
            Console.WriteLine("=" + new string('=', 78));
            Console.WriteLine($"Artifacts: {results.ArtifactsPath}");
            Console.WriteLine($"Report: {results.ReportPath}");
            Console.WriteLine("=" + new string('=', 78));
            Console.WriteLine();
        }
    }
}
