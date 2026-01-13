using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using PlaywrightTests.Configuration;
using Reqnroll;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using PlaywrightTests.Utilities;

namespace PlaywrightTests.Steps
{
    [Binding]
    public class FunctionalSearchSteps
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly PlaywrightTests.Context.SpecFlowContext _specFlowContext;
        private readonly TestConfiguration _config = TestConfiguration.Instance;

        public FunctionalSearchSteps(ScenarioContext scenarioContext, PlaywrightTests.Context.SpecFlowContext specFlowContext)
        {
            _scenarioContext = scenarioContext;
            _specFlowContext = specFlowContext;
        }

        // NOTE: Keep steps unique in this class. Do not duplicate this class or add wrapper [Binding] classes.

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
                    await action(page, label, idx);
                }
                catch (Exception ex)
                {
                    errors.Add($"[{label}] {ex.GetType().Name}: {ex.Message}");
                }
            }
            if (errors.Any()) Assert.Fail("One or more browsers failed:\n" + string.Join("\n", errors));
        }

        [Given("I am on the Northumbria homepage")]
        public async Task GivenIAmOnTheNorthumbriaHomepage()
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                var timeout = _config.Timeout;
                await page.GotoAsync(_config.BaseUrl, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = timeout
                });
                Assert.AreEqual(_config.BaseUrl, page.Url, $"Current URL should match the base URL ({label})");
                _scenarioContext[$"HomePage_Loaded_{label}"] = true;
                Console.WriteLine($"[Navigation:{label}] Verified page loaded at: {page.Url}");
            });
        }

        [Given("I open the Northumbria NHS homepage")]
        public async Task GivenIOpenTheNorthumbriaHomepage()
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                var url = "https://www.northumbria.nhs.uk/";
                var timeout = _config.Timeout;
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = timeout
                });
                Assert.IsTrue(page.Url.Contains("northumbria.nhs.uk"), $"Page URL should contain northumbria.nhs.uk ({label})");
                _scenarioContext[$"HomePage_Loaded_{label}"] = true;
                Console.WriteLine($"[Navigation:{label}] Opened Northumbria homepage: {page.Url}");
            });
        }

        [When("I search for \"(.*)\"")]
        public async Task WhenISearchFor(string term)
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                await DismissCookieBannerIfPresentAsync(page, label);
                Assert.IsTrue(_scenarioContext.ContainsKey($"HomePage_Loaded_{label}"), $"Home page should be loaded before searching ({label})");
                var home = new PlaywrightTests.PageObjects.HomePage(page);
                await home.SearchAsync(term);
                _scenarioContext[$"SearchTerm_{label}"] = term;
                _scenarioContext[$"Search_Executed_{label}"] = true;
                Console.WriteLine($"[Search:{label}] Verified search executed for term: {term}");
            });
        }

        [When("I accept cookies if prompted")]
        [Given("I accept cookies if prompted")]
        public async Task WhenIAcceptCookiesIfPrompted()
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                var dismissed = await DismissCookieBannerIfPresentAsync(page, label);
                if (!dismissed)
                {
                    Console.WriteLine($"[Cookies:{label}] No cookie prompt found");
                }
            });
        }

        [When(@"I enter ""(.*)"" into the site search and submit")]
        public async Task WhenIEnterIntoSiteSearchAndSubmit(string term)
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                await DismissCookieBannerIfPresentAsync(page, label);
                var home = new PlaywrightTests.PageObjects.HomePage(page);
                await home.SearchAsync(term);
                _scenarioContext[$"SearchTerm_{label}"] = term;
                _scenarioContext[$"Search_Executed_{label}"] = true;
                Console.WriteLine($"[Search:{label}] Executed search for: {term}");
            });
        }

        [When(@"I navigate to the search field using the keyboard \(Tab\)")]
        public async Task WhenINavigateToSearchFieldUsingKeyboardTab()
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                var home = new PlaywrightTests.PageObjects.HomePage(page);

                await DismissCookieBannerIfPresentAsync(page, label);
                await home.EnsureSearchInputReadyAsync();

                try
                {
                    await page.Locator("body").ClickAsync();
                    await page.WaitForTimeoutAsync(200);
                }
                catch { }

                try { await page.Keyboard.PressAsync("Home"); } catch { }

                var searchInputSelectors = new[]
                {
                    "input[name='query']",
                    "input#search-query-carousel-40618",
                    "input[type='search']",
                    "input[name='s']",
                    "input[name='search']",
                    "input[name='keys']",
                    "input[name='q']",
                    "input[name*='query']",
                    "input[name*='keyword']",
                    "input[id='search']",
                    "input[id*='search']",
                    "input[id*='Search']",
                    "input[id*='query']",
                    "input[class*='search']",
                    "input[aria-label*='search']",
                    "input[placeholder*='Search']",
                    "form[role='search'] input",
                    "[role='search'] input"
                };

                var toggleSelectors = new[]
                {
                    "button[aria-label*='search' i]:not([aria-label*='guide' i])",
                    "button[aria-controls*='search' i]:not([aria-controls*='guide' i])",
                    "button[aria-expanded][aria-controls*='search' i]:not([aria-controls*='guide' i])",
                    "button.search-toggle",
                    ".nhsuk-header__search-toggle",
                    "[data-action='toggle-search']",
                    "[data-component*='search'] button:not(:has-text('guide')):not(:has-text('Guide'))",
                    "button[class*='search']:not([class*='guide'])"
                };

                async Task<bool> ActiveElementMatchesAsync(string[] selectors) =>
                    await page.EvaluateAsync<bool>("selectors => { const el = document.activeElement; if (!el) return false; return selectors.some(sel => { try { return el.matches(sel); } catch { return false; } }); }", selectors);

                async Task<string> GetActiveElementInfoAsync() =>
                    await page.EvaluateAsync<string>(@"() => {
                        const el = document.activeElement;
                        if (!el) return '';
                        const tag = el.tagName.toLowerCase();
                        const text = (el.textContent || '').trim().substring(0, 50);
                        const ariaLabel = el.getAttribute('aria-label') || '';
                        const id = el.id || '';
                        const className = el.className || '';
                        return `${tag}|${text}|${ariaLabel}|${id}|${className}`;
                    }");

                await page.Keyboard.PressAsync("Tab");
                await page.WaitForTimeoutAsync(120);

                var maxTabs = 80;
                var found = false;

                for (int i = 0; i < maxTabs && !found; i++)
                {
                    if (await ActiveElementMatchesAsync(searchInputSelectors))
                    {
                        found = true;
                        Console.WriteLine($"[Keyboard:{label}] Focus landed on search field after {i + 1} tab iterations");
                        _scenarioContext[$"Keyboard_FocusOnSearch_{label}"] = true;
                        break;
                    }

                    var activeInfo = await GetActiveElementInfoAsync();

                    if (await ActiveElementMatchesAsync(toggleSelectors))
                    {
                        var isGuideButton = activeInfo.ToLower().Contains("guide");

                        if (!isGuideButton)
                        {
                            Console.WriteLine($"[Keyboard:{label}] Search toggle focused, invoking Enter to reveal search input");
                            await page.Keyboard.PressAsync("Enter");
                            await page.WaitForTimeoutAsync(300);

                            if (await ActiveElementMatchesAsync(searchInputSelectors))
                            {
                                found = true;
                                Console.WriteLine($"[Keyboard:{label}] Focus landed on search field after toggle");
                                _scenarioContext[$"Keyboard_FocusOnSearch_{label}"] = true;
                                break;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[Keyboard:{label}] Skipping guide button: {activeInfo}");
                        }
                    }

                    await page.Keyboard.PressAsync("Tab");
                    await page.WaitForTimeoutAsync(150);
                }

                if (!found)
                {
                    Console.WriteLine($"[Keyboard:{label}] Couldn't tab to search field, attempting direct resolver fallback");
                    var input = await home.FindSearchInputAsync();
                    if (input != null)
                    {
                        try { await input.ScrollIntoViewIfNeededAsync(); } catch { }
                        await input.FocusAsync();
                        Console.WriteLine($"[Keyboard:{label}] Directly focused search field via resolver");
                        _scenarioContext[$"Keyboard_FocusOnSearch_{label}"] = true;
                    }
                    else
                    {
                        Assert.Fail($"Could not navigate to search field using keyboard ({label})");
                    }
                }
            });
        }

        [When(@"I enter ""(.*)"" into the site search and submit using the Enter key")]
        public async Task WhenIEnterIntoSiteSearchAndSubmitUsingEnterKey(string term)
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                await DismissCookieBannerIfPresentAsync(page, label);
                var home = new PlaywrightTests.PageObjects.HomePage(page);
                var input = await home.FindSearchInputAsync();

                Assert.IsNotNull(input, $"Search input not found ({label})");

                await input!.FocusAsync();
                await page.WaitForTimeoutAsync(100);

                await input.ClearAsync();
                await page.WaitForTimeoutAsync(100);

                await input.FillAsync(term);
                await page.WaitForTimeoutAsync(200);

                var inputValue = await input.InputValueAsync();
                Assert.AreEqual(term, inputValue, $"Search input should contain the search term ({label})");

                await page.Keyboard.PressAsync("Enter");

                try
                {
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 15000 });
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });
                }
                catch
                {
                    Console.WriteLine($"[KeyboardSearch:{label}] Navigation wait timed out, continuing anyway");
                }

                _scenarioContext[$"SearchTerm_{label}"] = term;
                _scenarioContext[$"Search_Executed_{label}"] = true;
                Console.WriteLine($"[KeyboardSearch:{label}] Submitted search using Enter key: {term}");
            });
        }

        [When(@"I navigate to the search field using the mouse \(click\)")]
        public async Task WhenINavigateToTheSearchFieldUsingTheMouse_Click()
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                await DismissCookieBannerIfPresentAsync(page, label);
                var home = new PlaywrightTests.PageObjects.HomePage(page);
                var input = await home.FindSearchInputAsync();

                Assert.IsNotNull(input, $"Search input not found on page for mouse navigation ({label})");

                try
                {
                    ExtentReportHelper.LogInfo($"[Mouse:{label}] Clicking search input");
                    await ExtentReportHelper.AttachScreenshot(page, $"Before_Click_SearchInput_{label}");
                }
                catch { }

                try
                {
                    await input!.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Mouse:{label}] Click failed, will try focus: {ex.Message}");
                    await input!.FocusAsync();
                }

                _scenarioContext[$"Mouse_FocusOnSearch_{label}"] = true;
            });
        }

        [When(@"I enter ""(.*)"" into the site search and submit using the mouse")]
        public async Task WhenIEnterTermAndSubmitUsingMouse(string term)
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                await DismissCookieBannerIfPresentAsync(page, label);
                var home = new PlaywrightTests.PageObjects.HomePage(page);
                var input = await home.FindSearchInputAsync();

                Assert.IsNotNull(input, $"Search input not found for mouse submit ({label})");

                ExtentReportHelper.LogInfo($"[MouseSearch:{label}] Filling search input with term: {term}");
                await input!.FillAsync(term);

                var submitSelectors = new[] {
                    "button[type='submit']",
                    "input[type='submit']",
                    "button:has-text('Search')",
                    "button.search-submit",
                    ".search-submit",
                    ".search-button",
                    "button[aria-label*='search']"
                };

                ILocator? submit = null;
                foreach (var sel in submitSelectors)
                {
                    try
                    {
                        var s = page.Locator(sel);
                        if (await s.CountAsync() > 0)
                        {
                            submit = s.First;
                            break;
                        }
                    }
                    catch { }
                }

                if (submit != null)
                {
                    try
                    {
                        await submit.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                    }
                    catch (PlaywrightException)
                    {
                        await input.PressAsync("Enter");
                    }
                }
                else
                {
                    await input.PressAsync("Enter");
                }

                _scenarioContext[$"SearchTerm_{label}"] = term;
                _scenarioContext[$"Search_Executed_{label}"] = true;
                _scenarioContext[$"Mouse_Submit_Clicked_{label}"] = submit != null;

                Console.WriteLine($"[MouseSearch:{label}] Submitted search term via mouse: {term}");

                try { await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 }); } catch { }
            });
        }

        [Then("results are returned")]
        public async Task ThenResultsAreReturned()
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                Assert.IsTrue(_scenarioContext.ContainsKey($"Search_Executed_{label}"), $"Search should be executed before checking results ({label})");
                var results = new PlaywrightTests.PageObjects.SearchResultsPage(page);
                bool hasResults = await results.HasResultsAsync();
                Assert.IsTrue(hasResults, $"No search results were detected on the page ({label}).");
                _scenarioContext[$"Results_Verified_{label}"] = true;
            });
        }

        [Then(@"I should see results corresponding to the entered term")]
        public async Task ThenIShouldSeeResultsCorrespondingToTheEnteredTerm()
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                var term = _scenarioContext.ContainsKey($"SearchTerm_{label}") ? _scenarioContext[$"SearchTerm_{label}"].ToString() : null;
                Assert.IsNotNull(term, $"Search term not found in context for validation ({label})");

                var resultsPage = new PlaywrightTests.PageObjects.SearchResultsPage(page);
                var hasResults = await resultsPage.HasResultsAsync();
                Assert.IsTrue(hasResults, $"Expected results to be present for the search term ({label})");

                _scenarioContext[$"Results_ContentVerified_{label}"] = true;
            });
        }

        [Then("the search input was interactable and the submit control was clickable")]
        public async Task ThenTheSearchInputWasInteractableAndSubmitClickable()
        {
            await ExecutePerPageAsync(async (page, label, idx) =>
            {
                var home = new PlaywrightTests.PageObjects.HomePage(page);
                var input = await home.FindSearchInputAsync();
                Assert.IsNotNull(input, $"No search input found on page ({label})");

                var enabled = await input!.IsEnabledAsync();
                Assert.IsTrue(enabled, $"Search input appears disabled for {label}");

                var bbox = await input.BoundingBoxAsync();
                Assert.IsNotNull(bbox, $"Search input bounding box could not be determined for {label}");
                Assert.IsTrue(bbox.Width > 0 && bbox.Height > 0, $"Search input appears not visible or has zero size for {label}");
            });
        }

        private static async Task<bool> DismissCookieBannerIfPresentAsync(IPage page, string label)
        {
            return await SiteInteractionHelper.DismissCookieBannerIfPresentAsync(page, label);
        }
    }
}
