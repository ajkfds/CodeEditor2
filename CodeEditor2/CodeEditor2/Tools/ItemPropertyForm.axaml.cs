using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CodeEditor2.Tools;

public partial class ItemPropertyForm : Window
{
    public ItemPropertyForm(NavigatePanel.NavigatePanelNode node)
    {
        InitializeComponent();
        _node = node;

        Data.Item? item = _node.Item;
        if (item == null) return;
        RelativePathText.Text = item.RelativePath;

        if(item is Data.File)
        {
            Data.File file = (Data.File)item;
            if (file.FileType != null)
            {
                FileTYpeText.Text = file.FileType.ToString();
            }
        }

        OkButton.Click += OkButton_Click;
        CancelButton.Click += CancelButton_Click;
    }

    private NavigatePanel.NavigatePanelNode _node;

    private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void OkButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}