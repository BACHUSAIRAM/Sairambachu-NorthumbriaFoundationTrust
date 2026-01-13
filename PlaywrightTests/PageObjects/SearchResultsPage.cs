using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PlaywrightTests.PageObjects
{
    public class SearchResultsPage : BasePage
    {
        public SearchResultsPage(IPage page) : base(page)
        {
        }

        public async Task<bool> HasResultsAsync()
        {
            // Wait for page to be stable before checking content
            try
            {
                await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new PageWaitForLoadStateOptions { Timeout = 5000 });
            }
            catch
            {
                // Continue even if wait times out
            }

            // Wait for any of the result indicators to be present
            try
            {
                await Page.WaitForSelectorAsync("main, .search-results, #search-results, [class*='result']", new PageWaitForSelectorOptions { Timeout = 5000 });
            }
            catch
            {
                // Continue even if wait times out
            }

            // Now safely get content
            string content;
            try
            {
                content = await GetContentAsync();
            }
            catch (PlaywrightException)
            {
                // If still navigating, wait a bit more and retry
                await Page.WaitForTimeoutAsync(1000);
                try
                {
                    content = await GetContentAsync();
                }
                catch
                {
                    // As last resort, check URL to see if we're on results page
                    return Page.Url.Contains("search") || Page.Url.Contains("?");
                }
            }

            bool looksLikeResults = content.IndexOf("Search results", StringComparison.OrdinalIgnoreCase) >= 0
                || content.IndexOf("results", StringComparison.OrdinalIgnoreCase) >= 0
                || (await Locator("main a").CountAsync()) > 0
                || (await Locator(".search-result, [class*='result']").CountAsync()) > 0;

            return looksLikeResults;
        }

        public async Task<bool> HasNoResultsMessageAsync(string? expectedText = null)
        {
            var selectors = new[]
            {
                ".no-results",
                "#no-results",
                ".nhsuk-search__no-results",
                ".nhsuk-search__results--none",
                "text=/No results/i",
                "text=/did not match any documents/i"
            };

            foreach (var selector in selectors)
            {
                try
                {
                    var locator = Page.Locator(selector);
                    if (await locator.CountAsync().ConfigureAwait(false) == 0) continue;

                    var element = locator.First;
                    if (!await element.IsVisibleAsync().ConfigureAwait(false)) continue;

                    var text = (await element.InnerTextAsync().ConfigureAwait(false))?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(expectedText) || text.IndexOf(expectedText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore selector-specific errors and continue
                }
            }

            return false;
        }

        public async Task<string> GetFirstResultSignatureAsync()
        {
            var resultSelectors = new[]
            {
                ".result-item",
                ".search-result",
                "article",
                "li.search-result",
                ".result-card"
            };

            foreach (var selector in resultSelectors)
            {
                var locator = Page.Locator(selector).First;
                try
                {
                    await locator.WaitForAsync(new LocatorWaitForOptions
                    {
                        Timeout = 2000,
                        State = WaitForSelectorState.Visible
                    }).ConfigureAwait(false);

                    var title = await locator.Locator("h1, h2, h3, a").First.InnerTextAsync().ConfigureAwait(false);
                    var snippet = await locator.InnerTextAsync().ConfigureAwait(false);
                    return $"{title}|{snippet}".Trim();
                }
                catch
                {
                    // Try next selector
                }
            }

            return string.Empty;
        }

        public async Task<IReadOnlyList<string>> GetResultTitlesAsync(int maxItems = 10)
        {
            var titleSelectors = new[]
            {
                ".result-item h2",
                ".result-item a",
                ".search-result h2",
                "article h2",
                "article a",
                "#page-results .result-item h2"
            };

            foreach (var selector in titleSelectors)
            {
                var locator = Page.Locator(selector);
                if (await locator.CountAsync().ConfigureAwait(false) == 0) continue;

                var total = Math.Min(await locator.CountAsync().ConfigureAwait(false), maxItems);
                var list = new List<string>();
                for (var i = 0; i < total; i++)
                {
                    try
                    {
                        var text = await locator.Nth(i).InnerTextAsync().ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            list.Add(text.Trim());
                        }
                    }
                    catch
                    {
                        // Ignore individual entries
                    }
                }

                if (list.Any()) return list;
            }

            return Array.Empty<string>();
        }

        public async Task<PaginationResult> NavigatePaginationAsync(string direction)
        {
            var normalized = (direction ?? string.Empty).Trim().ToLowerInvariant();
            var selectors = normalized switch
            {
                "next" => new[]
                {
                    "a[rel='next']",
                    "button[rel='next']",
                    "a:has-text('Next')",
                    "button:has-text('Next')",
                    "[aria-label*='next']"
                },
                "previous" or "prev" => new[]
                {
                    "a[rel='prev']",
                    "button[rel='prev']",
                    "a:has-text('Previous')",
                    "button:has-text('Previous')",
                    "[aria-label*='previous']"
                },
                _ => Array.Empty<string>()
            };

            var result = new PaginationResult
            {
                BeforeSignature = await GetFirstResultSignatureAsync().ConfigureAwait(false)
            };
            var beforeUrl = Page.Url;

            foreach (var selector in selectors)
            {
                try
                {
                    var locator = Page.Locator(selector).First;
                    if (await locator.CountAsync().ConfigureAwait(false) == 0) continue;
                    if (!await locator.IsEnabledAsync().ConfigureAwait(false)) continue;

                    await ScrollElementIntoViewAsync(locator).ConfigureAwait(false);
                    await locator.ClickAsync(new LocatorClickOptions { Timeout = 4000 }).ConfigureAwait(false);
                    await WaitForResultsToStabilizeAsync().ConfigureAwait(false);

                    result.Clicked = true;
                    result.AfterSignature = await GetFirstResultSignatureAsync().ConfigureAwait(false);
                    result.UrlChanged = !string.Equals(beforeUrl, Page.Url, StringComparison.OrdinalIgnoreCase);
                    result.SignatureChanged = !string.Equals(result.BeforeSignature, result.AfterSignature, StringComparison.OrdinalIgnoreCase);
                    return result;
                }
                catch
                {
                    // Try next selector
                }
            }

            return result;
        }

        public async Task WaitForResultsToStabilizeAsync()
        {
            try
            {
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 7000 });
            }
            catch
            {
                // Non-blocking
            }

            try
            {
                await Page.WaitForSelectorAsync(".search-result, .result-item, article", new PageWaitForSelectorOptions { Timeout = 5000 });
            }
            catch
            {
                // Ignore
            }
        }

        public async Task<bool> ApplySortOptionAsync(string optionLabel)
        {
            if (string.IsNullOrWhiteSpace(optionLabel)) return false;

            var normalized = optionLabel.Trim();
            var dropdownSelectors = new[]
            {
                "select[name*='sort']",
                "select[id*='sort']",
                ".search-sort select",
                "form select[data-sort]",
                "select[data-drupal-selector*='sort']"
            };

            foreach (var selector in dropdownSelectors)
            {
                var dropdown = Page.Locator(selector);
                if (await dropdown.CountAsync().ConfigureAwait(false) == 0) continue;

                try
                {
                    var select = dropdown.First;
                    var selected = await TrySelectDropdownOptionAsync(select, normalized).ConfigureAwait(false);
                    if (selected)
                    {
                        await WaitForResultsToStabilizeAsync().ConfigureAwait(false);
                        return true;
                    }
                }
                catch
                {
                    // Try the next selector type
                }
            }

            var clickableSelectors = new[]
            {
                $"button:has-text('{normalized}')",
                $"a:has-text('{normalized}')",
                $"[role='menuitem']:has-text('{normalized}')",
                $"label:has-text('{normalized}')",
                $"button[data-sort*='{normalized.ToLowerInvariant()}']",
                $"[data-sort-option*='{normalized.ToLowerInvariant()}']"
            };

            foreach (var selector in clickableSelectors)
            {
                var locator = Page.Locator(selector);
                if (await locator.CountAsync().ConfigureAwait(false) == 0) continue;

                try
                {
                    await ScrollElementIntoViewAsync(locator.First).ConfigureAwait(false);
                    await locator.First.ClickAsync(new LocatorClickOptions { Timeout = 3000 }).ConfigureAwait(false);
                    await WaitForResultsToStabilizeAsync().ConfigureAwait(false);
                    return true;
                }
                catch
                {
                    // Continue to next selector
                }
            }

            return false;
        }

        private static async Task<bool> TrySelectDropdownOptionAsync(ILocator dropdown, string optionLabel)
        {
            // First try matching by visible label directly via Playwright API (case-sensitive, so attempt both).
            var labelValues = new[] { optionLabel, optionLabel.ToLowerInvariant(), optionLabel.ToUpperInvariant() };
            foreach (var label in labelValues)
            {
                try
                {
                    await dropdown.SelectOptionAsync(new[] { new SelectOptionValue { Label = label } }, new LocatorSelectOptionOptions { Timeout = 2500 }).ConfigureAwait(false);
                    return true;
                }
                catch
                {
                    // Continue attempts
                }
            }

            // Fallback: inspect option elements and select by value when their text partially matches the desired label.
            var options = dropdown.Locator("option");
            var count = await options.CountAsync().ConfigureAwait(false);
            for (var i = 0; i < count; i++)
            {
                string? text = null;
                string? value = null;
                try
                {
                    var option = options.Nth(i);
                    text = (await option.InnerTextAsync().ConfigureAwait(false))?.Trim();
                    value = await option.GetAttributeAsync("value").ConfigureAwait(false);
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrEmpty(text)) continue;
                if (text.IndexOf(optionLabel, StringComparison.OrdinalIgnoreCase) < 0) continue;

                try
                {
                    var optionValue = !string.IsNullOrWhiteSpace(value) ? value : text;
                    await dropdown.SelectOptionAsync(new[] { new SelectOptionValue { Value = optionValue } }, new LocatorSelectOptionOptions { Timeout = 2500 }).ConfigureAwait(false);
                    return true;
                }
                catch
                {
                    // Try next option
                }
            }

            return false;
        }

        private static async Task ScrollElementIntoViewAsync(ILocator locator)
        {
            try
            {
                await locator.ScrollIntoViewIfNeededAsync();
            }
            catch
            {
                // Ignore scrolling issues
            }
        }
    }

    public class PaginationResult
    {
        public bool Clicked { get; set; }
        public bool UrlChanged { get; set; }
        public bool SignatureChanged { get; set; }
        public string BeforeSignature { get; set; } = string.Empty;
        public string AfterSignature { get; set; } = string.Empty;
    }
}