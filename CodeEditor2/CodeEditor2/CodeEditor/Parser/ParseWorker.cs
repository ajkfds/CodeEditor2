using Avalonia.Threading;
using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor.Parser
{
    public class ParseWorker
    {
        private static Task? _currentTask;
        private static CancellationTokenSource? _cts;

        public async Task Parse(Data.TextFile textFile)
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

        private async Task runParse(Data.TextFile textFile, System.Threading.CancellationToken token)
        {
            await runSingleParse(textFile, token);
            token.ThrowIfCancellationRequested();

            if (textFile == null) return;
            List<Data.Item> items = textFile.Items.Values.ToList();
            foreach (Data.Item item in items)
            {
                token.ThrowIfCancellationRequested();
                if (item is not Data.TextFile) continue;
                Data.TextFile subFile = (Data.TextFile)item;
                if (!subFile.ReparseRequested) continue;
                await runSingleParse(subFile, token);

                token.ThrowIfCancellationRequested();
            }
        }

        private async Task runSingleParse(Data.TextFile textFile, System.Threading.CancellationToken token)
        {
            DocumentParser? parser = textFile?.CreateDocumentParser(DocumentParser.ParseModeEnum.EditParse, token);
            if (parser == null) return;
            await parser.ParseAsync();
            if (parser.ParsedDocument == null) return;

            Data.TextFile targetTextFile = parser.TextFile;
            CodeDocument targetCodeDocument = await targetTextFile.GetCodeDocumentAsync();

            if (targetTextFile == null)
            {
                parser.Dispose();
                return;
            }

            token.ThrowIfCancellationRequested();

            Controller.AppendLog("complete edit parse ID :" + parser.TextFile.ID);

            // If the version of the parsed document is already outdated, discard the parse result.
            if (targetCodeDocument.Version != parser.ParsedDocument.Version)
            {
                Controller.AppendLog("edit parsed mismatch " + DateTime.Now.ToString() + "ver" + targetCodeDocument.Version + "<-" + parser.ParsedDocument.Version);
                parser.Dispose();
                return;
            }

            Data.ITextFile? currentTextFile = null;
            await Dispatcher.UIThread.InvokeAsync(
                async () => {
                    await parser.TextFile.AcceptParsedDocumentAsync(parser.ParsedDocument);
                    currentTextFile = Controller.CodeEditor.GetTextFile();
                });


            targetCodeDocument.CopyColorMarkFrom(parser.Document);

            if (currentTextFile == null || currentTextFile != targetTextFile)
            {
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(
                () =>
                {
                    // update current view
                    Controller.CodeEditor.Refresh();
                    Controller.MessageView.Update(parser.ParsedDocument);
                }
            );
        }

    }
}
