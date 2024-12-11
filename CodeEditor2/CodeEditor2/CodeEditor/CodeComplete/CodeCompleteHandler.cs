using AjkAvaloniaLibs;
using Avalonia;
using Avalonia.Input;
using CodeEditor2.Views;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor.CodeComplete
{
    public class CodeCompleteHandler
    {
        public CodeCompleteHandler(CodeView codeView)
        {
            this.codeView = codeView;
        }

        private CodeView codeView;
        //        private AutoCompleteWindow? _completionWindow;
        private PopupMenuView? popupMenuView = null;
        private bool working = false;

        private bool forceOpened = false;

        public void Close()
        {
            if (popupMenuView == null) return;
            popupMenuView.Cancel();
            working = false;
        }

        public void KeyDown(object? sender, KeyEventArgs e)
        {
            if (!working) return;
            if (codeView.TextFile == null) return;
            if (codeView.CodeDocument == null) return;
            if (popupMenuView == null) return;

            if (e.Key == Key.Up)
            {
                popupMenuView.SelectUp();
                e.Handled = true;
            }else if(e.Key == Key.Down)
            {
                popupMenuView.SelectDown();
                e.Handled = true;
            }
            else if(e.Key == Key.Enter)
            {
                Apply();
                Close();
                e.Handled = true;
                return;
            }else if(e.Key == Key.Tab | e.Key == Key.Space)
            {
                Apply();
                Close();
                return;
            }else if( e.Key == Key.Escape)
            {
                Close();
                return;
            }
        }

        public void Apply()
        {
            if (codeView.TextFile == null) return;
            if (codeView.CodeDocument == null) return;
            if (popupMenuView == null) return;

            PopupMenu.PopupMenuItem? popupMenuItem = popupMenuView.GetSlectedItem();
            if (popupMenuItem == null) return;
            popupMenuItem.OnSelected();

        }

        public void TextEntered(object? sender, TextInputEventArgs e)
        {
            System.Diagnostics.Debug.Print("### TextEnteted "+working.ToString());
            if (codeView.TextFile == null) return;
            if (codeView.CodeDocument == null) return;

            char? prevChar = null;  // character before caret
            int prevIndex = codeView._textEditor.CaretOffset;
            if (prevIndex != 0)
            {
                prevIndex--;
                prevChar = codeView.CodeDocument.GetCharAt(prevIndex);
            }
            System.Diagnostics.Debug.Print("### TextEnteted 1");

            string? candidateWord;
            List<AutocompleteItem>? items = codeView.TextFile.GetAutoCompleteItems(codeView._textEditor.CaretOffset, out candidateWord);
            if (items == null || candidateWord == null)
            {
                Close();
                return;
            }
            if (candidateWord == "" & prevChar != '.')
            {
                Close();
                return;
            }
            System.Diagnostics.Debug.Print("### TextEnteted 2");

            List<PopupMenu.ToolItem> toolItems = new List<PopupMenu.ToolItem>();
            foreach(AutocompleteItem aItem in items)
            {
                if(candidateWord.Length<1 || aItem.Text.StartsWith(candidateWord))
                {
                    aItem.Assign(codeView.CodeDocument);
                    toolItems.Add(aItem);
                }
            }
            System.Diagnostics.Debug.Print("### TextEnteted 3");

            if (toolItems.Count == 0)
            {
                Close();
                return;
            }
            System.Diagnostics.Debug.Print("### TextEnteted 4");

            if (!working)
            {
                System.Diagnostics.Debug.Print("### TextEnteted 5");
                popupMenuView = Controller.CodeEditor.OpenAutoComplete(toolItems);
                if (popupMenuView == null)
                {
                    Close();
                    working = false;
                    return;
                }
                popupMenuView.SelectDown();
                working = true;
            }
            else
            {
                System.Diagnostics.Debug.Print("### TextEnteted 6");
                if (popupMenuView == null)
                {
                    Close();
                    return;
                }
                System.Diagnostics.Debug.Print("### TextEnteted 7 "+toolItems.Count);
                Controller.CodeEditor.UpdateAutoComplete(toolItems);
                popupMenuView.SelectDown();
                working = true;
            }
        }

        public void UpdateMenu()
        {

        }



        public void ForceOpenAutoComplete(List<AutocompleteItem> autocompleteItems)
        {
        }


    }
}
