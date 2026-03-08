using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Parser
{
    public class YamlParsedDocument : ParsedDocument
    {
        public YamlParsedDocument(TextFile textFile, string key, ulong version, DocumentParser.ParseModeEnum parseMode) : base(textFile, key, version, parseMode)
        {
        }
        public object ParsedObject { get; set; }
    }
}
