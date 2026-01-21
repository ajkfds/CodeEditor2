using Avalonia.Styling;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor.Parser;

namespace CodeEditor2.Parser
{
    public class FileClassifyParser : DocumentParser
    {
        [SetsRequiredMembers]
        public FileClassifyParser(Data.FileClassifyFile file, DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token) : base(file, parseMode,token)
        {
            this.Document = new CodeEditor2.CodeEditor.CodeDocument(file); // use verilog codeDocument
            CodeDocument? document = file.CodeDocument;
            this.Document.CopyTextOnlyFrom(document);
            this.ParseMode = parseMode;
            this.TextFile = file as CodeEditor2.Data.TextFile;

            ParsedDocument = new CodeEditor2.CodeEditor.ParsedDocument(file,file.RelativePath, file.CodeDocument.Version, parseMode);
        }
        public static class Style
        {
            public enum Color : byte
            {
                Normal = 0,
                Header = 5,
                Register = 3,
                Net = 9,
                Paramater = 7,
                Keyword = 4,
                Identifier = 6,
                Number = 8
            }
        }

        public override async Task ParseAsync()
        {
            for (int line = 1; line < Document.Lines; line++)
            {
                string lineText = Document.CreateString(Document.GetLineStartIndex(line), Document.GetLineLength(line));
                if (lineText.StartsWith("#"))
                {
                    colorLine(Style.Color.Header, line);
                }
                else if (lineText.StartsWith("+"))
                {
                    colorLine(Style.Color.Identifier, line);
                }
                else if (lineText.StartsWith("-"))
                {
                    colorLine(Style.Color.Register, line);
                }
            }
        }

        private void colorLine(Style.Color color, int line)
        {
            int start = Document.GetLineStartIndex(line);
            int end = start + Document.GetLineLength(line);
            for (int i = start; i < end; i++)
            {
                Document.TextColors.SetColorAt(i, (byte)color);
            }
        }
    }
}
