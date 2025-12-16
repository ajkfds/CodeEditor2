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

        private static Task? _currentTask;
        private static CancellationTokenSource? _cts;

        public void EntryParse()
        {
            if (codeView.TextFile == null) return;
            TextFile textFile = codeView.TextFile;

            // fire and forget
            Task.Run(async () => { await parse(textFile); });
        }

        private async Task parse(TextFile textFile)
        {
            if (_cts != null)
            {
                _cts.Cancel();

                try
                {
                    // wait completion of the previous task
                    //if (_currentTask != null) await _currentTask;
                }
                catch (OperationCanceledException) { }
            }

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            _currentTask = Task.Run(async () =>
            {
                await runParse(textFile, token);
            }, token);
            
            try
            {
                await _currentTask;
            }
            catch (OperationCanceledException)
            {
                _currentTask = null;
            }
            finally
            {
                _currentTask = null;
            }
            return;
        }

        private async Task runParse(TextFile textFile, System.Threading.CancellationToken token)
        {
            DocumentParser? parser = textFile?.CreateDocumentParser(DocumentParser.ParseModeEnum.EditParse, token);
            if (parser == null) return;
            await parser.ParseAsync();
            if (parser.ParsedDocument == null) return;

            Data.TextFile targetTextFile = parser.TextFile;
            CodeDocument? targetCodeDocument = targetTextFile.CodeDocument;

            if (targetTextFile == null || targetCodeDocument == null)
            {
                parser.Dispose();
                return;
            }

            Controller.AppendLog("complete edit parse ID :" + parser.TextFile.ID);

            await Dispatcher.UIThread.InvokeAsync(
                () =>
                {
                    // If the version of the parsed document is already outdated, discard the parse result.
                    if (targetCodeDocument.Version != parser.ParsedDocument.Version)
                    {
                        Controller.AppendLog("edit parsed mismatch " + DateTime.Now.ToString() + "ver" + targetCodeDocument.Version + "<-" + parser.ParsedDocument.Version);
                        parser.Dispose();
                        return;
                    }
                    parser.TextFile.AcceptParsedDocument(parser.ParsedDocument);

                    Data.ITextFile? currentTextFile = Controller.CodeEditor.GetTextFile();
                    targetCodeDocument.CopyColorMarkFrom(parser.Document);

                    if (currentTextFile == null || currentTextFile != targetTextFile)
                    {
                        return;
                    }

                    // update current view
                    Controller.CodeEditor.Refresh();
                    Controller.MessageView.Update(parser.ParsedDocument);
                }
            );

        }

    }

}
