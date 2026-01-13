using System.Threading.Tasks;
using Microsoft.Playwright;

namespace PlaywrightTests.PageObjects
{
    public abstract class BasePage
    {
        protected IPage Page { get; }

        protected BasePage(IPage page)
        {
            Page = page;
        }

        public Task<string> GetContentAsync() => Page.ContentAsync();

        protected ILocator Locator(string selector) => Page.Locator(selector);

        public Task GoToAsync(string url, PageGotoOptions options) => Page.GotoAsync(url, options);

        protected Task WaitForNetworkIdleAsync() => Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Capture full page screenshot
        public async Task CaptureFullPageScreenshotAsync(string filePath)
        {
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = filePath, FullPage = true });
        }

        // Capture element screenshot
        public async Task CaptureElementScreenshotAsync(string selector, string filePath)
        {
            var element = Locator(selector);
            if (await element.CountAsync() > 0)
                await element.First.ScreenshotAsync(new LocatorScreenshotOptions { Path = filePath });
            else
                throw new System.Exception($"Element '{selector}' not found for screenshot.");
        }

        // Capture page snapshot (HTML)
        public async Task CaptureSnapshotAsync(string filePath)
        {
            var html = await Page.ContentAsync();
            await System.IO.File.WriteAllTextAsync(filePath, html);
        }

        // Capture screenshot to buffer
        public async Task<byte[]> CaptureScreenshotBufferAsync(bool fullPage = false)
        {
            return await Page.ScreenshotAsync(new PageScreenshotOptions { FullPage = fullPage });
        }

        // Emulate pan gesture (drag)
        public async Task PanAsync(string selector, int deltaX, int deltaY)
        {
            var element = Locator(selector);
            if (await element.CountAsync() == 0) throw new System.Exception($"Element '{selector}' not found for pan gesture.");

            var box = await element.First.BoundingBoxAsync();
            if (box == null) throw new System.Exception($"Unable to determine bounding box for element '{selector}' for pan gesture.");

            var startX = box.X + box.Width / 2;
            var startY = box.Y + box.Height / 2;
            var endX = startX + deltaX;
            var endY = startY + deltaY;

            await Page.Mouse.MoveAsync(startX, startY);
            await Page.Mouse.DownAsync();
            await Page.Mouse.MoveAsync(endX, endY, new MouseMoveOptions { Steps = 10 });
            await Page.Mouse.UpAsync();
        }

        // Emulate pinch gesture (zoom)
        public async Task PinchAsync(string selector, double scale = 1.5)
        {
            // Playwright does not have a direct pinch API in all platforms; simulate a simple zoom transform on the element
            // Use a safe evaluate call that accepts arguments instead of interpolating values into the script.
            var result = await Page.EvaluateAsync<bool>(@"(sel, sc) => {
                try {
                    const el = document.querySelector(sel);
                    if (!el) return false;
                    el.style.transform = 'scale(' + sc + ')';
                    el.style.transition = 'transform 0.3s ease';
                    return true;
                } catch (e) { return false; }
            }", new object[] { selector, scale });

            if (!result) throw new System.Exception($"Pinch simulation could not be applied to selector: {selector}");
        }
    }
}