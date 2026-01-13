using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PlaywrightTests.PageObjects
{
    public class HomePage : BasePage
    {
        public HomePage(IPage page) : base(page)
        {
        }

        private static readonly string[] PrimarySearchSelectors =
        {
            "form[action*='search'] input[name='query']",
            "#search-query-carousel-40618",
            "input[name='query']"
        };

        // Centralized resolver for the search input. Prefers ARIA role then common CSS selectors.
        public async Task<ILocator?> FindSearchInputAsync()
        {
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await EnsureHeroSearchReadyAsync();

            // Prefer role-based locator when available
            try
            {
                var roleLocator = Page.GetByRole(AriaRole.Searchbox);
                var visibleRole = await FirstVisibleAsync(roleLocator);
                if (visibleRole != null) return visibleRole;

                var searchRegion = Page.GetByRole(AriaRole.Search);
                var visibleRegionInput = await FirstVisibleAsync(searchRegion.Locator("input, input[type='search'], input[type='text']"));
                if (visibleRegionInput != null) return visibleRegionInput;
            }
            catch { /* Ignore role lookup failures */ }

            // Hero carousel search block
            var heroSelectors = new[]
            {
                "form[action*='search'] input[name='query']",
                "form[action*='search'] input.search-field",
                "#search-query-carousel-40618",
                "input[name='query']"
            };
            var heroResult = await LocateFromSelectorsAsync(heroSelectors);
            if (heroResult != null) return heroResult;

            // Additional structural search regions (forms with role=search, header search containers, etc.)
            var searchRegions = new[]
            {
                "form[role='search']",
                "[role='search']",
                "header .search",
                "#search",
                ".search-panel",
                "[data-component*='search']",
                ".nhsuk-header__search"
            };

            foreach (var regionSelector in searchRegions)
            {
                try
                {
                    var region = Page.Locator(regionSelector);
                    var regionInput = await FirstVisibleAsync(region.Locator("input, input[type='search'], input[type='text']"));
                    if (regionInput != null) return regionInput;
                }
                catch { }
            }

            var selectors = new[] {
                "input[name='query']",
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
                "input[placeholder*='search']",
                "form[role='search'] input",
                "[role='search'] input"
            };

            var selectorResult = await LocateFromSelectorsAsync(selectors);
            if (selectorResult != null) return selectorResult;

            // If not found, try toggling any header search control to reveal input
            var toggleSelectors = new[]
            {
                "button[aria-label*='search']",
                "button[aria-controls*='search']",
                "button.search-toggle",
                ".search-toggle",
                ".nhsuk-header__search-toggle",
                "[data-action='toggle-search']"
            };

            foreach (var toggleSelector in toggleSelectors)
            {
                var toggle = Page.Locator(toggleSelector);
                if (await toggle.CountAsync() > 0)
                {
                    try
                    {
                        await toggle.First.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                        await Page.WaitForTimeoutAsync(300);
                    }
                    catch { }

                    var toggledResult = await LocateFromSelectorsAsync(selectors);
                    if (toggledResult != null) return toggledResult;
                }
            }

            return null;
        }

        private async Task EnsureHeroSearchReadyAsync()
        {
            foreach (var selector in PrimarySearchSelectors)
            {
                var locator = Page.Locator(selector).First;
                try
                {
                    await locator.WaitForAsync(new LocatorWaitForOptions
                    {
                        State = WaitForSelectorState.Visible,
                        Timeout = 10000
                    });
                    return;
                }
                catch
                {
                    // Try next selector
                }
            }
        }

        public Task EnsureSearchInputReadyAsync() => EnsureHeroSearchReadyAsync();

        private async Task<ILocator?> LocateFromSelectorsAsync(string[] selectors)
        {
            foreach (var sel in selectors)
            {
                try
                {
                    var locator = Page.Locator(sel);
                    var visible = await FirstVisibleAsync(locator);
                    if (visible != null) return visible;
                }
                catch { }
            }
            return null;
        }

        private static async Task<ILocator?> FirstVisibleAsync(ILocator locator)
        {
            if (locator == null) return null;
            var count = await locator.CountAsync();
            for (int i = 0; i < count; i++)
            {
                var candidate = locator.Nth(i);
                try
                {
                    await candidate.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 1500 });
                    return candidate;
                }
                catch
                {
                    // Ignore non-visible entries
                }
            }
            return null;
        }

        public async Task SearchAsync(string term)
        {
            var inputLocator = await FindSearchInputAsync();

            if (inputLocator == null)
                throw new Exception("Search input not found on page");

            await inputLocator.FillAsync(term);
            await inputLocator.PressAsync("Enter");
            await WaitForNetworkIdleAsync();
        }

        // Example usage: capture element screenshot after search
        public async Task CaptureSearchInputScreenshotAsync(string filePath)
        {
            await CaptureElementScreenshotAsync("input[type='search'], input[name='s'], input[aria-label*='search'], input.search-field", filePath);
        }
    }
}