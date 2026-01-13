using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using PlaywrightTests.Configuration;
using PlaywrightTests.Models;
using PlaywrightTests.PageObjects;
using PlaywrightTests.Utilities;
using Reqnroll;

namespace PlaywrightTests.Steps
{
    [Binding]
    public class SearchSuiteSteps
    {
        private const string DataSetCasesKey = "DDT_SearchCases";
        private const string DataSetResultsKey = "DDT_CaseResults";

        private readonly ScenarioContext _scenarioContext;
        private readonly PlaywrightTests.Context.SpecFlowContext _specFlowContext;
        private readonly TestConfiguration _config = TestConfiguration.Instance;

        public SearchSuiteSteps(ScenarioContext scenarioContext, PlaywrightTests.Context.SpecFlowContext specFlowContext)
        {
            _scenarioContext = scenarioContext;
            _specFlowContext = specFlowContext;
        }

        [Given("cross-browser mode is enabled for this run")]
        public void GivenCrossBrowserModeIsEnabledForThisRun()
        {
            var browserCount = _specFlowContext.Browsers?.Count ?? (_specFlowContext.Pages?.Count ?? 0);
            Assert.Greater(browserCount, 1,
                "Cross-browser scenario expects multiple browsers. Enable TestEnvironment:MultibrowserEnabled or set PLAYWRIGHT_MULTIBROWSER=1.");
        }

        [Then("a no results message should be displayed")]
        public async Task ThenANoResultsMessageShouldBeDisplayed()
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                var resultsPage = new SearchResultsPage(page);
                var hasMessage = await resultsPage.HasNoResultsMessageAsync().ConfigureAwait(false);
                Assert.IsTrue(hasMessage, $"Expected a no-results message on {label}.");
                _scenarioContext[$"NoResultsMessage_{label}"] = true;
            });
        }

        [When("I go to the \"(.*)\" page of results")]
        public async Task WhenIGoToThePageOfResults(string direction)
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                var resultsPage = new SearchResultsPage(page);
                var paginationResult = await resultsPage.NavigatePaginationAsync(direction).ConfigureAwait(false);
                Assert.IsTrue(paginationResult.Clicked, $"Could not locate a '{direction}' pagination control on {label}.");
                var changed = paginationResult.UrlChanged || paginationResult.SignatureChanged;
                Assert.IsTrue(changed, $"Pagination on {label} did not change the visible results.");
                _scenarioContext[$"PaginationChanged_{label}"] = changed;
            });
        }

        [Then("the search page indicator should update after pagination")]
        public void ThenTheSearchPageIndicatorShouldUpdateAfterPagination()
        {
            ExecutePerPage((page, label, idx) =>
            {
                var key = $"PaginationChanged_{label}";
                Assert.IsTrue(_scenarioContext.ContainsKey(key) && (bool)_scenarioContext[key],
                    $"Pagination verification state missing or false for {label}.");
            });
        }

        [When("I sort the results by \"(.*)\"")]
        public async Task WhenISortTheResultsBy(string optionLabel)
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                var resultsPage = new SearchResultsPage(page);
                var beforeOrder = (await resultsPage.GetResultTitlesAsync().ConfigureAwait(false)).ToList();
                var applied = await resultsPage.ApplySortOptionAsync(optionLabel).ConfigureAwait(false);
                Assert.IsTrue(applied, $"Sort control '{optionLabel}' not found on {label}.");
                var afterOrder = (await resultsPage.GetResultTitlesAsync().ConfigureAwait(false)).ToList();
                var changed = beforeOrder.Count == 0 ? afterOrder.Count > 0 : !beforeOrder.SequenceEqual(afterOrder);
                _scenarioContext[$"SortChanged_{label}"] = changed;
                _scenarioContext[$"SortBefore_{label}"] = beforeOrder;
                _scenarioContext[$"SortAfter_{label}"] = afterOrder;
            });
        }

        [Then("the search results order should change")]
        public void ThenTheSearchResultsOrderShouldChange()
        {
            ExecutePerPage((page, label, idx) =>
            {
                var key = $"SortChanged_{label}";
                Assert.IsTrue(_scenarioContext.ContainsKey(key) && (bool)_scenarioContext[key],
                    $"Sorting did not change the results order on {label}.");
            });
        }

        [Given("I load the search dataset \"(.*)\" from \"(.*)\"")]
        public void GivenILoadTheSearchDatasetFrom(string datasetKey, string relativePath)
        {
            var cases = LoadSearchCases(datasetKey, relativePath);
            Assert.IsNotEmpty(cases, $"Dataset '{datasetKey}' from '{relativePath}' returned no rows.");
            _scenarioContext[DataSetCasesKey] = cases;
            _scenarioContext[$"{DataSetCasesKey}_Source"] = relativePath;
        }

        [When("I execute the loaded data-driven search cases")]
        public async Task WhenIExecuteTheLoadedData_DrivenSearchCases()
        {
            Assert.IsTrue(_scenarioContext.ContainsKey(DataSetCasesKey), "Search dataset was not loaded.");
            var cases = (List<SearchDataCase>)_scenarioContext[DataSetCasesKey];
            var resultMap = new Dictionary<string, List<SearchCaseExecutionResult>>(StringComparer.OrdinalIgnoreCase);

            foreach (var dataCase in cases)
            {
                await ExecutePerPageAsync(async (page, label, idx) =>
                {
                    await page.GotoAsync(_config.BaseUrl, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.NetworkIdle,
                        Timeout = _config.Timeout
                    }).ConfigureAwait(false);

                    await SiteInteractionHelper.DismissCookieBannerIfPresentAsync(page, label).ConfigureAwait(false);

                    var home = new HomePage(page);
                    await home.SearchAsync(dataCase.Term).ConfigureAwait(false);

                    var resultsPage = new SearchResultsPage(page);
                    await resultsPage.WaitForResultsToStabilizeAsync().ConfigureAwait(false);
                    var hasResults = await resultsPage.HasResultsAsync().ConfigureAwait(false);

                    var executionResult = new SearchCaseExecutionResult
                    {
                        Term = dataCase.Term,
                        ExpectResults = dataCase.ExpectResults,
                        ExpectedMessage = dataCase.ExpectedMessage,
                        ActualResults = hasResults
                    };

                    if (dataCase.ExpectResults)
                    {
                        executionResult.Passed = hasResults;
                        if (!hasResults)
                        {
                            executionResult.Notes = "Expected results but none were detected.";
                        }
                    }
                    else
                    {
                        var messagePresent = await resultsPage.HasNoResultsMessageAsync(dataCase.ExpectedMessage).ConfigureAwait(false);
                        executionResult.MessageDisplayed = messagePresent;
                        executionResult.Passed = !hasResults && messagePresent;
                        if (hasResults)
                        {
                            executionResult.Notes = "Dataset expected zero results but items were returned.";
                        }
                        else if (!messagePresent)
                        {
                            executionResult.Notes = "Empty state message missing.";
                        }
                    }

                    var key = $"{DataSetResultsKey}_{label}";
                    if (!resultMap.TryGetValue(key, out var list))
                    {
                        list = new List<SearchCaseExecutionResult>();
                        resultMap[key] = list;
                    }
                    list.Add(executionResult);
                });
            }

            _scenarioContext[DataSetResultsKey] = resultMap;
        }

        [Then("each data-driven search should satisfy expectations")]
        public void ThenEachData_DrivenSearchShouldSatisfyExpectations()
        {
            Assert.IsTrue(_scenarioContext.ContainsKey(DataSetResultsKey), "Data-driven results were not captured.");
            var resultMap = (Dictionary<string, List<SearchCaseExecutionResult>>)_scenarioContext[DataSetResultsKey];
            var failures = new List<string>();

            foreach (var kvp in resultMap)
            {
                foreach (var result in kvp.Value)
                {
                    if (!result.Passed)
                    {
                        var note = string.IsNullOrWhiteSpace(result.Notes) ? string.Empty : $" Notes: {result.Notes}";
                        failures.Add($"[{kvp.Key}] Term='{result.Term}' expected {(result.ExpectResults ? "results" : "no results")}." + note);
                    }
                }
            }

            if (failures.Any())
            {
                Assert.Fail("Data-driven validations failed:\n" + string.Join("\n", failures));
            }
        }

        private List<SearchDataCase> LoadSearchCases(string datasetKey, string relativePath)
        {
            var resolvedPath = ResolveDataPath(relativePath);
            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException($"Dataset file '{relativePath}' could not be located.", resolvedPath);
            }

            var extension = Path.GetExtension(resolvedPath).ToLowerInvariant();
            return extension switch
            {
                ".json" => LoadFromJson(resolvedPath, datasetKey),
                ".csv" => LoadFromCsv(resolvedPath, datasetKey),
                _ => throw new NotSupportedException($"Unsupported dataset format: {extension}")
            };
        }

        private static List<SearchDataCase> LoadFromJson(string path, string datasetKey)
        {
            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var map = JsonSerializer.Deserialize<Dictionary<string, List<SearchDataCase>>>(json, options)
                      ?? new Dictionary<string, List<SearchDataCase>>(StringComparer.OrdinalIgnoreCase);

            if (!map.TryGetValue(datasetKey, out var cases) || cases == null || cases.Count == 0)
            {
                return new List<SearchDataCase>();
            }

            return cases;
        }

        private static List<SearchDataCase> LoadFromCsv(string path, string datasetKey)
        {
            var lines = File.ReadAllLines(path)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"))
                .ToList();

            if (lines.Count <= 1) return new List<SearchDataCase>();

            var header = SplitCsvLine(lines[0]);
            // Normalize header fields to prevent issues with BOM/whitespace
            header = header
                .Select(h => (h ?? string.Empty).Trim().Trim('\uFEFF'))
                .ToArray();

            var datasetIndex = Array.FindIndex(header, h => h.Equals("dataset", StringComparison.OrdinalIgnoreCase));
            var termIndex = Array.FindIndex(header, h => h.Equals("term", StringComparison.OrdinalIgnoreCase));
            var expectIndex = Array.FindIndex(header, h => h.Equals("expectResults", StringComparison.OrdinalIgnoreCase));
            var messageIndex = Array.FindIndex(header, h => h.Equals("expectedMessage", StringComparison.OrdinalIgnoreCase));

            var cases = new List<SearchDataCase>();
            foreach (var line in lines.Skip(1))
            {
                var fields = SplitCsvLine(line);

                // If the CSV includes a dataset column, filter by datasetKey.
                // If it doesn't, treat all rows as belonging to the requested dataset.
                if (datasetIndex >= 0)
                {
                    if (datasetIndex >= fields.Length) continue;
                    var datasetValue = (fields[datasetIndex] ?? string.Empty).Trim();
                    if (!datasetValue.Equals(datasetKey, StringComparison.OrdinalIgnoreCase)) continue;
                }

                var term = termIndex >= 0 && termIndex < fields.Length ? fields[termIndex] : string.Empty;
                var expectValue = expectIndex >= 0 && expectIndex < fields.Length ? fields[expectIndex] : "true";
                var expectedMessage = messageIndex >= 0 && messageIndex < fields.Length ? fields[messageIndex] : null;

                cases.Add(new SearchDataCase
                {
                    Term = (term ?? string.Empty).Trim(),
                    ExpectResults = bool.TryParse(expectValue, out var expect) ? expect : true,
                    ExpectedMessage = string.IsNullOrWhiteSpace(expectedMessage) ? null : expectedMessage
                });
            }

            return cases;
        }

        private static string[] SplitCsvLine(string line)
        {
            var values = new List<string>();
            var current = string.Empty;
            var inQuotes = false;

            foreach (var ch in line)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    values.Add(current);
                    current = string.Empty;
                }
                else
                {
                    current += ch;
                }
            }

            values.Add(current);
            return values.Select(v => v.Trim()).ToArray();
        }

        private static string ResolveDataPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return relativePath;

            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;

            var candidates = new List<string>();

            void AddCandidate(string rawPath)
            {
                try
                {
                    var fullPath = Path.GetFullPath(rawPath);
                    if (!candidates.Contains(fullPath))
                    {
                        candidates.Add(fullPath);
                    }
                }
                catch
                {
                    // Ignore invalid paths
                }
            }

            AddCandidate(Path.Combine(baseDir, normalized));
            AddCandidate(Path.Combine(baseDir, "..", "..", "..", normalized));
            AddCandidate(Path.Combine(Directory.GetCurrentDirectory(), normalized));

            var existingCandidates = candidates
                .Where(File.Exists)
                .Select(path => new { Path = path, Timestamp = File.GetLastWriteTimeUtc(path) })
                .OrderByDescending(x => x.Timestamp)
                .ToList();

            if (existingCandidates.Any())
            {
                return existingCandidates.First().Path;
            }

            return candidates.Last();
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
            var errors = new List<string>();
            if (!targets.Any()) Assert.Fail("No pages initialized for this scenario");
            foreach (var (page, label, idx) in targets)
            {
                try
                {
                    await ExecuteWithRecoveryAsync(page, label, idx, action).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    errors.Add($"[{label}] {ex.GetType().Name}: {ex.Message}");
                }
            }
            if (errors.Any()) Assert.Fail("One or more browsers failed:\n" + string.Join("\n", errors));
        }

        private void ExecutePerPage(Action<IPage, string, int> action)
        {
            var targets = GetTargets();
            var errors = new List<string>();
            if (!targets.Any()) Assert.Fail("No pages initialized for this scenario");
            foreach (var (page, label, idx) in targets)
            {
                try
                {
                    action(page, label, idx);
                }
                catch (Exception ex)
                {
                    errors.Add($"[{label}] {ex.GetType().Name}: {ex.Message}");
                }
            }
            if (errors.Any()) Assert.Fail("One or more browsers failed:\n" + string.Join("\n", errors));
        }

        private async Task ExecuteWithRecoveryAsync(IPage page, string label, int index, Func<IPage, string, int, Task> action)
        {
            try
            {
                await action(page, label, index).ConfigureAwait(false);
            }
            catch (PlaywrightException ex) when (IsTargetClosed(ex))
            {
                var recoveredPage = await TryRecreatePageAsync(index, label, ex).ConfigureAwait(false);
                if (recoveredPage == null)
                {
                    throw;
                }

                await action(recoveredPage, label, index).ConfigureAwait(false);
            }
            catch (PlaywrightException ex) when (IsTransientNavigationError(ex))
            {
                var recovered = await TryRecoverNavigationAsync(page, label, ex).ConfigureAwait(false);
                if (!recovered)
                {
                    throw;
                }

                await action(page, label, index).ConfigureAwait(false);
            }
        }

        private async Task<IPage?> TryRecreatePageAsync(int index, string label, Exception reason)
        {
            if (_specFlowContext.BrowserContexts == null || index >= _specFlowContext.BrowserContexts.Count)
            {
                Console.WriteLine($"[Recovery:{label}] Unable to recreate page for index {index}: browser context missing.");
                return null;
            }

            var context = _specFlowContext.BrowserContexts[index];
            if (context == null)
            {
                Console.WriteLine($"[Recovery:{label}] Browser context at index {index} is null.");
                return null;
            }

            try
            {
                var newPage = await context.NewPageAsync().ConfigureAwait(false);

                _specFlowContext.Pages ??= new List<IPage>();
                while (_specFlowContext.Pages.Count <= index)
                {
                    _specFlowContext.Pages.Add(newPage);
                }
                _specFlowContext.Pages[index] = newPage;

                if (index == 0)
                {
                    _specFlowContext.Page = newPage;
                }

                Console.WriteLine($"[Recovery:{label}] Target closed ({reason.Message}); new page created.");
                return newPage;
            }
            catch (Exception recreateEx)
            {
                Console.WriteLine($"[Recovery:{label}] Failed to recreate page: {recreateEx.Message}");
                return null;
            }
        }

        private async Task<bool> TryRecoverNavigationAsync(IPage page, string label, Exception reason)
        {
            try
            {
                Console.WriteLine($"[Recovery:{label}] Transient navigation failure detected ({reason.Message}). Retrying navigation...");
                await page.WaitForTimeoutAsync(750);
                await page.GotoAsync(_config.BaseUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = _config.Timeout
                }).ConfigureAwait(false);
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                {
                    Timeout = _config.Timeout
                }).ConfigureAwait(false);
                return true;
            }
            catch (Exception retryEx)
            {
                Console.WriteLine($"[Recovery:{label}] Navigation retry failed: {retryEx.Message}");
                return false;
            }
        }

        private static bool IsTargetClosed(PlaywrightException ex)
        {
            var message = ex.Message ?? string.Empty;
            return message.IndexOf("Target page, context or browser has been closed", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("browser has been closed", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("page has been closed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsTransientNavigationError(PlaywrightException ex)
        {
            var message = ex.Message ?? string.Empty;
            if (string.IsNullOrEmpty(message)) return false;

            return message.IndexOf("net::ERR_EMPTY_RESPONSE", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("net::ERR_CONNECTION_RESET", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("Navigation timeout", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("Protocol error", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        [Then("the search should not navigate away from the homepage")]
        public void ThenTheSearchShouldNotNavigateAwayFromTheHomepage()
        {
            ExecutePerPage((page, label, idx) =>
            {
                var isHomepage = page.Url.StartsWith(_config.BaseUrl, StringComparison.OrdinalIgnoreCase);
                Assert.IsTrue(isHomepage, $"Expected to remain on homepage ({label}). Actual: {page.Url}");
            });
        }
    }
}
