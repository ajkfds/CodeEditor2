﻿using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using System.Net.Http.Headers;
using CodeEditor2.Snippets;

namespace CodeEditor2.CodeEditor.PopupMenu
{
    public class PopupMenuHandler
    {
        // popup menu for in-line menu select

        public PopupMenuHandler(CodeView codeView)
        {
            this.codeView = codeView;
        }

        CodeView codeView;

        // tool selection form /////////////////////////////////////////////////////////////////////////

        public List<PopupMenuItem> PopupMenuItems = new List<PopupMenuItem>();

        public void OpenCustomSelection(List<ToolItem> candidates)
        {
            System.Diagnostics.Debug.Print("## OpenCustomSelection");
            PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            if (flyout == null) return;
            if (flyout.IsOpen) return;
            if (codeView.TextFile == null) return;

            TransformedBounds? tBound = Global.codeView.Editor.GetTransformedBounds();
            if (tBound == null) return;
            TransformedBounds transformedBound = (TransformedBounds)tBound;
            var caretRect = codeView._textEditor.TextArea.Caret.CalculateCaretRectangle();


            Avalonia.Point position = transformedBound.Clip.Position;

            //Avalonia.PixelPoint screenPosition = new Avalonia.PixelPoint(
            //    Global.mainWindow.Position.X + (int)(transformedBound.Clip.Position.X * Global.mainWindow.DesktopScaling),
            //    Global.mainWindow.Position.Y + (int)(transformedBound.Clip.Position.Y * Global.mainWindow.DesktopScaling)
            //    );

            PopupMenuItems.Clear();
            foreach (ToolItem item in candidates) { PopupMenuItems.Add(item.CreatePopupMenuItem()); }

            flyout.Placement = PlacementMode.AnchorAndGravity;
            flyout.VerticalOffset = caretRect.Top;
            flyout.HorizontalOffset = caretRect.Left;
            flyout.PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight;
            flyout.PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft;

            flyout.ShowAt(codeView._textEditor); // = FlyoutBase.ShowAttachedFlyout(_textEditor);
        }

        public void ShowToolSelectionPopupMenu()
        {
            if (Snippet != null)
            {
                Controller.CodeEditor.AbortInteractiveSnippet();
            }

            System.Diagnostics.Debug.Print("## ShowToolSelectionPopupMenu");
            PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            if (flyout == null) return;
            if (flyout.IsOpen) return;
            if (codeView.TextFile == null) return;


            List<ToolItem> tools = codeView.TextFile.GetToolItems(codeView.CodeDocument.CaretIndex);
            //items.Add(new Snippets.ToLower());
            if (tools == null)
            {
                tools = new List<ToolItem>();
            }
            tools.Add(new ToUpper());
            tools.Add(new ToLower());

            if (tools.Count == 0)
            {
                HidePopupMenu();
                return;
            }

            //TransformedBounds? tBound = Global.codeView.Editor.GetTransformedBounds();
            //if (tBound == null) return;
            //TransformedBounds transformedBound = (TransformedBounds)tBound;
            var caretRect = codeView._textEditor.TextArea.Caret.CalculateCaretRectangle();


            Avalonia.Point position = caretRect.Position;// transformedBound.Clip.Position;
            Avalonia.Vector scroll = codeView._textEditor.TextArea.TextView.ScrollOffset;
            //Avalonia.PixelPoint screenPosition = new Avalonia.PixelPoint(
            //    Global.mainWindow.Position.X + (int)(position.X * Global.mainWindow.DesktopScaling),
            //    Global.mainWindow.Position.Y + (int)((position.Y-scroll.Y) * Global.mainWindow.DesktopScaling)
            //    );

            PopupMenuItems.Clear();
            foreach (ToolItem item in tools) { PopupMenuItems.Add(item.CreatePopupMenuItem()); }

            flyout.Placement = PlacementMode.AnchorAndGravity;
            flyout.VerticalOffset = position.Y - scroll.Y;
            flyout.HorizontalOffset = position.X - scroll.X;
            flyout.PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight;
            flyout.PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft;

            flyout.ShowAt(codeView._textEditor); // = FlyoutBase.ShowAttachedFlyout(_textEditor);
        }


        public bool IsOpened
        {
            get
            {
                PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
                if (flyout == null) return false;
                if (!flyout.IsOpen) return false;
                return true;
            }
        }

        public void HidePopupMenu()
        {
            System.Diagnostics.Debug.Print("## HidePopupMenu");
            PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            if (flyout == null) return;
            if (!flyout.IsOpen) return;
            flyout.Hide();
        }

        public void PopupMenu_Selected(PopupMenuItem popUpMenuItem)
        {
            System.Diagnostics.Debug.Print("## PopupMenu_Selected");

            //            ToolItem? toolItem = popUpMenuItem as ToolItem;
            //if (popUpMenuItem is ToolItem)
            //{
            //    if (codeView.CodeDocument == null) return;
            //    (popUpMenuItem as ToolItem)?.Apply(codeView.CodeDocument);

            //    //InteractiveSnippet? snippet = popUpMenuItem as InteractiveSnippet;
            //    //if (snippet == null) return;
            //    //Snippet = snippet;
            //}
            //else
            //{
            popUpMenuItem.OnSelected();
            //}
        }

        public InteractiveSnippet Snippet = null;

        public void StartInteractiveSnippet(InteractiveSnippet interactiveSnippet)
        {
            System.Diagnostics.Debug.Print("## CodeViewSetupMenu.StartInteractiveSnippet");
            AbortInteractiveSnippet();
            Snippet = interactiveSnippet;
        }

        public void AbortInteractiveSnippet()
        {
            if (Snippet == null) return;
            System.Diagnostics.Debug.Print("## CodeViewSetupMenu.AbortInteractiveSnippet for snippet");
            Snippet.Aborted();
            Snippet = null;
        }

        public void TextArea_KeyDown(object? sender, KeyEventArgs e)
        {
            if (Snippet == null) return;
            System.Diagnostics.Debug.Print("## TextArea_KeyDown for snippet");
            Snippet.KeyDown(sender, e, codeView.PopupMenu);
        }
        public void TextEntering(object? sender, TextInputEventArgs e)
        {
            if (Snippet == null) return;
            System.Diagnostics.Debug.Print("## TextEntering for snippet");
            Snippet.BeforeKeyDown(sender, e, codeView.PopupMenu);
        }

        public void TextEntered(object? sender, TextInputEventArgs e)
        {
            if (Snippet == null) return;
            System.Diagnostics.Debug.Print("## TextEntered for snippet");
            Snippet.AfterKeyDown(sender, e, codeView.PopupMenu);
        }
        public virtual void AfterAutoCompleteHandled()
        {
            if (Snippet == null) return;
            System.Diagnostics.Debug.Print("## AfterAutoCompleteHandled for snippet");
            Snippet.AfterAutoCompleteHandled(codeView.PopupMenu);
        }

    }
}
