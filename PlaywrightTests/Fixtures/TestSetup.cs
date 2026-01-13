/*
 * ============================================================================
 * Project: Northumbria Healthcare NHS Foundation Trust - Test Automation
 * File: TestSetup.cs
 * Purpose: Global test setup and teardown fixture
 * 
 * Standards Compliance:
 * - UK GDS Digital Service Standard
 * - NHS Digital Technology Standards
 * - NUnit Best Practices
 * 
 * Author: SAIRAM BACHU
 * Organisation: Northumbria Healthcare NHS Foundation Trust
 * Last Modified: January 2025
 * Version: 3.0.0
 * ============================================================================
 */

using System;
using System.IO;
using NUnit.Framework;
using PlaywrightTests.Utilities;

namespace PlaywrightTests.Fixtures
{
    /// <summary>
    /// Global test setup fixture that runs once before/after all tests.
    /// Methods must be static for NUnit InstancePerTestCase mode compatibility.
    /// </summary>
    [SetUpFixture]
    public class TestSetup
    {
        private static string? _runResultsDir;

        /// <summary>
        /// Gets the results directory for the current test run.
        /// </summary>
        public static string? RunResultsDir => _runResultsDir;

        /// <summary>
        /// Global setup - runs once before all tests.
        /// Must be static for NUnit InstancePerTestCase mode.
        /// </summary>
        [OneTimeSetUp]
        public static void GlobalSetup()
        {
            var dt = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            
            // Get TestResults directory under the PlaywrightTests project
            var testResultsRoot = GetTestResultsDirectory();
            
            // Create TestResults folder inside the project
            _runResultsDir = Path.Combine(testResultsRoot, $"Run-{dt}");
            Directory.CreateDirectory(_runResultsDir);
            Directory.CreateDirectory(Path.Combine(_runResultsDir, "traces"));
            Directory.CreateDirectory(Path.Combine(_runResultsDir, "logs"));
            Directory.CreateDirectory(Path.Combine(_runResultsDir, "screenshots"));
            Directory.CreateDirectory(Path.Combine(_runResultsDir, "videos"));
            Directory.CreateDirectory(Path.Combine(_runResultsDir, "evidence"));
            Directory.CreateDirectory(Path.Combine(_runResultsDir, "accessibility-reports"));
            Directory.CreateDirectory(Path.Combine(_runResultsDir, "performance-reports"));

            Console.WriteLine("=" + new string('=', 78));
            Console.WriteLine("NORTHUMBRIA HEALTHCARE NHS FOUNDATION TRUST - TEST AUTOMATION");
            Console.WriteLine("=" + new string('=', 78));
            Console.WriteLine($"[TestResults] Test Run Started: {DateTime.Now:dd/MM/yyyy HH:mm:ss} (UK Time)");
            Console.WriteLine($"[TestResults] Results Directory: {_runResultsDir}");
            Console.WriteLine("=" + new string('=', 78));

            // Initialize ExtentReports for the entire run
            ExtentReportHelper.InitReport(_runResultsDir);
        }

        /// <summary>
        /// Global teardown - runs once after all tests.
        /// Must be static for NUnit InstancePerTestCase mode.
        /// </summary>
        [OneTimeTearDown]
        public static void GlobalTearDown()
        {
            // Ensure report is flushed at the end of the run
            ExtentReportHelper.FlushReport();
            
            Console.WriteLine("=" + new string('=', 78));
            Console.WriteLine($"[TestResults] Test Run Completed: {DateTime.Now:dd/MM/yyyy HH:mm:ss} (UK Time)");
            Console.WriteLine($"[TestResults] Final Results: {_runResultsDir}");
            Console.WriteLine("=" + new string('=', 78));
        }

        /// <summary>
        /// Gets the TestResults directory inside the PlaywrightTests project.
        /// </summary>
        private static string GetTestResultsDirectory()
        {
            // Start from the base directory (typically bin/Debug/net8.0)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var currentDir = new DirectoryInfo(baseDir);
            
            // Navigate up to PlaywrightTests project root (3 levels up)
            // bin/Debug/net8.0 -> Debug -> bin -> PlaywrightTests
            for (int i = 0; i < 3 && currentDir.Parent != null; i++)
            {
                currentDir = currentDir.Parent;
            }
            
            var projectRoot = currentDir.FullName;
            
            // Create TestResults folder under PlaywrightTests
            var testResultsFolder = Path.Combine(projectRoot, "TestResults");
            
            // Ensure the TestResults directory exists
            Directory.CreateDirectory(testResultsFolder);
            
            return testResultsFolder;
        }
    }
}
