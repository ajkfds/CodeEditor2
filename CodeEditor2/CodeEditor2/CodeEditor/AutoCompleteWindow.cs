﻿using Avalonia.Input;
using AvaloniaEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor
{
    internal class AutoCompleteWindow : AvaloniaEdit.CodeCompletion.CompletionWindow
    {
        public AutoCompleteWindow(TextArea textArea) : base(textArea)
        {
        }

        //public new void OnKeyDown(KeyEventArgs e)
        //{
        //    base.OnKeyDown(e);
        //    if (!e.Handled)
        //    {
        //        CompletionList.HandleKey(e);
        //    }
        //}

    }
}
