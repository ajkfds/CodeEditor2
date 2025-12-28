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
        Loaded += ItemPropertyForm_Loaded;
        node.InitializePropertyForm(this);

        Initialized += ItemPropertyForm_Initialized;
        OkButton.Click += OkButton_Click;
        CancelButton.Click += CancelButton_Click;
    }

    private void ItemPropertyForm_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Position = new PixelPoint((int)(Position.X+Width*0.1), (int)(Position.Y+Height*0.1));
        Width = Width * 0.8;
        Height = Height * 0.8;
    }

    public TabControl TabControl { get => Tabs; }
    public Button OkButtonControl { get => OkButton; }
    public Button CancelButtonControl { get => CancelButton; }

    private void ItemPropertyForm_Initialized(object? sender, System.EventArgs e)
    {
        _node.UpdatePropertyForm(this);
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