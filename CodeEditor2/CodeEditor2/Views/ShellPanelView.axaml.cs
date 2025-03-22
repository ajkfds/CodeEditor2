using AjkAvaloniaLibs.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using CodeEditor2.NavigatePanel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace CodeEditor2.Views
{
    public partial class ShellPanelView : UserControl
    {
        private ObservableCollection<ListBoxItem> listItems = new ObservableCollection<ListBoxItem>();
        public ShellPanelView()
        {
            InitializeComponent();

            {
                inputBox = new TextBox();
                inputBox.FontSize = 11;
                inputBox.Margin = new Avalonia.Thickness(0, 0, 0, 0);
                inputBox.Padding = new Avalonia.Thickness(5, 5, 5, 5);
                inputBox.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                inputBox.MinHeight = 8;

                ListBoxItem item = new ListBoxItem();
                item.Content = inputBox;
                item.Margin = new Avalonia.Thickness(0, 0, 0, 0);
                item.Padding = new Avalonia.Thickness(0, 0, 0, 0);
                listItems.Add(item);
            }

            ListBox0.ItemsSource = listItems;

            Style style = new Style();
            style.Selector = ((Selector?)null).OfType(typeof(ListBoxItem));
            style.Add(new Setter(Layoutable.MinHeightProperty, 10.0));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shell = new Shells.WinCmdShell(new List<string> { "prompt $P$G$_" });
                shell.LineReceived += Shell_LineReceived;

                shell.Start();
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                shell = new Shells.LinuxBash();
                shell.LineReceived += Shell_LineReceived;

                shell.Start();
            }

            inputBox.KeyDown += InputBox_KeyDown;
        }

        private void InputBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if(e.Key == Avalonia.Input.Key.Return)
            {
                appendLog(inputBox.Text, Colors.Green);
                shell.Execute(inputBox.Text);
                inputBox.Text = "";
            }
        }

        public void Execute(string command)
        {
            shell.Execute(command);
        }

        TextBox inputBox;
        Shells.Shell shell;
        public void Shell_LineReceived(string lineString)
        {
            appendLog(lineString,null);
        }

        private void appendLog(string lineString,Color? color)
        {
            Dispatcher.UIThread.Post(
                new Action(() =>
                {
                    TextBlock textBlock = new TextBlock()
                    {
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 0),
                        Padding = new Thickness(2, 2, 2, 2),
                        Text = lineString,
                        FontSize = 10,
                        MinHeight = 8
                    };

                    if(color != null)
                    {
                        textBlock.Foreground = new SolidColorBrush((Color)color);
                    }

                    ListBoxItem item = new ListBoxItem()
                    {
                        Content = textBlock,
                        Margin = new Thickness(0, 0, 0, 0),
                        Padding = new Thickness(0, 0, 0, 0),
                        MinHeight = 8
                    };

                    lock (listItems)
                    {
                        int i = listItems.Count - 1;
                        if (i < 0) i = 0;
                        listItems.Insert(i, item);
                        
                        if (listItems.Count > 1000)
                        {
                            ListBoxItem? removeItem = listItems[0] as ListBoxItem;
                            if (removeItem == null) return;
                            listItems.Remove(removeItem);
                        }
                        
                        listItems.Last().IsSelected = true;
                        ListBox0.ScrollIntoView(listItems.Last());
                    }
//                    ListBox0.InvalidateVisual();
                })
            );
        }
    }


}
