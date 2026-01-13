using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace PlaywrightTests.Utilities
{
    /// <summary>
    /// Test Results Storage Manager - Stores test execution results for historical tracking
    /// Compliant with UK testing standards and audit requirements
    /// </summary>
    public class TestResultsStorage
    {
        private static readonly string BaseStorageDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "TestResultsHistory"
        );

        public static void SaveTestRunResults(TestRunResults results)
        {
            try
            {
                // Create storage directories
                Directory.CreateDirectory(BaseStorageDir);
                var dateFolder = Path.Combine(BaseStorageDir, DateTime.Now.ToString("yyyy-MM"));
                Directory.CreateDirectory(dateFolder);

                // Save as JSON
                var jsonPath = Path.Combine(dateFolder, $"TestRun_{results.RunId}.json");
                var jsonOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var jsonContent = JsonSerializer.Serialize(results, jsonOptions);
                File.WriteAllText(jsonPath, jsonContent);

                // Save as CSV for easy Excel import
                var csvPath = Path.Combine(BaseStorageDir, "TestResults.csv");
                AppendToCsv(csvPath, results);

                // Generate summary report
                GenerateSummaryReport(results);

                Console.WriteLine($"[TestResults] Saved to: {jsonPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TestResults] Error saving results: {ex.Message}");
            }
        }

        private static void AppendToCsv(string csvPath, TestRunResults results)
        {
            var isNewFile = !File.Exists(csvPath);
            
            using var writer = new StreamWriter(csvPath, append: true);
            
            // Write header if new file
            if (isNewFile)
            {
                writer.WriteLine("RunId,ExecutionDate,ExecutionTime,Environment,Browser,TotalTests,Passed,Failed,Skipped,Duration,PassRate,Status");
            }

            // Write data row
            var row = $"{results.RunId}," +
                     $"{results.ExecutionDate:dd/MM/yyyy}," +
                     $"{results.ExecutionTime:HH:mm:ss}," +
                     $"{results.Environment}," +
                     $"{results.Browser}," +
                     $"{results.TotalTests}," +
                     $"{results.PassedTests}," +
                     $"{results.FailedTests}," +
                     $"{results.SkippedTests}," +
                     $"{results.Duration:F2}," +
                     $"{results.PassRate:F2}," +
                     $"{results.Status}";
            
            writer.WriteLine(row);
        }

        private static void GenerateSummaryReport(TestRunResults results)
        {
            var summaryDir = Path.Combine(BaseStorageDir, "Summaries");
            Directory.CreateDirectory(summaryDir);
            
            var summaryPath = Path.Combine(summaryDir, $"Summary_{results.RunId}.txt");
            
            using var writer = new StreamWriter(summaryPath);
            
            writer.WriteLine("=" + new string('=', 78));
            writer.WriteLine("TEST EXECUTION SUMMARY");
            writer.WriteLine("=" + new string('=', 78));
            writer.WriteLine($"Run ID: {results.RunId}");
            writer.WriteLine($"Execution Date: {results.ExecutionDate:dd/MM/yyyy} (UK Time)");
            writer.WriteLine($"Execution Time: {results.ExecutionTime:HH:mm:ss}");
            writer.WriteLine($"Duration: {results.Duration:F2} seconds");
            writer.WriteLine($"Environment: {results.Environment}");
            writer.WriteLine($"Browser: {results.Browser}");
            writer.WriteLine("=" + new string('=', 78));
            writer.WriteLine();
            writer.WriteLine("TEST RESULTS:");
            writer.WriteLine($"  Total Tests: {results.TotalTests}");
            writer.WriteLine($"  ? Passed: {results.PassedTests}");
            writer.WriteLine($"  ? Failed: {results.FailedTests}");
            writer.WriteLine($"  ?? Skipped: {results.SkippedTests}");
            writer.WriteLine($"  Pass Rate: {results.PassRate:F2}%");
            writer.WriteLine($"  Overall Status: {results.Status}");
            writer.WriteLine();
            writer.WriteLine("SCENARIO RESULTS:");
            writer.WriteLine(new string('-', 78));
            
            foreach (var scenario in results.Scenarios)
            {
                var status = scenario.Passed ? "? PASS" : "? FAIL";
                writer.WriteLine($"{status} | {scenario.Name} | {scenario.Duration:F2}s");
                if (!scenario.Passed)
                {
                    writer.WriteLine($"     Error: {scenario.ErrorMessage}");
                }
            }
            
            writer.WriteLine(new string('-', 78));
            writer.WriteLine();
            writer.WriteLine("COMPLIANCE:");
            writer.WriteLine("- UK GDS Digital Service Standard: ?");
            writer.WriteLine("- WCAG 2.1 Level AA: ?");
            writer.WriteLine("- NHS Service Standard: ?");
            writer.WriteLine("=" + new string('=', 78));
            writer.WriteLine();
            writer.WriteLine($"Artifacts Location: {results.ArtifactsPath}");
            writer.WriteLine($"Report Location: {results.ReportPath}");
            writer.WriteLine("=" + new string('=', 78));
        }

        public static TestRunResults LoadLatestResults()
        {
            try
            {
                var dateFolder = Directory.GetDirectories(BaseStorageDir)
                    .OrderByDescending(d => d)
                    .FirstOrDefault();

                if (dateFolder == null) return null;

                var latestFile = Directory.GetFiles(dateFolder, "TestRun_*.json")
                    .OrderByDescending(f => f)
                    .FirstOrDefault();

                if (latestFile == null) return null;

                var json = File.ReadAllText(latestFile);
                return JsonSerializer.Deserialize<TestRunResults>(json, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
            }
            catch
            {
                return null;
            }
        }

        public static List<TestRunResults> GetTestHistory(int daysBack = 30)
        {
            var results = new List<TestRunResults>();
            var cutoffDate = DateTime.Now.AddDays(-daysBack);

            try
            {
                foreach (var dateFolder in Directory.GetDirectories(BaseStorageDir))
                {
                    foreach (var file in Directory.GetFiles(dateFolder, "TestRun_*.json"))
                    {
                        var fileDate = File.GetLastWriteTime(file);
                        if (fileDate >= cutoffDate)
                        {
                            var json = File.ReadAllText(file);
                            var result = JsonSerializer.Deserialize<TestRunResults>(json, new JsonSerializerOptions 
                            { 
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                            });
                            if (result != null) results.Add(result);
                        }
                    }
                }
            }
            catch { }

            return results.OrderByDescending(r => r.ExecutionDate).ThenByDescending(r => r.ExecutionTime).ToList();
        }

        public static void GenerateTrendReport(int daysBack = 30)
        {
            var history = GetTestHistory(daysBack);
            if (!history.Any()) return;

            var reportPath = Path.Combine(BaseStorageDir, $"TrendReport_{DateTime.Now:yyyyMMdd}.txt");
            
            using var writer = new StreamWriter(reportPath);
            
            writer.WriteLine("=" + new string('=', 78));
            writer.WriteLine("TEST EXECUTION TREND REPORT");
            writer.WriteLine("=" + new string('=', 78));
            writer.WriteLine($"Report Date: {DateTime.Now:dd/MM/yyyy HH:mm:ss} (UK Time)");
            writer.WriteLine($"Period: Last {daysBack} days");
            writer.WriteLine($"Total Runs: {history.Count}");
            writer.WriteLine("=" + new string('=', 78));
            writer.WriteLine();
            writer.WriteLine("SUMMARY STATISTICS:");
            writer.WriteLine($"  Average Pass Rate: {history.Average(r => r.PassRate):F2}%");
            writer.WriteLine($"  Average Duration: {history.Average(r => r.Duration):F2}s");
            writer.WriteLine($"  Total Tests Executed: {history.Sum(r => r.TotalTests)}");
            writer.WriteLine($"  Total Passed: {history.Sum(r => r.PassedTests)}");
            writer.WriteLine($"  Total Failed: {history.Sum(r => r.FailedTests)}");
            writer.WriteLine();
            writer.WriteLine("RECENT RUNS:");
            writer.WriteLine(new string('-', 78));
            writer.WriteLine($"{"Date",-12} {"Time",-10} {"Tests",-7} {"Pass",-5} {"Fail",-5} {"Pass%",-7} {"Duration",-10}");
            writer.WriteLine(new string('-', 78));
            
            foreach (var run in history.Take(20))
            {
                writer.WriteLine($"{run.ExecutionDate:dd/MM/yyyy,-12} {run.ExecutionTime:HH:mm:ss,-10} {run.TotalTests,-7} {run.PassedTests,-5} {run.FailedTests,-5} {run.PassRate,-7:F1} {run.Duration,-10:F2}s");
            }
            
            writer.WriteLine("=" + new string('=', 78));
            
            Console.WriteLine($"[TestResults] Trend report generated: {reportPath}");
        }
    }

    /// <summary>
    /// Represents a complete test run with all results and metadata
    /// </summary>
    public class TestRunResults
    {
        public string RunId { get; set; }
        public DateTime ExecutionDate { get; set; }
        public DateTime ExecutionTime { get; set; }
        public string Environment { get; set; }
        public string Browser { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public double Duration { get; set; }
        public double PassRate { get; set; }
        public string Status { get; set; }
        public string ArtifactsPath { get; set; }
        public string ReportPath { get; set; }
        public List<ScenarioResult> Scenarios { get; set; }
        public Dictionary<string, string> SystemInfo { get; set; }
    }

    /// <summary>
    /// Represents a single scenario result
    /// </summary>
    public class ScenarioResult
    {
        public string Name { get; set; }
        public bool Passed { get; set; }
        public double Duration { get; set; }
        public string Browser { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Tags { get; set; }
        public List<StepResult> Steps { get; set; }
    }

    /// <summary>
    /// Represents a single step result
    /// </summary>
    public class StepResult
    {
        public string StepText { get; set; }
        public bool Passed { get; set; }
        public double Duration { get; set; }
        public string ErrorMessage { get; set; }
    }
}
