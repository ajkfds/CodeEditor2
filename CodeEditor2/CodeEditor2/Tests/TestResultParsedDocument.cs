using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
