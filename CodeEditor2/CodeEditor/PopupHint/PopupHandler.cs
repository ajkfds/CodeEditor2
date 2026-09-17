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

        public void OpenPopup(List<PopupItem> popupItems)
        {
            if (codeView == null || codeView.Editor == null) return;
            if (popupItems == null || popupItems.Count == 0) return;

            // Calculate caret rectangle in editor (text area) coordinates.
            var caretRect = codeView._textEditor.TextArea.Caret.CalculateCaretRectangle();

            // Combine all popup items into a single PopupItem so that we can
            // reuse the existing ToolTip-based popup mechanism (PopupTextBlock).
            PopupItem combined = new PopupItem();
            foreach (PopupItem? item in popupItems)
            {
                if (item == null) continue;
                List<AjkAvaloniaLibs.Controls.ColorLabel.labelItem> itemLabels = item.GetItems();
                if (itemLabels == null) continue;
                foreach (var label in itemLabels)
                {
                    combined.GetItems().Add(label);
                }
                // Insert a newline between popup items so they appear on separate lines.
                combined.GetItems().Add(new AjkAvaloniaLibs.Controls.ColorLabel.labelNewLine());
            }
            combined.RemoveLastNewLine();

            // Update the ToolTip content (TextBlock shared with TextArea_PointerMoved).
            if (codeView.PopupTextBlock.Inlines == null) return;
            codeView.PopupTextBlock.Inlines.Clear();
            if (combined.ItemCount > 0)
            {
                combined.AppendToTextBlock(codeView.PopupTextBlock);
            }

            if (codeView.PopupTextBlock.Inlines.Count == 0)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }

            // Anchor the ToolTip to the caret position. The placement target
            // is codeView.Editor, so the offset is interpreted relative to it.
            ToolTip.SetPlacement(codeView.Editor, PlacementMode.BottomEdgeAlignedLeft);
            ToolTip.SetVerticalOffset(codeView.Editor, caretRect.Height);
            ToolTip.SetHorizontalOffset(codeView.Editor, 0);

            // Close once so that the popup repositions at the new caret location,
            // then open it.
            ToolTip.SetIsOpen(codeView.Editor, false);
            ToolTip.SetIsOpen(codeView.Editor, true);
        }

    }

}
