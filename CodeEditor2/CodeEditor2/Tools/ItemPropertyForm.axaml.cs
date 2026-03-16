using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

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


    // workaround
    // X11環境では呼び出し側でのWindowStartupLocation = WindowStartupLocation.CenterOwner;
    // が効かない場合がある
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (this.Owner is Window parent)
        {
            // 1. 親ウィンドウのスクリーン上の座標とサイズを取得
            // PositionはPixelPoint(物理ピクセル)、Boundsは論理ピクセルなので注意
            var parentPos = parent.Position;
            var parentSize = parent.Bounds.Size;

            // 2. 自分（ダイアログ）の現在のサイズを取得
            var selfSize = this.Bounds.Size;

            // 3. 中央位置を計算 (物理ピクセル換算が必要な場合があるため、Scalingを考慮)
            double scaling = this.DesktopScaling;

            int centerX = parentPos.X + (int)((parentSize.Width - selfSize.Width) * scaling / 2);
            int centerY = parentPos.Y + (int)((parentSize.Height - selfSize.Height) * scaling / 2);

            // 4. 新しい位置を設定
            this.Position = new PixelPoint(centerX, centerY);
        }
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
