using AventStack.ExtentReports;
using AventStack.ExtentReports.Reporter;
using Microsoft.Playwright;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.IO;
using PlaywrightTests.Configuration;
using System.Text;

namespace PlaywrightTests.Utilities
{
    /// <summary>
    /// Enhanced ExtentReportHelper following UK Government Digital Service (GDS) standards
    /// and NHS Digital accessibility guidelines for test reporting
    /// </summary>
    public static class ExtentReportHelper
    {
        private static ExtentReports? _extent;
        private static ExtentTest? _currentTest;
        private static ExtentSparkReporter? _sparkReporter;
        private static string? _reportPath;
        private static string? _reportDirectory;

        private static readonly Dictionary<string, ExtentTest> _tests = new();
        private static readonly Dictionary<string, ExtentTest> _nodes = new();
        private static readonly Dictionary<string, ExtentTest> _nestedNodes = new();

        private static readonly TestConfiguration _config = TestConfiguration.Instance;

        public static void InitReport(string reportDir)
        {
            if (_extent != null) return;

            _reportDirectory = reportDir;
            Directory.CreateDirectory(reportDir);

            // Create subdirectories for artifacts (UK testing standards)
            Directory.CreateDirectory(Path.Combine(reportDir, "screenshots"));
            Directory.CreateDirectory(Path.Combine(reportDir, "videos"));
            Directory.CreateDirectory(Path.Combine(reportDir, "traces"));
            Directory.CreateDirectory(Path.Combine(reportDir, "logs"));
            Directory.CreateDirectory(Path.Combine(reportDir, "accessibility-reports"));
            Directory.CreateDirectory(Path.Combine(reportDir, "evidence"));

            _reportPath = Path.Combine(reportDir, "ExtentReport.html");
            _sparkReporter = new ExtentSparkReporter(_reportPath);

            ConfigureReportTheme();

            _extent = new ExtentReports();
            _extent.AttachReporter(_sparkReporter);

            AddSystemInformation();
        }

        private static void ConfigureReportTheme()
        {
            try
            {
                // UK Government Digital Service (GDS) compliant styling
                _sparkReporter!.Config.DocumentTitle = "Northumbria NHS - Test Execution Report";
                _sparkReporter.Config.ReportName = "Automated Test Results";
                _sparkReporter.Config.TimeStampFormat = "yyyy-MM-dd HH:mm:ss"; // UK date format
                _sparkReporter.Config.Encoding = "UTF-8";

                // Keep styling minimal and readable.
                _sparkReporter.Config.CSS = @"
                    .card-panel { border-left: 4px solid #005eb8; }
                    .test-name { font-weight: 600; }
                    body { font-family: Arial, Helvetica, sans-serif; }
                ";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExtentReport] Warning: Could not apply theme - {ex.Message}");
            }
        }

        private static void AddSystemInformation()
        {
            try
            {
                _extent!.AddSystemInfo("Organisation", "Northumbria Healthcare NHS Foundation Trust");
                _extent.AddSystemInfo("Environment", _config.EnvironmentName);
                _extent.AddSystemInfo("BaseUrl", _config.BaseUrl);
                _extent.AddSystemInfo("Browsers", string.Join(", ", _config.Browsers));
                _extent.AddSystemInfo("OS", RuntimeInformation.OSDescription);
                _extent.AddSystemInfo("MachineName", Environment.MachineName);
                _extent.AddSystemInfo("User", Environment.UserName);
                _extent.AddSystemInfo("DotNet", RuntimeInformation.FrameworkDescription);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExtentReport] Warning: Could not add system info - {ex.Message}");
            }
        }

        public static void FlushReport()
        {
            try
            {
                _extent?.Flush();

                if (!string.IsNullOrEmpty(_reportPath) && File.Exists(_reportPath))
                {
                    Console.WriteLine($"[Report] Extent HTML: {_reportPath}");
                    GenerateArtifactIndex();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExtentReport] Error flushing report: {ex.Message}");
            }
        }

        private static void GenerateArtifactIndex()
        {
            try
            {
                if (string.IsNullOrEmpty(_reportDirectory)) return;

                var indexPath = Path.Combine(_reportDirectory, "index.html");
                var sb = new StringBuilder();

                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html lang=\"en-GB\">");
                sb.AppendLine("<head>");
                sb.AppendLine("  <meta charset=\"UTF-8\">");
                sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                sb.AppendLine("  <title>Test Execution Artifacts</title>");
                sb.AppendLine("  <style>");
                sb.AppendLine("    body { font-family: Arial, sans-serif; margin: 20px; } ");
                sb.AppendLine("    .header { background: #005eb8; color: #fff; padding: 16px; border-radius: 6px; }");
                sb.AppendLine("    .section { margin-top: 18px; }");
                sb.AppendLine("    ul { padding-left: 18px; }");
                sb.AppendLine("    a { color: #005eb8; text-decoration: none; }");
                sb.AppendLine("    a:hover { text-decoration: underline; }");
                sb.AppendLine("  </style>");
                sb.AppendLine("</head>");
                sb.AppendLine("<body>");
                sb.AppendLine("  <div class=\"header\">");
                sb.AppendLine("    <h1>Northumbria NHS - Test Execution Artifacts</h1>");
                sb.AppendLine($"    <div>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>");
                sb.AppendLine("  </div>");

                sb.AppendLine("  <div class=\"section\">");
                sb.AppendLine("    <h2>Primary report</h2>");
                sb.AppendLine("    <ul>");
                sb.AppendLine("      <li><a href=\"ExtentReport.html\" target=\"_blank\">ExtentReport.html</a></li>");
                sb.AppendLine("    </ul>");
                sb.AppendLine("  </div>");

                // Add directory sections
                AddArtifactSection(sb, "screenshots", "Screenshots");
                AddArtifactSection(sb, "videos", "Videos");
                AddArtifactSection(sb, "traces", "Playwright traces");
                AddArtifactSection(sb, "logs", "Logs");
                AddArtifactSection(sb, "accessibility-reports", "Accessibility reports");
                AddArtifactSection(sb, "evidence", "Evidence");

                sb.AppendLine("</body>");
                sb.AppendLine("</html>");

                File.WriteAllText(indexPath, sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExtentReport] Warning: Could not generate artifact index - {ex.Message}");
            }
        }

        private static void AddArtifactSection(StringBuilder sb, string folderName, string title)
        {
            try
            {
                if (string.IsNullOrEmpty(_reportDirectory)) return;

                var folderPath = Path.Combine(_reportDirectory, folderName);
                if (!Directory.Exists(folderPath)) return;

                var files = Directory.GetFiles(folderPath);
                if (files.Length == 0) return;

                sb.AppendLine("  <div class=\"section\">");
                sb.AppendLine($"    <h2>{System.Net.WebUtility.HtmlEncode(title)}</h2>");
                sb.AppendLine("    <ul>");

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var relativePath = (folderName + "/" + fileName).Replace("\\", "/");
                    sb.AppendLine($"      <li><a href=\"{relativePath}\" target=\"_blank\">{System.Net.WebUtility.HtmlEncode(fileName)}</a></li>");
                }

                sb.AppendLine("    </ul>");
                sb.AppendLine("  </div>");
            }
            catch { }
        }

        public static void CreateTest(string testName)
        {
            if (_extent == null) return;
            if (string.IsNullOrWhiteSpace(testName)) testName = "Unnamed Test";

            if (_tests.TryGetValue(testName, out var existing))
            {
                _currentTest = existing;
                return;
            }

            var test = _extent.CreateTest(testName);
            _tests[testName] = test;
            _currentTest = test;
        }

        public static void CreateNode(string parentTestName, string nodeName)
        {
            if (_extent == null) return;
            if (string.IsNullOrWhiteSpace(parentTestName)) parentTestName = "Unnamed Test";
            if (string.IsNullOrWhiteSpace(nodeName)) nodeName = "node";

            if (!_tests.ContainsKey(parentTestName))
            {
                CreateTest(parentTestName);
            }

            var key = BuildNodeKey(parentTestName, nodeName);
            if (_nodes.TryGetValue(key, out var existing))
            {
                _currentTest = existing;
                return;
            }

            var parent = _tests[parentTestName];
            var node = parent.CreateNode(nodeName);
            _nodes[key] = node;
            _currentTest = node;
        }

        public static void CreateNestedNode(string parentTestName, string parentNodeName, string nodeName)
        {
            if (_extent == null) return;
            if (string.IsNullOrWhiteSpace(parentTestName)) parentTestName = "Unnamed Test";
            if (string.IsNullOrWhiteSpace(parentNodeName)) parentNodeName = "node";
            if (string.IsNullOrWhiteSpace(nodeName)) nodeName = "node";

            var parentKey = BuildNodeKey(parentTestName, parentNodeName);
            if (!_nodes.ContainsKey(parentKey))
            {
                CreateNode(parentTestName, parentNodeName);
            }

            var nestedKey = BuildNestedKey(parentTestName, parentNodeName, nodeName);
            if (_nestedNodes.TryGetValue(nestedKey, out var existing))
            {
                _currentTest = existing;
                return;
            }

            var parentNode = _nodes[parentKey];
            var childNode = parentNode.CreateNode(nodeName);
            _nestedNodes[nestedKey] = childNode;
            _currentTest = childNode;
        }

        public static void SetCurrentTest(string parentTestName, string? nodeName = null)
        {
            if (string.IsNullOrWhiteSpace(parentTestName)) parentTestName = "Unnamed Test";

            if (!string.IsNullOrWhiteSpace(nodeName))
            {
                var key = BuildNodeKey(parentTestName, nodeName);
                if (_nodes.TryGetValue(key, out var node))
                {
                    _currentTest = node;
                    return;
                }
            }

            if (_tests.TryGetValue(parentTestName, out var test))
            {
                _currentTest = test;
                return;
            }

            _currentTest = null;
        }

        public static void SetCurrentNestedTest(string parentTestName, string parentNodeName, string nodeName)
        {
            var nestedKey = BuildNestedKey(parentTestName, parentNodeName, nodeName);
            if (_nestedNodes.TryGetValue(nestedKey, out var node))
            {
                _currentTest = node;
                return;
            }

            SetCurrentTest(parentTestName, parentNodeName);
        }

        private static string BuildNodeKey(string parent, string node) => parent + "||" + node;
        private static string BuildNestedKey(string parent, string parentNode, string node) => parent + "||" + parentNode + "||" + node;

        // Logging helpers
        public static void LogInfo(string message) => _currentTest?.Info(message);
        public static void LogPass(string message) => _currentTest?.Pass(message);
        public static void LogWarning(string message) => _currentTest?.Warning(message);
        public static void LogError(string message) => _currentTest?.Fail(message);

        public static async Task AttachScreenshot(IPage page, string name)
        {
            if (_extent == null || page == null || string.IsNullOrEmpty(_reportDirectory)) return;

            try
            {
                var screenshotDir = Path.Combine(_reportDirectory, "screenshots");
                Directory.CreateDirectory(screenshotDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var fileName = $"{SafeFileName(name)}_{timestamp}.png";
                var filePath = Path.Combine(screenshotDir, fileName);

                var bytes = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = true,
                    Type = ScreenshotType.Png
                });
                await File.WriteAllBytesAsync(filePath, bytes);

                if (_currentTest != null)
                {
                    var relativePath = Path.Combine("screenshots", fileName).Replace("\\", "/");
                    _currentTest.AddScreenCaptureFromPath(relativePath, name);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExtentReport] Failed to attach screenshot: {ex.Message}");
            }
        }

        public static async Task AttachPageSource(IPage page, string name = "PageSource")
        {
            if (_extent == null || page == null || string.IsNullOrEmpty(_reportDirectory)) return;

            try
            {
                var evidenceDir = Path.Combine(_reportDirectory, "evidence");
                Directory.CreateDirectory(evidenceDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var fileName = $"{SafeFileName(name)}_{timestamp}.html";
                var filePath = Path.Combine(evidenceDir, fileName);

                var source = await page.ContentAsync();
                await File.WriteAllTextAsync(filePath, source);

                if (_currentTest != null)
                {
                    var relativePath = Path.Combine("evidence", fileName).Replace("\\", "/");
                    _currentTest.Info($"Page source: <a href=\"{relativePath}\" target=\"_blank\">{System.Net.WebUtility.HtmlEncode(fileName)}</a>");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExtentReport] Failed to attach page source: {ex.Message}");
            }
        }

        public static void AttachFile(string filePath, string displayName)
        {
            if (_extent == null || _currentTest == null || !File.Exists(filePath) || string.IsNullOrEmpty(_reportPath)) return;

            try
            {
                var reportDir = Path.GetDirectoryName(_reportPath) ?? AppDomain.CurrentDomain.BaseDirectory;
                var relativePath = Path.GetRelativePath(reportDir, filePath).Replace("\\", "/");
                _currentTest.Info($"Artifact: <a href=\"{relativePath}\" target=\"_blank\">{System.Net.WebUtility.HtmlEncode(displayName)}</a>");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExtentReport] Failed to attach file: {ex.Message}");
            }
        }

        public static void AttachAccessibilityReport(string jsonPath, string displayName)
        {
            if (_extent == null || _currentTest == null || !File.Exists(jsonPath) || string.IsNullOrEmpty(_reportDirectory)) return;

            try
            {
                // Do not copy the file (can cause file locks during parallel test execution).
                // Link the existing report file instead.
                AttachFile(jsonPath, displayName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExtentReport] Failed to attach accessibility report: {ex.Message}");
            }
        }

        private static string SafeFileName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "file";
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var c in invalid)
            {
                input = input.Replace(c, '_');
            }
            return input.Replace(" ", "_").Replace("(", "").Replace(")", "");
        }
    }
}
