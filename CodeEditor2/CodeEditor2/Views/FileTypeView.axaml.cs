using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ExCSS;
using System.Collections.Generic;

namespace CodeEditor2.Views;

public partial class FileTypeView : UserControl
{
    public FileTypeView()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
        {
            ListBoxItem item = new ListBoxItem();
            item.Content = "TEST";
            ListBox0.Items.Add(item);
            return;
        }
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        ListBox0.Items.Clear();
        foreach(var fileType in Global.FileTypes.Values)
        {
            ListBox0.Items.Add(new FileTypeItem(fileType,FontSize));
        }
    }

    public class FileTypeItem : ListBoxItem
    {
        public FileTypeItem(FileTypes.FileType fileType,double fontSize)
        {
            stackPanel.Margin = new Thickness(0, fontSize*0.1);
            stackPanel.Height = fontSize;
//            stackPanel.MinHeight = minSize;

            textBlock.FontSize = fontSize;


            FontSize = fontSize;
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            Background = new SolidColorBrush(Avalonia.Media.Colors.Transparent);

            checkBox.IsChecked = fileType.Visible;
            checkBox.FontSize = fontSize;
            checkBox.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            checkBox.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            checkBox.Margin = new Thickness(0);
            // use viewbox to change checkbox size
            viewbox.Child = checkBox;
            viewbox.Height = fontSize*1.2;
            viewbox.Width = fontSize*1.2;
            viewbox.Margin = new Thickness(0);
            stackPanel.Children.Add(viewbox);

            iconImage.Width = FontSize;
            iconImage.Height = FontSize;
            iconImage.Margin = new Thickness(0, 0, FontSize * 0.2, 0);
            RenderOptions.SetBitmapInterpolationMode(iconImage, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
            iconImage.Source = fileType.GetIconImage();
            stackPanel.Children.Add(iconImage);

            textBlock.Text = fileType.ID;
            stackPanel.Children.Add(textBlock);
            
            Content = stackPanel;
        }
        Viewbox viewbox = new Viewbox();
        CheckBox checkBox = new CheckBox();
        StackPanel stackPanel = new StackPanel() {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        TextBlock textBlock = new TextBlock() { 
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center 
        };
        protected Avalonia.Controls.Image iconImage = new Avalonia.Controls.Image()
        {
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

    }
}