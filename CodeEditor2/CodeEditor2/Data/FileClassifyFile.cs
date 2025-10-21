using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    public class FileClassifyFile : CodeEditor2.Data.TextFile
    {
        public static new FileClassifyFile Create(string relativePath, CodeEditor2.Data.Project project)
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

            return fileItem;
        }

        public override void AcceptParsedDocument(ParsedDocument newParsedDocument)
        {
            base.AcceptParsedDocument(newParsedDocument);
            Project.FileClassify = new FileClassify(Project);
        }



        protected override CodeEditor2.NavigatePanel.NavigatePanelNode CreateNode()
        {
            return new CodeEditor2.NavigatePanel.TextFileNode(this);
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode,System.Threading.CancellationToken? token)
        {
            return new Parser.FileClassifyParser(this, parseMode,token);
        }


    }
}
