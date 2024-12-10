using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace CodeEditor2.Views
{
    public partial class PopupMenuView : UserControl
    {
        public PopupMenuView()
        {
            InitializeComponent();
            this.Margin = new Avalonia.Thickness(0);
            this.Padding = new Avalonia.Thickness(0);

            //Style style = new Style();
            //style.Selector = ((Selector?)null).OfType(typeof(ListBoxItem));
            //style.Add(new Setter(Layoutable.MinHeightProperty, 8.0));
            //style.Add(new Setter(Layoutable.HeightProperty, 14.0));
            //ListBox0.Styles.Add(style);

            KeyDown += PopupMenuView_KeyDown;
            LostFocus += PopupMenuView_LostFocus;
            TextBox0.TextChanged += TextBox0_TextChanged;

            if (ListView.Items.Count > 0)
            {
                ListView.SelectedIndex = 0;
            }
        }

        private void TextBox0_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (Design.IsDesignMode) return;

            PopupMenuItem? selectedItem = null;//ListBox0.SelectedItem as PopupMenuItem;

            List<PopupMenuItem> topHititems = new List<PopupMenuItem>();
            List<PopupMenuItem> partialHititems = new List<PopupMenuItem>();

            if (TextBox0.Text == null) return;
            string targetText = TextBox0.Text.ToLower();

            if(TextBox0.Text == "mo")
            {
                string s = "";
            }

            foreach (PopupMenuItem item in Global.codeView.codeViewPopupMenu.PopupMenuItems)
            {
                if (item.Text == null) continue;
                if(targetText == "")
                {
                    topHititems.Add(item);
                    continue;
                }

                if (item.Text.ToLower().StartsWith(targetText))
                {
                    topHititems.Add(item);
                }
                else if(item.Text.ToLower().Contains(targetText))
                {
                    partialHititems.Add(item);
                }
            }

            ListView.Items.Clear();
            foreach (var item in topHititems)
            {
                ListView.Items.Add(item);
            }
            foreach (var item in partialHititems)
            {
                ListView.Items.Add(item);
            }

            if (selectedItem != null)
            {
                if (ListView.Items.Contains(selectedItem))
                {
                    ListView.SelectedItem = selectedItem;
                }
            }

            if (ListView.Items.Count == 0)
            {
                cancel();
            }
            if (ListView.SelectedItem == null)
            {
                ListView.SelectedIndex = 0;
            }
        }

        public Action<PopupMenuItem?>? Selected;


        public void OnOpen(CancelEventArgs args)
        {
            TextBox0.Text = "";
            ListView.Items.Clear();

            foreach (PopupMenuItem item in Global.codeView.codeViewPopupMenu.PopupMenuItems)
            {
                ListView.Items.Add(item);
            }
        }

        private void PopupMenuView_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Return | e.Key == Avalonia.Input.Key.Enter)
            {
                select();
                return;
            }

            if (e.Key == Avalonia.Input.Key.Escape)
            {
                cancel();
                return;
            }

            if (e.Key == Avalonia.Input.Key.Up)
            {
                int i = ListView.SelectedIndex;
                if (i != 0) i--;
                ListView.SelectedIndex = i;
                return;
            }

            if (e.Key == Avalonia.Input.Key.Down)
            {
                int i = ListView.SelectedIndex;
                if (i < ListView.ItemCount - 1) i++;
                ListView.SelectedIndex = i;
                return;
            }
        }
        private void PopupMenuView_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            cancel();
            //CodeEditor2.Controller.CodeEditor.AbortInteractiveSnippet();
        }

        private void cancel()
        {
            Global.codeView.HidePopupMenu();
        }

        private void select()
        {
            Global.codeView.HidePopupMenu();
            PopupMenuItem? selectedItem = ListView.SelectedItem as PopupMenuItem;
            if (selectedItem == null) return;

            if (Selected != null) Selected(selectedItem);
        }


    }
}
