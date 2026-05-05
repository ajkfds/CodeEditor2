using CodeEditor2.Data;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor.Parser
{
    public class CodeViewParser
    {
        public CodeViewParser(Views.CodeView codeView)
        {
            this.codeView = codeView;
        }

        private Views.CodeView codeView;

        public void EntryParse()
        {
            if (codeView.TextFile == null) return;
            TextFile textFile = codeView.TextFile;
            ParseWorker worker = new ParseWorker();

            System.Diagnostics.Debug.Print("### parserEntry "+textFile.RelativePath);
            // fire and forget
            Task.Run(async () => { await worker.Parse(textFile); });
        }
    }

}
