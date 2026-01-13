using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PlaywrightTests.Utilities
{
    /// <summary>
    /// Shared helpers for interacting with site-level chrome such as cookie banners.
    /// </summary>
    public static class SiteInteractionHelper
    {
        private static readonly string[] CookieSelectors = new[]
        {
            "button:has-text('Accept all')",
            "button:has-text('Accept cookies')",
            "button:has-text('Accept')",
            "button:has-text('I accept')",
            "button[aria-label*='accept']",
            "#ccc-recommended-settings",
            ".cc-btn.cc-allow",
            ".cc-window button",
            "#onetrust-accept-btn-handler"
        };

        public static async Task<bool> DismissCookieBannerIfPresentAsync(IPage page, string label)
        {
            foreach (var selector in CookieSelectors)
            {
                try
                {
                    var locator = page.Locator(selector);
                    if (await locator.CountAsync().ConfigureAwait(false) > 0 && await locator.First.IsVisibleAsync().ConfigureAwait(false))
                    {
                        await locator.First.ClickAsync(new LocatorClickOptions { Timeout = 2000 }).ConfigureAwait(false);
                        await page.WaitForTimeoutAsync(400).ConfigureAwait(false);
                        System.Console.WriteLine($"[Cookies:{label}] Dismissed cookie banner via '{selector}'");
                        return true;
                    }
                }
                catch
                {
                    // Ignore selector-specific failures; continue with next option.
                }
            }

            return false;
        }
    }
}
