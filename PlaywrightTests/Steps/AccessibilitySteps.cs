using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using PlaywrightTests.Configuration;
using PlaywrightTests.Utilities;
using Reqnroll;

namespace PlaywrightTests.Steps
{
    [Binding]
    public class AccessibilitySteps
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly PlaywrightTests.Context.SpecFlowContext _specFlowContext;
        private readonly TestConfiguration _config = TestConfiguration.Instance;

        public AccessibilitySteps(ScenarioContext scenarioContext, PlaywrightTests.Context.SpecFlowContext specFlowContext)
        {
            _scenarioContext = scenarioContext;
            _specFlowContext = specFlowContext;
        }

        [Then("the page should meet screen reader accessibility requirements")]
        public async Task ThenThePageShouldMeetScreenReaderAccessibilityRequirements()
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                var results = await AxeHelper.RunAxeAsync(page).ConfigureAwait(false);
                Assert.IsNotNull(results, $"Axe results were null ({label})");

                var runDir = _specFlowContext.RunResultsDir ?? AppDomain.CurrentDomain.BaseDirectory;
                var a11yDir = Path.Combine(runDir, "accessibility-reports");
                Directory.CreateDirectory(a11yDir);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var scenarioSafe = SafeFileName(_scenarioContext.ScenarioInfo.Title);
                var fileName = $"{scenarioSafe}_{label}_axe_report_{timestamp}.json";
                var filePath = Path.Combine(a11yDir, fileName);

                var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);

                // Avoid file lock issues by attaching as a simple link (Extent will read later) 
                // and by ensuring the file write has completed.
                ExtentReportHelper.AttachFile(filePath, $"Axe Results ({label})");

                if (results!.ViolationsCount > 0)
                {
                    ExtentReportHelper.LogWarning($"Accessibility violations detected: {results.ViolationsCount} ({label}). Report: {fileName}");
                    Assert.Fail($"Accessibility violations detected: {results.ViolationsCount} ({label}). See report: {fileName}");
                }
                else
                {
                    ExtentReportHelper.LogPass($"No axe violations detected ({label})");
                }
            });
        }

        private List<(IPage page, string label, int index)> GetTargets()
        {
            var targets = new List<(IPage, string, int)>();
            if (_specFlowContext.Pages != null && _specFlowContext.Pages.Count > 0)
            {
                for (int i = 0; i < _specFlowContext.Pages.Count; i++)
                {
                    var p = _specFlowContext.Pages[i];
                    var label = "browser" + i;
                    if (_specFlowContext.Browsers != null && i < _specFlowContext.Browsers.Count)
                    {
                        try { label = _specFlowContext.Browsers[i]?.ToString()?.Replace(' ', '_') ?? label; } catch { }
                    }
                    targets.Add((p, label, i));
                }
            }
            else if (_specFlowContext.Page != null)
            {
                targets.Add((_specFlowContext.Page, "browser0", 0));
            }
            return targets;
        }

        private async Task ExecutePerPageAsync(Func<IPage, string, int, Task> action)
        {
            var targets = GetTargets();
            if (targets.Count == 0) Assert.Fail("No pages initialized for this scenario");

            // Execute sequentially and fail fast on the first failing browser.
            foreach (var (page, label, idx) in targets)
            {
                await action(page, label, idx).ConfigureAwait(false);
            }
        }

        private static string SafeFileName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "unnamed";
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                input = input.Replace(c, '_');
            }
            return input.Replace(' ', '_');
        }
    }
}
