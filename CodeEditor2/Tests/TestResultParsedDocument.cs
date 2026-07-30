using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.Data;

namespace CodeEditor2.Tests
{
    public class TestResultParsedDocument : ParsedDocument
    {
        public TestResultParsedDocument(TextFile textFile, string key, ulong version, DocumentParser.ParseModeEnum parseMode) : base(textFile, key, version, parseMode)
        {
        }

        public string TestName { get; set; }
        public string Hash { get; set; }
        public bool Passed { get; set; }
        public bool Failed { get; set; }
    }
}
