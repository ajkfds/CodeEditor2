using CodeEditor2.CodeEditor;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    /// <summary>
    /// Reference to a position on a text file
    /// </summary>
    public class TextReference
    {
        public TextReference(ITextFile textFile, int startIndex, int length)
        {
            textFileRef = new WeakReference<ITextFile>(textFile);
            StartIndex = startIndex;
            Length = length;
            Caption = textFile.Name;
            CodeDocument? document = textFile.CodeDocument;
            if(document != null)
            {
                int line = document.GetLineAt(startIndex);
                Caption = Caption + " : line" + line.ToString();
            }
        }

        public string Caption { get; init; }
        public int StartIndex { get; init; }
        public int Length { get; init; }

        private WeakReference<ITextFile> textFileRef { get; init; }
        public ITextFile? TextFile { 
            get {
                if (!textFileRef.TryGetTarget(out ITextFile? textFile)) return null;
                return textFile;
            } 
        }
    }
}
