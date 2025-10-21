using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor.Parser
{
    public class DocumentParser : IDisposable
    {
        [SetsRequiredMembers]
        public DocumentParser(Data.TextFile textFile, ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            Document = new CodeDocument(textFile);
            Document.CopyTextOnlyFrom(textFile.CodeDocument);
            ParseMode = parseMode;
            TextFile = textFile;
        }

        public void Dispose()
        {
//            Document = null;
        }

        public required Data.TextFile TextFile { get; init; }
        public required ParseModeEnum ParseMode { get; init; }
        public required CodeDocument Document
        {
            get;
            set;
        }

        public enum ParseModeEnum
        {
            LoadParse,
            BackgroundParse,
            ActivatedParse,
            EditParse,
            PostEditParse
        }

        public virtual void Parse()
        {
        }

        public virtual ParsedDocument? ParsedDocument { get; protected set; } = null;
    }
}
