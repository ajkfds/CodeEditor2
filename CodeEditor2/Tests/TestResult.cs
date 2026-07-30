namespace CodeEditor2.Tests
{
    public class TestResult
    {
        public enum TestStatus
        {
            NoResult = 0,
            Passed = 1,
            OldPassed = 2,
            Failed = 3,
            OldFailed = 4
        }

        public TestStatus Status { get; set; }
    }
}
