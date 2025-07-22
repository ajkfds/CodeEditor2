using Avalonia.Controls;
using Avalonia.Input;
using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Rendering;

namespace CodeEditor2.CodeEditor.PopupHint
{
    public class PopupHandler
    {
        // Popup-Hint Handler
        //
        // show mouse over popup-hinting
        public PopupHandler(CodeView codeView)
        {
            this.codeView = codeView;
        }

        CodeView codeView;

        private int popupInex = -1;
        public void TextArea_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (codeView.CodeDocument == null || codeView.TextFile == null)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }
            Avalonia.Point point = e.GetPosition(codeView._textEditor.TextArea);
            var pos = codeView._textEditor.GetPositionFromPoint(point);
            if (pos == null)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }

            TextViewPosition tpos = (TextViewPosition)pos;
            if (tpos.Line > codeView.CodeDocument.Lines)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }
            int index = codeView.CodeDocument.TextDocument.GetOffset(tpos.Line, tpos.Column);
            if (index >= codeView.CodeDocument.Length)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }

            int headIndex, length;
            codeView.CodeDocument.GetWord(index, out headIndex, out length);

            if (popupInex != headIndex) // close once to move popup position
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
            }
            popupInex = headIndex;
            if (codeView.PopupTextBlock.Inlines == null) throw new Exception();

            codeView.PopupTextBlock.Inlines.Clear();
//            CodeEditor2.CodeEditor.TextBlockMessages messages = new TextBlockMessages(codeView.PopupTextBlock);
            PopupItem? popupItem = codeView.TextFile.GetPopupItem(codeView.CodeDocument.Version, index);
            if (popupItem != null && popupItem.ItemCount > 0)
            {
                popupItem.RemoveLastNewLine();
                popupItem.AppendToTextBlock(codeView.PopupTextBlock);
            }


            if (codeView.PopupTextBlock.Inlines.Count == 0)//pItem == null || pItem.Inlines == null || pItem.Inlines.Count == 0)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }


            //.Inlines.Clear();
            //codeView.PopupColorLabel.Add(pItem);

            //            ToolTip.SetIsOpen(codeView.Editor, false); // close once to update pop-up window position
            //if (pItem.GetItems().Count != 0)
            {
                //                ToolTip.SetIsOpen(codeView.Editor, false);
                ToolTip.SetIsOpen(codeView.Editor, true);
            }
        }


    }

}
