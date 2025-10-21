using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Tools
{
    internal class ParseProjectUnit
    {
        public ParseProjectUnit()
        {
        }


        public async Task Run(System.Collections.Concurrent.BlockingCollection<Data.TextFile> files, ProgressWindow progressWindow)
        {
            foreach (Data.TextFile file in files.GetConsumingEnumerable())
            {
                await parse(file,progressWindow);
            }
        }

        private async Task parse(Data.TextFile textFile, ProgressWindow progressWindow)
        {
            DocumentParser? parser = textFile.CreateDocumentParser(DocumentParser.ParseModeEnum.LoadParse,null);
            if (parser == null) return;

            parser.Document._tag = "TextParserTask:"+textFile.Name;
            parser.Parse();
            if (parser.ParsedDocument == null) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                textFile.AcceptParsedDocument(parser.ParsedDocument);
            });

            Dispatcher.UIThread.Invoke(
                () => {
                    if(progressWindow.ProgressMaxValue> progressWindow.ProgressValue+1) progressWindow.ProgressValue++;
                    progressWindow.Message = textFile.Name;
                }
            );

            //            textFile.Close();
            textFile.ReparseRequested = true;
        }
    }
}
