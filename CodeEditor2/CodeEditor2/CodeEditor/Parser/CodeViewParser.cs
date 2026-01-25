using Avalonia.Remote.Protocol;
using Avalonia.Threading;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        private ParseWorker worker = new ParseWorker();

        public void EntryParse()
        {
            if (codeView.TextFile == null) return;
            TextFile textFile = codeView.TextFile;

            // fire and forget
            Task.Run(async () => { await worker.Parse(textFile); });
        }
    }

}
