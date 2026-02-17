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

        public void EntryParse()
        {
            if (codeView.TextFile == null) return;
            TextFile textFile = codeView.TextFile;
            ParseWorker worker = new ParseWorker();

            // fire and forget
            Task.Run(async () => { await worker.Parse(textFile); });
        }
    }

}
