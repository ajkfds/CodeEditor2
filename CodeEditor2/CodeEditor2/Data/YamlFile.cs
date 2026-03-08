using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    public class YamlFile : CodeEditor2.Data.TextFile
    {
        public static async Task<TextFile> CreateAsync(string relativePath, Project project)
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
            YamlFile fileItem = new YamlFile()
            {
                Project = project,
                RelativePath = relativePath,
                Name = name
            };

            await fileItem.FileCheck();
            if (fileItem.document == null) System.Diagnostics.Debugger.Break();
            return fileItem;
        }

        public static Action<YamlFile>? AcceptCustomYamlParsedDocument;
        public override async Task AcceptParsedDocumentAsync(ParsedDocument newParsedDocument)
        {
            await base.AcceptParsedDocumentAsync(newParsedDocument);

            if(AcceptCustomYamlParsedDocument != null) AcceptCustomYamlParsedDocument.Invoke(this);
        }



        protected override CodeEditor2.NavigatePanel.NavigatePanelNode CreateNode()
        {
            return new CodeEditor2.NavigatePanel.TextFileNode(this);
        }

        public override DocumentParser CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return new Parser.YamlParser(this, parseMode, token);
        }

    }
}
