using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;

namespace PlaywrightTests.Integrations.Xray
{
    /// <summary>
    /// Xray for Jira integration client for importing test results
    /// Supports Xray Cloud and Data Center/Server
    /// </summary>
    public class XrayClient
    {
        private readonly XrayConfiguration _config;
        private readonly RestClient _client;

        public XrayClient(XrayConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            if (!_config.Enabled)
            {
                Console.WriteLine("[Xray] Integration is disabled in configuration");
                return;
            }

            var options = new RestClientOptions(_config.BaseUrl)
            {
                Authenticator = new HttpBasicAuthenticator(_config.Username, _config.ApiToken),
                ThrowOnAnyError = false,
                MaxTimeout = 30000
            };

            _client = new RestClient(options);
            
            Console.WriteLine($"[Xray] Client initialized for {_config.BaseUrl}");
        }

        /// <summary>
        /// Import test execution results to Xray
        /// </summary>
        public async Task<XrayImportResponse> ImportTestExecutionAsync(XrayTestExecution execution)
        {
            if (!_config.Enabled)
            {
                return new XrayImportResponse 
                { 
                    Success = false, 
                    Message = "Xray integration is disabled" 
                };
            }

            try
            {
                Console.WriteLine($"[Xray] Importing test execution: {execution.Summary}");

                // Build Xray JSON format
                var xrayJson = BuildXrayImportJson(execution);

                // Create request
                var request = new RestRequest("/rest/raven/1.0/import/execution", Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(xrayJson);

                // Execute request
                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    Console.WriteLine($"[Xray] ? Successfully imported test execution");
                    
                    // Parse response
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        response.Content ?? "{}");
                    
                    return new XrayImportResponse
                    {
                        Success = true,
                        TestExecIssue = result?.ContainsKey("testExecIssue") == true 
                            ? result["testExecIssue"].ToString() ?? "" 
                            : execution.Key,
                        Message = $"Test execution imported successfully"
                    };
                }
                else
                {
                    Console.WriteLine($"[Xray] ? Failed to import: {response.StatusCode} - {response.ErrorMessage}");
                    Console.WriteLine($"[Xray] Response: {response.Content}");
                    
                    return new XrayImportResponse
                    {
                        Success = false,
                        Message = $"Failed to import: {response.StatusCode} - {response.ErrorMessage}"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Xray] ? Exception: {ex.Message}");
                return new XrayImportResponse
                {
                    Success = false,
                    Message = $"Exception: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Import test execution results using multipart format (for attachments)
        /// </summary>
        public async Task<XrayImportResponse> ImportTestExecutionWithEvidenceAsync(
            XrayTestExecution execution,
            Dictionary<string, string> evidenceFiles)
        {
            if (!_config.Enabled)
            {
                return new XrayImportResponse 
                { 
                    Success = false, 
                    Message = "Xray integration is disabled" 
                };
            }

            try
            {
                Console.WriteLine($"[Xray] Importing test execution with {evidenceFiles.Count} evidence files");

                // Build Xray JSON
                var xrayJson = BuildXrayImportJson(execution);

                // Create multipart request
                var request = new RestRequest("/rest/raven/1.0/import/execution/multipart", Method.Post);
                
                // Add JSON info
                request.AddParameter("info", JsonSerializer.Serialize(xrayJson), ParameterType.GetOrPost);

                // Add evidence files
                foreach (var evidence in evidenceFiles)
                {
                    if (File.Exists(evidence.Value))
                    {
                        var fileBytes = await File.ReadAllBytesAsync(evidence.Value);
                        request.AddFile(evidence.Key, fileBytes, Path.GetFileName(evidence.Value));
                        Console.WriteLine($"[Xray] Added evidence: {evidence.Key} -> {Path.GetFileName(evidence.Value)}");
                    }
                }

                // Execute request
                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    Console.WriteLine($"[Xray] ? Successfully imported test execution with evidence");
                    
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        response.Content ?? "{}");
                    
                    return new XrayImportResponse
                    {
                        Success = true,
                        TestExecIssue = result?.ContainsKey("testExecIssue") == true 
                            ? result["testExecIssue"].ToString() ?? "" 
                            : execution.Key,
                        Message = $"Test execution with evidence imported successfully"
                    };
                }
                else
                {
                    Console.WriteLine($"[Xray] ? Failed to import: {response.StatusCode} - {response.ErrorMessage}");
                    return new XrayImportResponse
                    {
                        Success = false,
                        Message = $"Failed to import: {response.StatusCode} - {response.ErrorMessage}"
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Xray] ? Exception: {ex.Message}");
                return new XrayImportResponse
                {
                    Success = false,
                    Message = $"Exception: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Create a new Test Execution issue in Jira/Xray
        /// </summary>
        public async Task<string?> CreateTestExecutionAsync(string summary, string? description = null, List<string>? testKeys = null)
        {
            if (!_config.Enabled)
            {
                Console.WriteLine("[Xray] Integration is disabled");
                return null;
            }

            try
            {
                Console.WriteLine($"[Xray] Creating test execution: {summary}");

                var issueData = new Dictionary<string, object>
                {
                    ["fields"] = new Dictionary<string, object>
                    {
                        ["project"] = new { key = _config.ProjectKey },
                        ["summary"] = summary,
                        ["description"] = description ?? $"Automated test execution created on {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                        ["issuetype"] = new { name = "Test Execution" }
                    }
                };

                var request = new RestRequest("/rest/api/2/issue", Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(issueData);

                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful && response.Content != null)
                {
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(response.Content);
                    var testExecKey = result?.ContainsKey("key") == true ? result["key"].ToString() : null;
                    
                    Console.WriteLine($"[Xray] ? Created Test Execution: {testExecKey}");

                    // Link tests if provided
                    if (testKeys != null && testKeys.Any() && testExecKey != null)
                    {
                        await LinkTestsToExecutionAsync(testExecKey, testKeys);
                    }

                    return testExecKey;
                }
                else
                {
                    Console.WriteLine($"[Xray] ? Failed to create: {response.StatusCode} - {response.ErrorMessage}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Xray] ? Exception: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Link tests to a Test Execution
        /// </summary>
        private async Task<bool> LinkTestsToExecutionAsync(string testExecKey, List<string> testKeys)
        {
            try
            {
                Console.WriteLine($"[Xray] Linking {testKeys.Count} tests to {testExecKey}");

                var linkData = new Dictionary<string, object>
                {
                    ["add"] = testKeys
                };

                var request = new RestRequest($"/rest/raven/1.0/api/testexec/{testExecKey}/test", Method.Post);
                request.AddHeader("Content-Type", "application/json");
                request.AddJsonBody(linkData);

                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    Console.WriteLine($"[Xray] ? Linked {testKeys.Count} tests successfully");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[Xray] ? Failed to link tests: {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Xray] ? Exception linking tests: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Build Xray import JSON format
        /// </summary>
        private object BuildXrayImportJson(XrayTestExecution execution)
        {
            var json = new Dictionary<string, object>();

            // Test Execution info
            var info = new Dictionary<string, object>
            {
                ["project"] = _config.ProjectKey,
                ["summary"] = execution.Summary,
                ["description"] = execution.Description
            };

            // Add Test Execution key if provided
            if (!string.IsNullOrEmpty(execution.Key))
            {
                info["testExecutionKey"] = execution.Key;
            }
            else if (!string.IsNullOrEmpty(_config.TestExecutionKey))
            {
                info["testExecutionKey"] = _config.TestExecutionKey;
            }

            // Add Test Plan if configured
            if (!string.IsNullOrEmpty(_config.TestPlanKey))
            {
                info["testPlanKey"] = _config.TestPlanKey;
            }

            json["info"] = info;

            // Test results
            var tests = execution.TestResults.Select(test => new Dictionary<string, object>
            {
                ["testKey"] = test.TestKey,
                ["status"] = test.Status,
                ["comment"] = test.Comment,
                ["start"] = test.Start?.ToString("yyyy-MM-ddTHH:mm:sszzz") ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                ["finish"] = test.Finish?.ToString("yyyy-MM-ddTHH:mm:sszzz") ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                ["evidences"] = test.Evidences.Select(e => new { data = e, filename = Path.GetFileName(e), contentType = GetContentType(e) }).ToList(),
                ["steps"] = test.Steps.Select(step => new Dictionary<string, object>
                {
                    ["status"] = step.Status,
                    ["comment"] = step.Comment,
                    ["evidences"] = step.Evidences.Select(e => new { data = e, filename = Path.GetFileName(e), contentType = GetContentType(e) }).ToList()
                }).ToList()
            }).ToList();

            json["tests"] = tests;

            return json;
        }

        /// <summary>
        /// Get content type based on file extension
        /// </summary>
        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Get Test Execution details
        /// </summary>
        public async Task<XrayTestExecution?> GetTestExecutionAsync(string testExecKey)
        {
            if (!_config.Enabled)
            {
                return null;
            }

            try
            {
                var request = new RestRequest($"/rest/raven/1.0/api/testexec/{testExecKey}", Method.Get);
                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful && response.Content != null)
                {
                    // Parse and return test execution details
                    // Implementation depends on Xray API response format
                    return new XrayTestExecution { Key = testExecKey };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Xray] Error getting test execution: {ex.Message}");
                return null;
            }
        }
    }
}
