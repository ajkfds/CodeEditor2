using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CodeEditor2.Views;
using System.Collections.Generic;

namespace CodeEditor2.CodeEditor.PopupHint
{
    // Caret-anchored hint popup handler.
    //
    // Shows popup items (PopupItem) in a popup anchored to the caret
    // position. This popup is implemented with an Avalonia Popup control
    // that is independent from the ToolTip used by PopupHandler (the
    // mouse-over hint popup), so that the input-time hint popup and the
    // mouse-over popup can be displayed at the same time.
    public class HintPopupHandler
    {
       public HintPopupHandler(CodeView codeView)
       {
           this.codeView = codeView;
       }

       private CodeView codeView;

       // Combine the given PopupItems into a single PopupItem. Items are joined
       // by a newline so that each item appears on its own line.
       // Returns null if the resulting PopupItem would be empty.
       private static PopupItem? CombinePopupItems(IEnumerable<PopupItem?>? popupItems)
       {
           if (popupItems == null) return null;
           PopupItem combined = new PopupItem();
           bool any = false;
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
               any = true;
           }
           if (!any) return null;
           combined.RemoveLastNewLine();
           return combined;
       }

       // Update the content of the hint popup TextBlock.
       // Returns true if the text block has content.
       private bool UpdateHintTextBlock(PopupItem combined)
       {
           TextBlock? hintTextBlock = codeView.HintPopupTextBlock;
           if (hintTextBlock == null) return false;
           if (hintTextBlock.Inlines == null) return false;
           hintTextBlock.Inlines.Clear();
           if (combined.ItemCount > 0)
           {
               combined.AppendToTextBlock(hintTextBlock);
           }
           return hintTextBlock.Inlines.Count > 0;
       }

       // Calculate the position of the hint popup in editor (TextEditor)
       // coordinates. The popup is anchored just below the caret rectangle.
       // The offset is interpreted relative to the popup's placement target
       // (codeView.Editor).
       private bool GetCaretOffset(out Point offset)
       {
           offset = new Point(0, 0);
           if (codeView._textEditor == null) return false;

           Rect caretRect = codeView._textEditor.TextArea.Caret.CalculateCaretRectangle();
           if (caretRect == default) return false;

           // caretRect is in document coordinates (from the top of the
           // document), so subtract the scroll offset to get the viewport
           // (TextView-local) position, then translate it into the Editor
           // coordinate space before applying it as the popup offset.
           // (Same calculation as AvaloniaEdit CompletionWindowBase.)
           var textView = codeView._textEditor.TextArea.TextView;
           Vector scrollOffset = textView.ScrollOffset;
           Point caretViewportPos = new Point(
               caretRect.X - scrollOffset.X,
               caretRect.Y - scrollOffset.Y);
           Point? caretOriginInEditor = textView.TranslatePoint(caretViewportPos, codeView.Editor);
           if (caretOriginInEditor == null) return false;

           // Position the popup anchor at the bottom edge of the caret line so
           // that the popup (BottomRight gravity) appears right below the caret.
           offset = new Point(
               caretOriginInEditor.Value.X,
               caretOriginInEditor.Value.Y - caretRect.Height);
           return true;
       }

       public void OpenPopup(List<PopupItem> popupItems)
       {
           if (codeView == null || codeView.Editor == null) return;
           if (popupItems == null || popupItems.Count == 0) return;
           if (!Dispatcher.UIThread.CheckAccess())
           {
               Dispatcher.UIThread.Post(() => { OpenPopup(popupItems); });
               return;
           }

           Popup? hintPopup = codeView.HintPopup;
           if (hintPopup == null) return;

           // The popup is created in the CodeView constructor and is not part
           // of any visual / logical tree. Avalonia resolves the host TopLevel
           // (Window) of a Popup from its logical parent, so attach the
           // logical parent here (same approach as AvaloniaEdit
           // CompletionWindowBase.AttachEvents). Without this, setting
           // IsOpen = true shows nothing.
           TopLevel? topLevel = TopLevel.GetTopLevel(codeView.Editor);
           if (topLevel == null) return;
           ((ISetLogicalParent)hintPopup).SetParent(topLevel as ILogical);

           // Combine all popup items into a single PopupItem so that they can
           // be rendered as the content of the hint popup.
           PopupItem? combined = CombinePopupItems(popupItems);
           if (combined == null) return;

           // Update the hint popup content (TextBlock dedicated to the hint
           // popup, independent from PopupTextBlock used by the ToolTip).
           if (!UpdateHintTextBlock(combined)) return;

           // Anchor the popup to the caret position. The placement target
           // is codeView.Editor, so the offset is interpreted relative to it.
           if (!GetCaretOffset(out Point caretOffset)) return;
           hintPopup.HorizontalOffset = caretOffset.X;
            hintPopup.VerticalOffset = caretOffset.Y;
           hintPopup.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
           hintPopup.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;

           // Close once so that the popup repositions at the new caret
           // location, then open it.
           if (hintPopup.IsOpen) hintPopup.IsOpen = false;
           hintPopup.IsOpen = true;
       }

       public void ClosePopup()
       {
           if (codeView == null) return;
           if (!Dispatcher.UIThread.CheckAccess())
           {
               Dispatcher.UIThread.Post(() => { ClosePopup(); });
               return;
           }
           Popup? hintPopup = codeView.HintPopup;
           if (hintPopup == null) return;
           if (hintPopup.IsOpen) hintPopup.IsOpen = false;
       }
   }
}
