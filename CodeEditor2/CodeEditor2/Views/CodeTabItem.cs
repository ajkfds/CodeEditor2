using System;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Media.Imaging;

namespace CodeEditor2.Views
{
    public class CodeTabItem : Avalonia.Controls.TabItem, IStyleable // IStylable is need to inherit from TabItem (https://github.com/AvaloniaUI/Avalonia/issues/2566)
    {
        public CodeTabItem(string title, string? iconName, Avalonia.Media.Color? iconColor, bool closeButtonEnable)
        {
            if (ActiveCloseButtonBmp == null) ActiveCloseButtonBmp = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                                    "CodeEditor2/Assets/Icons/x.svg",
                                    Avalonia.Media.Colors.Gray
                                    );
            if (InactivecloseButtonBmp == null) InactivecloseButtonBmp = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                                    "CodeEditor2/Assets/Icons/x.svg",
                                    Avalonia.Media.Color.FromRgb(50, 50, 50)
                                    );

            TextBlock headerText = new TextBlock();
            if (iconName != null)
            {
                if (iconColor == null) throw new Exception();
                headerText.Inlines?.Add(new Image
                {
                    Source = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                                    "CodeEditor2/Assets/Icons/" + iconName + ".svg",
                                    (Avalonia.Media.Color)iconColor
                                    ),
                    Width = 12,
                    Height = 12
                });
            }

            headerText.Inlines?.Add(new Avalonia.Controls.Documents.Run(title));

            if (closeButtonEnable)
            {
                CloseButton = new Image
                {
                    Source = InactivecloseButtonBmp,
                    Width = 12,
                    Height = 12,
                    Margin = new Avalonia.Thickness(4, 0, 0, 0)
                };
                headerText.Inlines?.Add(CloseButton);
                CloseButton.Tapped += CloseButton_Tapped;
                CloseButton.PointerEntered += CloseButton_PointerEntered;
                CloseButton.PointerExited += CloseButton_PointerExited;
            }

            Header = headerText;
            FontSize = 12.0;
        }

        internal static Bitmap ActiveCloseButtonBmp;
        internal static Bitmap InactivecloseButtonBmp;



        private void CloseButton_PointerExited(object? sender, PointerEventArgs e)
        {
            CloseButton.Source = InactivecloseButtonBmp;
        }

        private void CloseButton_PointerEntered(object? sender, PointerEventArgs e)
        {
            CloseButton.Source = ActiveCloseButtonBmp;
        }

        public  void CloseButton_Tapped(object? sender, TappedEventArgs e)
        {
            if(CloseButton_Clicked != null) CloseButton_Clicked();
        }

        public Action? CloseButton_Clicked;

        Type IStyleable.StyleKey => typeof(TabItem); // need to inherit from TabItem (https://github.com/AvaloniaUI/Avalonia/issues/2566)
        Image? CloseButton;


    }
}