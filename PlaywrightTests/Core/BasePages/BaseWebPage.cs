/*
 * ============================================================================
 * Project: Reusable Test Automation Framework
 * File: BaseWebPage.cs
 * Purpose: Base page object that works for ANY web page
 * 
 * Description:
 * This is the foundation of the Page Object Model. All application-specific
 * page objects should inherit from this class to get access to common
 * functionality like navigation, waits, evidence capture, and accessibility testing.
 * 
 * Key Features:
 * - Application-agnostic design
 * - Reusable helper methods
 * - Built-in waiting strategies
 * - Automatic evidence capture
 * - Accessibility testing support
 * - Performance measurement
 * 
 * Usage:
 * ```csharp
 * public class MyApplicationHomePage : BaseWebPage
 * {
 *     public MyApplicationHomePage(IPage page) : base(page) { }
 *     
 *     public async Task ClickLoginAsync()
 *     {
 *         await ClickElementAsync(".login-button");
 *     }
 * }
 * ```
 * 
 * Standards Compliance:
 * - UK GDS Digital Service Standard
 * - WCAG 2.1 Level AA (Accessibility)
 * - ISO/IEC 25010:2011 (Software Quality)
 * 
 * Author: SAIRAM BACHU
 * Organisation: Northumbria Healthcare NHS Foundation Trust
 * Version: 3.0.0 - Reusable Architecture
 * Last Modified: January 2025
 * ============================================================================
 */

using Microsoft.Playwright;
using PlaywrightTests.Core.Configuration;
using PlaywrightTests.Utilities;
using System;
using System.Threading.Tasks;

namespace PlaywrightTests.Core.BasePages
{
    /// <summary>
    /// Base class for all page objects in the framework.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides common functionality that works for ANY web page,
    /// regardless of the application being tested. All application-specific
    /// page objects should inherit from this class.
    /// </para>
    /// <para>
    /// Key Principles:
    /// - DRY (Don't Repeat Yourself)
    /// - Single Responsibility
    /// - Open/Closed (open for extension, closed for modification)
    /// </para>
    /// </remarks>
    public abstract class BaseWebPage
    {
        #region Protected Properties

        /// <summary>
        /// Gets the Playwright page instance.
        /// </summary>
        protected IPage Page { get; }

        /// <summary>
        /// Gets the application configuration.
        /// </summary>
        protected ApplicationConfiguration Config { get; }

        /// <summary>
        /// Gets the timeout value in milliseconds from configuration.
        /// </summary>
        protected int Timeout => Config.Timeout;

        /// <summary>
        /// Gets the implicit wait value in milliseconds from configuration.
        /// </summary>
        protected int ImplicitWait => Config.ImplicitWait;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseWebPage"/> class.
        /// </summary>
        /// <param name="page">The Playwright page instance.</param>
        protected BaseWebPage(IPage page)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            Config = ApplicationConfiguration.Instance;
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Navigates to a specified URL.
        /// </summary>
        /// <param name="url">The URL to navigate to (absolute or relative).</param>
        /// <param name="waitUntil">The wait condition (default: NetworkIdle).</param>
        public virtual async Task NavigateToAsync(string url, WaitUntilState waitUntil = WaitUntilState.NetworkIdle)
        {
            // Handle relative URLs
            if (!url.StartsWith("http"))
            {
                url = Config.BaseUrl.TrimEnd('/') + "/" + url.TrimStart('/');
            }

            await Page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = waitUntil,
                Timeout = Timeout
            });

            Console.WriteLine($"[Navigation] Navigated to: {url}");
        }

        /// <summary>
        /// Navigates to the application homepage.
        /// </summary>
        public virtual async Task NavigateToHomeAsync()
        {
            await NavigateToAsync(Config.BaseUrl);
        }

        /// <summary>
        /// Refreshes the current page.
        /// </summary>
        public virtual async Task RefreshPageAsync()
        {
            await Page.ReloadAsync(new PageReloadOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = Timeout
            });

            Console.WriteLine("[Navigation] Page refreshed");
        }

        /// <summary>
        /// Navigates back in browser history.
        /// </summary>
        public virtual async Task GoBackAsync()
        {
            await Page.GoBackAsync(new PageGoBackOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = Timeout
            });

            Console.WriteLine("[Navigation] Navigated back");
        }

        #endregion

        #region Element Interaction Methods

        /// <summary>
        /// Clicks an element identified by the specified selector.
        /// </summary>
        /// <param name="selector">The CSS selector or text selector.</param>
        /// <param name="timeout">Optional timeout override.</param>
        public virtual async Task ClickElementAsync(string selector, int? timeout = null)
        {
            await WaitForElementAsync(selector, timeout);
            await Page.Locator(selector).ClickAsync(new LocatorClickOptions
            {
                Timeout = timeout ?? Timeout
            });

            Console.WriteLine($"[Interaction] Clicked element: {selector}");
        }

        /// <summary>
        /// Fills a text input element.
        /// </summary>
        /// <param name="selector">The CSS selector for the input.</param>
        /// <param name="text">The text to fill.</param>
        /// <param name="timeout">Optional timeout override.</param>
        public virtual async Task FillTextAsync(string selector, string text, int? timeout = null)
        {
            await WaitForElementAsync(selector, timeout);
            await Page.Locator(selector).FillAsync(text, new LocatorFillOptions
            {
                Timeout = timeout ?? Timeout
            });

            Console.WriteLine($"[Interaction] Filled text in {selector}: {text}");
        }

        /// <summary>
        /// Gets the text content of an element.
        /// </summary>
        /// <param name="selector">The CSS selector.</param>
        /// <param name="timeout">Optional timeout override.</param>
        /// <returns>The text content of the element.</returns>
        public virtual async Task<string> GetTextAsync(string selector, int? timeout = null)
        {
            await WaitForElementAsync(selector, timeout);
            var text = await Page.Locator(selector).TextContentAsync(new LocatorTextContentOptions
            {
                Timeout = timeout ?? Timeout
            });

            Console.WriteLine($"[Interaction] Got text from {selector}: {text}");
            return text ?? string.Empty;
        }

        /// <summary>
        /// Checks if an element is visible.
        /// </summary>
        /// <param name="selector">The CSS selector.</param>
        /// <param name="timeout">Optional timeout in milliseconds.</param>
        /// <returns>True if the element is visible; otherwise, false.</returns>
        public virtual async Task<bool> IsElementVisibleAsync(string selector, int? timeout = null)
        {
            try
            {
                await Page.Locator(selector).WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeout ?? ImplicitWait
                });
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the count of elements matching the selector.
        /// </summary>
        /// <param name="selector">The CSS selector.</param>
        /// <returns>The number of matching elements.</returns>
        public virtual async Task<int> GetElementCountAsync(string selector)
        {
            return await Page.Locator(selector).CountAsync();
        }

        #endregion

        #region Wait Methods

        /// <summary>
        /// Waits for an element to be visible.
        /// </summary>
        /// <param name="selector">The CSS selector.</param>
        /// <param name="timeout">Optional timeout override.</param>
        public virtual async Task WaitForElementAsync(string selector, int? timeout = null)
        {
            await Page.Locator(selector).WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeout ?? Timeout
            });

            Console.WriteLine($"[Wait] Element visible: {selector}");
        }

        /// <summary>
        /// Waits for an element to be hidden.
        /// </summary>
        /// <param name="selector">The CSS selector.</param>
        /// <param name="timeout">Optional timeout override.</param>
        public virtual async Task WaitForElementHiddenAsync(string selector, int? timeout = null)
        {
            await Page.Locator(selector).WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = timeout ?? Timeout
            });

            Console.WriteLine($"[Wait] Element hidden: {selector}");
        }

        /// <summary>
        /// Waits for the page to be fully loaded.
        /// </summary>
        /// <param name="state">The load state to wait for.</param>
        public virtual async Task WaitForPageLoadAsync(LoadState state = LoadState.NetworkIdle)
        {
            await Page.WaitForLoadStateAsync(state, new PageWaitForLoadStateOptions
            {
                Timeout = Timeout
            });

            Console.WriteLine($"[Wait] Page loaded: {state}");
        }

        /// <summary>
        /// Waits for a specific amount of time.
        /// </summary>
        /// <param name="milliseconds">The time to wait in milliseconds.</param>
        public virtual async Task WaitAsync(int milliseconds)
        {
            await Page.WaitForTimeoutAsync(milliseconds);
            Console.WriteLine($"[Wait] Waited for {milliseconds}ms");
        }

        #endregion

        #region Evidence Capture Methods

        /// <summary>
        /// Captures a screenshot of the current page.
        /// </summary>
        /// <param name="fileName">The filename for the screenshot (without extension).</param>
        /// <param name="fullPage">Whether to capture the full page or just viewport.</param>
        /// <returns>The path to the saved screenshot.</returns>
        public virtual async Task<string> CaptureScreenshotAsync(string fileName, bool fullPage = true)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var screenshotPath = $"TestResults/screenshots/{fileName}_{timestamp}.png";

            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = screenshotPath,
                FullPage = fullPage,
                Type = ScreenshotType.Png
            });

            Console.WriteLine($"[Evidence] Screenshot saved: {screenshotPath}");
            return screenshotPath;
        }

        /// <summary>
        /// Captures the page source (HTML).
        /// </summary>
        /// <returns>The HTML content of the page.</returns>
        public virtual async Task<string> CapturePageSourceAsync()
        {
            var content = await Page.ContentAsync();
            Console.WriteLine($"[Evidence] Page source captured ({content.Length} characters)");
            return content;
        }

        #endregion

        #region Accessibility Methods

        /// <summary>
        /// Runs accessibility tests on the current page using axe-core.
        /// </summary>
        /// <returns>The accessibility test results.</returns>
        public virtual async Task<AxeResults> RunAccessibilityTestsAsync()
        {
            if (!Config.EnableAccessibilityTests)
            {
                Console.WriteLine("[Accessibility] Accessibility tests are disabled");
                return null;
            }

            var results = await AxeHelper.RunAxeAsync(Page);
            Console.WriteLine($"[Accessibility] Tests complete. Violations: {results?.ViolationsCount ?? 0}");
            return results;
        }

        /// <summary>
        /// Validates that the page has proper ARIA landmarks.
        /// </summary>
        /// <returns>True if landmarks are present; otherwise, false.</returns>
        public virtual async Task<bool> ValidateAriaLandmarksAsync()
        {
            var landmarksSelector = "[role='navigation'], [role='main'], [role='search'], [role='banner'], [role='contentinfo']";
            var count = await GetElementCountAsync(landmarksSelector);

            Console.WriteLine($"[Accessibility] ARIA Landmarks found: {count}");
            return count > 0;
        }

        /// <summary>
        /// Validates that the page has a proper heading structure.
        /// </summary>
        /// <returns>True if headings are present; otherwise, false.</returns>
        public virtual async Task<bool> ValidateHeadingStructureAsync()
        {
            var count = await GetElementCountAsync("h1, h2, h3, h4, h5, h6");
            Console.WriteLine($"[Accessibility] Headings found: {count}");
            return count > 0;
        }

        #endregion

        #region Performance Methods

        /// <summary>
        /// Measures the page load time.
        /// </summary>
        /// <returns>The page load time in seconds.</returns>
        public virtual async Task<double> MeasurePageLoadTimeAsync()
        {
            if (!Config.EnablePerformanceTests)
            {
                Console.WriteLine("[Performance] Performance tests are disabled");
                return 0.0;
            }

            try
            {
                var loadTime = await Page.EvaluateAsync<double>(@"
                    () => {
                        const timing = performance.timing;
                        const navigation = performance.getEntriesByType('navigation')[0];
                        
                        if (navigation) {
                            return (navigation.loadEventEnd - navigation.fetchStart) / 1000;
                        }
                        
                        return (timing.loadEventEnd - timing.navigationStart) / 1000;
                    }
                ");

                Console.WriteLine($"[Performance] Page load time: {loadTime:F3}s");
                return loadTime;
            }
            catch
            {
                return 0.0;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the current page URL.
        /// </summary>
        /// <returns>The current URL.</returns>
        public virtual string GetCurrentUrl()
        {
            return Page.Url;
        }

        /// <summary>
        /// Gets the page title.
        /// </summary>
        /// <returns>The page title.</returns>
        public virtual async Task<string> GetPageTitleAsync()
        {
            return await Page.TitleAsync();
        }

        /// <summary>
        /// Scrolls to an element.
        /// </summary>
        /// <param name="selector">The CSS selector.</param>
        public virtual async Task ScrollToElementAsync(string selector)
        {
            await Page.Locator(selector).ScrollIntoViewIfNeededAsync();
            Console.WriteLine($"[Scroll] Scrolled to element: {selector}");
        }

        /// <summary>
        /// Accepts cookie consent if prompted.
        /// </summary>
        /// <remarks>
        /// This is a generic implementation. Override in application-specific
        /// pages if needed for custom cookie handling.
        /// </remarks>
        public virtual async Task AcceptCookiesIfPromptedAsync()
        {
            var cookieSelectors = new[]
            {
                "button:has-text('Accept')",
                "button:has-text('Accept all')",
                "button:has-text('I accept')",
                "button:has-text('OK')",
                ".cookie-accept",
                "#cookie-accept"
            };

            foreach (var selector in cookieSelectors)
            {
                try
                {
                    if (await IsElementVisibleAsync(selector, 2000))
                    {
                        await ClickElementAsync(selector, 2000);
                        Console.WriteLine("[Cookies] Cookie consent accepted");
                        await WaitAsync(500);
                        return;
                    }
                }
                catch { }
            }

            Console.WriteLine("[Cookies] No cookie prompt found");
        }

        #endregion
    }
}
