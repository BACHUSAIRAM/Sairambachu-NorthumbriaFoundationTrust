using Microsoft.Playwright;
using System.Collections.Generic;

namespace PlaywrightTests.Context
{
    /// <summary>
    /// Scenario-scoped context object for SpecFlow DI. Holds Playwright objects for the running scenario.
    /// SpecFlow will create one instance per scenario and inject it into hooks and step classes.
    /// </summary>
    public class SpecFlowContext
    {
        public IPlaywright? Playwright { get; set; }
        public IBrowser? Browser { get; set; }
        public IBrowserContext? BrowserContext { get; set; }
        public IPage? Page { get; set; }
        public string? RunResultsDir { get; set; }

        // Added support for multi-browser runs: lists will contain one entry per requested browser
        public List<IPlaywright>? Playwrights { get; set; }
        public List<IBrowser>? Browsers { get; set; }
        public List<IBrowserContext>? BrowserContexts { get; set; }
        public List<IPage>? Pages { get; set; }
    }
}