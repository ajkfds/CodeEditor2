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
    // X11迺ｰ蠅・〒縺ｯ蜻ｼ縺ｳ蜃ｺ縺怜・縺ｧ縺ｮWindowStartupLocation = WindowStartupLocation.CenterOwner;
    // 縺悟柑縺九↑縺・ｴ蜷医′縺ゅｋ
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (this.Owner is Window parent)
        {
            // 1. 隕ｪ繧ｦ繧｣繝ｳ繝峨え縺ｮ繧ｹ繧ｯ繝ｪ繝ｼ繝ｳ荳翫・蠎ｧ讓吶→繧ｵ繧､繧ｺ繧貞叙蠕・
            // Position縺ｯPixelPoint(迚ｩ逅・ヴ繧ｯ繧ｻ繝ｫ)縲。ounds縺ｯ隲也炊繝斐け繧ｻ繝ｫ縺ｪ縺ｮ縺ｧ豕ｨ諢・
            var parentPos = parent.Position;
            var parentSize = parent.Bounds.Size;

            // 2. 閾ｪ蛻・ｼ医ム繧､繧｢繝ｭ繧ｰ・峨・迴ｾ蝨ｨ縺ｮ繧ｵ繧､繧ｺ繧貞叙蠕・
            var selfSize = this.Bounds.Size;

            // 3. 荳ｭ螟ｮ菴咲ｽｮ繧定ｨ育ｮ・(迚ｩ逅・ヴ繧ｯ繧ｻ繝ｫ謠帷ｮ励′蠢・ｦ√↑蝣ｴ蜷医′縺ゅｋ縺溘ａ縲ヾcaling繧定・・)
            double scaling = this.DesktopScaling;

            int centerX = parentPos.X + (int)((parentSize.Width - selfSize.Width) * scaling / 2);
            int centerY = parentPos.Y + (int)((parentSize.Height - selfSize.Height) * scaling / 2);

            // 4. 譁ｰ縺励＞菴咲ｽｮ繧定ｨｭ螳・
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