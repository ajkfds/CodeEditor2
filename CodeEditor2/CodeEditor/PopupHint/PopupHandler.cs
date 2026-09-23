using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using CodeEditor2.Views;
using System.Collections.Generic;
using System;

namespace CodeEditor2.CodeEditor.PopupHint
{
    public class PopupHandler
    {
        // Popup-Hint Handler (mouse-over popup)
        //
        // Shows the mouse-over popup hint via a ToolTip anchored to the
        // pointer. The input-time hint popup (caret-anchored) is handled
        // separately by HintPopupHandler with its own dedicated Popup, so
        // both popups can be displayed at the same time.
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

            // Mouse-over hint popup: use the lightweight GetPopupItem API so that
            // we do not trigger heavy parsing on every pointer-move event.
            PopupItem? popupItem = codeView.TextFile.GetPopupItem(codeView.CodeDocument.Version, index);
            if (popupItem != null && popupItem.ItemCount > 0)
            {
                popupItem.RemoveLastNewLine();
                popupItem.AppendToTextBlock(codeView.PopupTextBlock);
            }

            if (codeView.PopupTextBlock.Inlines.Count == 0)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }

            // Mouse hover: anchor the ToolTip to the pointer (default placement).
            ToolTip.SetPlacement(codeView.Editor, PlacementMode.Pointer);
            ToolTip.SetHorizontalOffset(codeView.Editor, 0);
            ToolTip.SetVerticalOffset(codeView.Editor, 0);
            ToolTip.SetIsOpen(codeView.Editor, true);
        }

    }

}
