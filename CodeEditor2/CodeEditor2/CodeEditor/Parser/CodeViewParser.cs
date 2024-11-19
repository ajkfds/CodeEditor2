using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor.Parser
{
    public class CodeViewParser
    {
        public CodeViewParser(Views.CodeView codeView)
        {
            this.codeView = codeView;

            backGroundParser.Run();
        }

        private Views.CodeView codeView;
        public BackgroundParser backGroundParser = new BackgroundParser();

        public void Timer_Tick(object? sender, EventArgs e)
        {
            // update background parser result
            DocumentParser parser = backGroundParser.GetResult();
            if (parser == null) return;
            if (parser.ParsedDocument == null) return;

            // parser.Text <- parsedDodument,Document update

            Data.TextFile targetTextFile = parser.TextFile;
            CodeDocument targetCodeDocument = targetTextFile.CodeDocument;

            if (targetTextFile == null)
            {
                parser.Dispose();
                return;
            }

            Controller.AppendLog("complete edit parse ID :" + parser.TextFile.ID);

            // If the version of the parsed document is already outdated, discard the parse result.
            if (targetCodeDocument.Version != parser.ParsedDocument.Version)
            {
                Controller.AppendLog("edit parsed mismatch " + DateTime.Now.ToString() + "ver" + targetCodeDocument.Version + "<-" + parser.ParsedDocument.Version);
                parser.Dispose();
                return;
            }

            if (parser.ParsedDocument != null)
            {
                parser.TextFile.AcceptParsedDocument(parser.ParsedDocument);
                System.Diagnostics.Debug.Print("# BackgroundParser.AcceptParsedDocument " + parser.TextFile.ID);
            }

            Data.ITextFile? currentTextFile = Controller.CodeEditor.GetTextFile();
            targetCodeDocument.CopyColorMarkFrom(parser.Document);

            if (currentTextFile == null || currentTextFile != targetTextFile)
            {
                return;
            }

            //targetCodeDocument.CopyColorMarkFrom(parser.Document);
            //Controller.MessageView.Update(currentTextFile.ParsedDocument);

            // update current view
            Controller.CodeEditor.Refresh();
            Controller.MessageView.Update(parser.ParsedDocument);
        }


        public void EntryParse()
        {
            //            if (Global.StopParse) return;
            if (codeView.TextFile == null) return;
            Controller.AppendLog("### CodeViewParser.EntryParse ID :" + codeView.TextFile.ID + " " + DateTime.Now.ToShortTimeString());
            backGroundParser.EntryParse(codeView.TextFile);
        }


    }

}
