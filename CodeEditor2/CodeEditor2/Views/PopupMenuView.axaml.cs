using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Styling;
using CodeEditor2.CodeEditor;
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

            Style style = new Style();
            style.Selector = ((Selector?)null).OfType(typeof(ListBoxItem));
            style.Add(new Setter(Layoutable.MinHeightProperty, 8.0));
            style.Add(new Setter(Layoutable.HeightProperty, 14.0));
            ListBox0.Styles.Add(style);

            KeyDown += PopupMenuView_KeyDown;
            LostFocus += PopupMenuView_LostFocus;
            TextBox0.TextChanged += TextBox0_TextChanged;

            if (ListBox0.Items.Count > 0)
            {
                ListBox0.SelectedIndex = 0;
            }
        }

        private void TextBox0_TextChanged(object? sender, TextChangedEventArgs e)
        {
            PopupMenuItem? selectedItem = ListBox0.SelectedItem as PopupMenuItem;

            List<PopupMenuItem> topHititems = new List<PopupMenuItem>();
            List<PopupMenuItem> partialHititems = new List<PopupMenuItem>();

            if (TextBox0.Text == null) return;
            string targetText = TextBox0.Text.ToLower();

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

            ListBox0.Items.Clear();
            foreach(var item in topHititems)
            {
                ListBox0.Items.Add(item);
            }
            foreach (var item in partialHititems)
            {
                ListBox0.Items.Add(item);
            }

            if(selectedItem != null)
            {
                if (ListBox0.Items.Contains(selectedItem))
                {
                    ListBox0.SelectedItem = selectedItem;
                } 
            }

            if (ListBox0.Items.Count == 0)
            {
                cancel();
            }
            if (ListBox0.SelectedItem == null)
            {
                ListBox0.SelectedIndex = 0;
            }
        }

        public Action<PopupMenuItem?>? Selected;


        public void OnOpen(CancelEventArgs args)
        {
            TextBox0.Text = "";
            ListBox0.Items.Clear();

            foreach(PopupMenuItem item in Global.codeView.codeViewPopupMenu.PopupMenuItems)
            {
                ListBox0.Items.Add(item);
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

            if(e.Key == Avalonia.Input.Key.Up)
            {
                int i = ListBox0.SelectedIndex;
                if (i != 0) i--;
                ListBox0.SelectedIndex = i;
                return;
            }

            if(e.Key == Avalonia.Input.Key.Down)
            {
                int i = ListBox0.SelectedIndex;
                if (i < ListBox0.ItemCount - 1) i++;
                ListBox0.SelectedIndex = i;
                return;
            }
        }
        private void PopupMenuView_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            cancel();
        }

        private void cancel()
        {
            Global.codeView.HidePopupMenu();
        }

        private void select()
        {
            Global.codeView.HidePopupMenu();
            PopupMenuItem? selectedItem = ListBox0.SelectedItem as PopupMenuItem;
            if (selectedItem == null) return;

            if (Selected != null) Selected(selectedItem);
        }


    }
}
