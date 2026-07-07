using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    /// <summary>
    /// .parseRule ファイルのTextFile表現
    /// </summary>
    public class ParseRuleFile : CodeEditor2.Data.TextFile
    {
        public static new async Task<TextFile> CreateAsync(string relativePath, Project project)
        {
            string name;
            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                name = relativePath;
            }
            ParseRuleFile fileItem = new ParseRuleFile()
            {
                Project = project,
                RelativePath = relativePath,
                Name = name
            };

            await fileItem.FileCheckAsync();
            if (fileItem.CodeDocument == null) System.Diagnostics.Debugger.Break();
            return fileItem;
        }

        public override async Task AcceptParsedDocumentAsync(CodeEditor.Parser.DocumentParser parser)
        {
            await base.AcceptParsedDocumentAsync(parser);
            Project.ParseRuleDefinition = new ParseRuleDefinition(Project);
            Project.ParseRule = new ParseRule(Project);
        }

        protected override CodeEditor2.NavigatePanel.NavigatePanelNode CreateNode()
        {
            return new CodeEditor2.NavigatePanel.TextFileNode(this);
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return new Parser.ParseRuleFileParser(this, parseMode, token);
        }
    }
}
