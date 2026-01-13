using System;
using System.Collections.Generic;

namespace PlaywrightTests.Integrations.Xray
{
    public class XrayConfiguration
    {
        public bool Enabled { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ApiToken { get; set; } = string.Empty;
        public string ProjectKey { get; set; } = string.Empty;
        public string TestExecutionKey { get; set; } = string.Empty;
        public string TestPlanKey { get; set; } = string.Empty;
        public bool AutoCreateTestExecution { get; set; }
        public string TestExecutionSummaryPrefix { get; set; } = "Automated Test Execution -";
    }

    public class XrayTestResult
    {
        public string TestKey { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;  // PASS, FAIL, TODO, EXECUTING, ABORTED
        public string Comment { get; set; } = string.Empty;
        public DateTime? Start { get; set; }
        public DateTime? Finish { get; set; }
        public List<string> Evidences { get; set; } = new List<string>();
        public List<XrayTestStep> Steps { get; set; } = new List<XrayTestStep>();
        public Dictionary<string, object> CustomFields { get; set; } = new Dictionary<string, object>();
    }

    public class XrayTestStep
    {
        public string Action { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string ExpectedResult { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;  // PASS, FAIL, TODO, EXECUTING, ABORTED
        public string Comment { get; set; } = string.Empty;
        public List<string> Evidences { get; set; } = new List<string>();
    }

    public class XrayTestExecution
    {
        public string Key { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> TestKeys { get; set; } = new List<string>();
        public List<XrayTestResult> TestResults { get; set; } = new List<XrayTestResult>();
    }

    public class XrayImportResponse
    {
        public string TestExecIssue { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
