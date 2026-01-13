using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Playwright;
using System.Text.Json;
using System.Collections.Generic;

namespace PlaywrightTests.Utilities
{
    public class AxeResults
    {
        public List<AxeViolation> Violations { get; set; } = new();
        public int ViolationsCount { get; set; }
    }

    public class AxeViolation
    {
        public string Id { get; set; } = "";
        public string Impact { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Tags { get; set; } = new();
        public List<AxeNode> Nodes { get; set; } = new();
    }

    public class AxeNode
    {
        public string Html { get; set; } = "";
        public List<string> Target { get; set; } = new();
    }

    public static class AxeHelper
    {
        private const string AxeCdnUrl = "https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.6.3/axe.min.js";
        private const string AxeRelativePath = "tools/axe/axe.min.js";

        /// <summary>
        /// Ensure that a local copy of axe.min.js exists in the repo output folder. If not present,
        /// this will attempt to download it from the canonical CDN and save it to the local path.
        /// Returns the full path to the local file that can be used with Playwright's AddScriptTagAsync(Path = ...).
        /// </summary>
        public static async Task<string> EnsureLocalAxeAsync()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var localPath = Path.Combine(baseDir, AxeRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(localPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(localPath)) return localPath;

            // Try to download from CDN
            try
            {
                using var http = new HttpClient();
                http.Timeout = TimeSpan.FromSeconds(30);
                var bytes = await http.GetByteArrayAsync(AxeCdnUrl);
                await File.WriteAllBytesAsync(localPath, bytes);
                return localPath;
            }
            catch (Exception)
            {
                // If download fails, rethrow as a clearer exception
                throw new InvalidOperationException($"Failed to obtain axe.min.js from {AxeCdnUrl}. Please place a copy at {localPath}.");
            }
        }

        /// <summary>
        /// Run axe-core accessibility tests on the current page
        /// </summary>
        public static async Task<AxeResults?> RunAxeAsync(IPage page)
        {
            try
            {
                // Try to inject axe-core from CDN
                try
                {
                    await page.AddScriptTagAsync(new PageAddScriptTagOptions { Url = AxeCdnUrl });
                }
                catch
                {
                    // Fallback to local file
                    var localAxePath = await EnsureLocalAxeAsync();
                    await page.AddScriptTagAsync(new PageAddScriptTagOptions { Path = localAxePath });
                }

                // Wait for axe to be available
                await page.WaitForFunctionAsync("typeof window.axe !== 'undefined'", null, new PageWaitForFunctionOptions { Timeout = 5000 });

                // Run axe
                var resultsJson = await page.EvaluateAsync<string>(@"async () => {
                    try {
                        const results = await axe.run();
                        return JSON.stringify(results);
                    } catch (e) {
                        return JSON.stringify({ violations: [], error: e.message });
                    }
                }");

                if (string.IsNullOrEmpty(resultsJson))
                {
                    return new AxeResults();
                }

                // Parse the results
                using var doc = JsonDocument.Parse(resultsJson);
                var root = doc.RootElement;
                
                var axeResults = new AxeResults();
                
                if (root.TryGetProperty("violations", out var violationsElement))
                {
                    var violations = new List<AxeViolation>();
                    
                    foreach (var violation in violationsElement.EnumerateArray())
                    {
                        var v = new AxeViolation();
                        
                        if (violation.TryGetProperty("id", out var id))
                            v.Id = id.GetString() ?? "";
                            
                        if (violation.TryGetProperty("impact", out var impact))
                            v.Impact = impact.GetString() ?? "";
                            
                        if (violation.TryGetProperty("description", out var desc))
                            v.Description = desc.GetString() ?? "";
                            
                        if (violation.TryGetProperty("tags", out var tags))
                        {
                            v.Tags = new List<string>();
                            foreach (var tag in tags.EnumerateArray())
                            {
                                v.Tags.Add(tag.GetString() ?? "");
                            }
                        }
                        
                        if (violation.TryGetProperty("nodes", out var nodes))
                        {
                            v.Nodes = new List<AxeNode>();
                            foreach (var node in nodes.EnumerateArray())
                            {
                                var n = new AxeNode();
                                if (node.TryGetProperty("html", out var html))
                                    n.Html = html.GetString() ?? "";
                                if (node.TryGetProperty("target", out var target))
                                {
                                    n.Target = new List<string>();
                                    foreach (var t in target.EnumerateArray())
                                    {
                                        n.Target.Add(t.GetString() ?? "");
                                    }
                                }
                                v.Nodes.Add(n);
                            }
                        }
                        
                        violations.Add(v);
                    }
                    
                    axeResults.Violations = violations;
                    axeResults.ViolationsCount = violations.Count;
                }
                
                return axeResults;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AxeHelper] Failed to run accessibility tests: {ex.Message}");
                return null;
            }
        }
    }
}
