using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.Data;

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
