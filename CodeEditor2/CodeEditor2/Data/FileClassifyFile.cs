using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    public class FileClassifyFile : CodeEditor2.Data.TextFile
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
            FileClassifyFile fileItem = new FileClassifyFile()
            {
                Project = project,
                RelativePath = relativePath,
                Name = name
            };

            await fileItem.FileCheck();
            if (fileItem.CodeDocument == null) System.Diagnostics.Debugger.Break();
            return fileItem;
        }

        public override async Task AcceptParsedDocumentAsync(CodeEditor.Parser.DocumentParser parser)
        {
            await base.AcceptParsedDocumentAsync(parser);
            Project.FileClassify = new FileClassify(Project);
        }



        protected override CodeEditor2.NavigatePanel.NavigatePanelNode CreateNode()
        {
            return new CodeEditor2.NavigatePanel.TextFileNode(this);
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return new Parser.FileClassifyParser(this, parseMode, token);
        }


    }
}
