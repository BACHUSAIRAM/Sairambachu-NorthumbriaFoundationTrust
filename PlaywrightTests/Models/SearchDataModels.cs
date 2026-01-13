using System.Collections.Generic;

namespace PlaywrightTests.Models
{
    public class SearchDataCase
    {
        public string Term { get; set; } = string.Empty;
        public bool ExpectResults { get; set; } = true;
        public string? ExpectedMessage { get; set; }
    }

    public class SearchCaseExecutionResult
    {
        public string Term { get; set; } = string.Empty;
        public bool ExpectResults { get; set; }
        public bool ActualResults { get; set; }
        public bool MessageDisplayed { get; set; }
        public string? ExpectedMessage { get; set; }
        public string? Notes { get; set; }
        public bool Passed { get; set; }
    }
}
