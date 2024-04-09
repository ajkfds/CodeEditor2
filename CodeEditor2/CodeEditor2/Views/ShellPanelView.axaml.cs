using AjkAvaloniaLibs.Contorls;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using CodeEditor2.NavigatePanel;
using System;
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
                inputBox.Padding = new Avalonia.Thickness(0, 0, 0, 0);
                inputBox.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
                inputBox.Height = 11;
                inputBox.MinHeight = 11;

                ListBoxItem item = new ListBoxItem();
                item.Content = inputBox;
                listItems.Add(item);
            }

            ListBox0.ItemsSource = listItems;

            Style style = new Style();
            style.Selector = ((Selector?)null).OfType(typeof(ListBoxItem));
            style.Add(new Setter(Layoutable.MinHeightProperty, 8.0));
            style.Add(new Setter(Layoutable.HeightProperty, 10.0));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shell = new Shells.WinCmdChell();
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

        TextBox inputBox;
        Shells.Shell shell;
        private void Shell_LineReceived(string lineString)
        {
            appendLog(lineString,null);
        }

        private void appendLog(string lineString,Color? color)
        {
            Dispatcher.UIThread.Post(
                    new Action(() =>
                    {
                        TextBlock textBlock = new TextBlock();
                        textBlock.Text = lineString;
                        textBlock.FontSize = 10;
                        textBlock.Height = 11;
                        textBlock.MinHeight = 11;
                        if(color != null)
                        {
                            textBlock.Foreground = new SolidColorBrush((Color)color);
                        }
                        textBlock.Margin = new Avalonia.Thickness(0, 0, 0, 0);
                        lock (listItems)
                        {
                            ListBoxItem item = new ListBoxItem();
                            item.Content = textBlock;
                            int i = listItems.Count - 2;
                            if (i < 0) i = 0;
                            listItems.Insert(i, item);
                            if (listItems.Count > 1000)
                            {
                                ListBoxItem? removeItem = listItems[0] as ListBoxItem;
                                if (removeItem == null) return;
                                listItems.Remove(removeItem);
                            }

                        }
                    })
                );
        }


    }


}
