using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor
{
    public class CodeViewParser
    {
        public CodeViewParser(Views.CodeView codeView)
        {
            this.codeView = codeView;

            backGroundParser.Run();
        }

        private Views.CodeView codeView;
        public BackroungParser backGroundParser = new BackroungParser();

        public void Timer_Tick(object? sender, EventArgs e)
        {
            DocumentParser parser = backGroundParser.GetResult();
            if (parser == null) return;
            if (parser.ParsedDocument == null) return;
            //if (TextFile == null) return;
            //if (TextFile != parser.TextFile)
            //{   // return not current file
            //    return;
            //}

            Data.TextFile textFile = parser.TextFile;
            CodeDocument codeDocument = textFile.CodeDocument;

            if (textFile == null || textFile == null)
            {
                parser.Dispose();
                return;
            }

            Controller.AppendLog("complete edit parse ID :" + parser.TextFile.ID);
            if (codeDocument.Version != parser.ParsedDocument.Version)
            {
                Controller.AppendLog("edit parsed mismatch " + DateTime.Now.ToString() + "ver" + codeDocument.Version + "<-" + parser.ParsedDocument.Version);
                parser.Dispose();
                return;
            }

            //            CodeDocument.CopyFrom(parser.Document);
            codeDocument.CopyColorMarkFrom(parser.Document);

            if (parser.ParsedDocument != null)
            {
                parser.TextFile.AcceptParsedDocument(parser.ParsedDocument);
            }

            // update current view
            codeView._textEditor.TextArea.TextView.Redraw();
            Controller.MessageView.Update(codeView.TextFile.ParsedDocument);
        }

        public void EntryParse()
        {
            //            if (Global.StopParse) return;
            if (codeView.TextFile == null) return;
            Controller.AppendLog("entry edit parse ID :" + codeView.TextFile.ID);
            backGroundParser.EntryParse(codeView.TextFile);
        }


    }

}
