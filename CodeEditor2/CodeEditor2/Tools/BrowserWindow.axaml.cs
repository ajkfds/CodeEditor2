using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CodeEditor2.Tools;

public partial class BrowserWindow : Window
{
    public BrowserWindow()
    {
        InitializeComponent();

        Loaded += BrowserWindow_OnLoaded;
    }

    private async void BrowserWindow_OnLoaded(object? sender, System.EventArgs e)
    {
        await browser.NavigateAsync("https://www.google.com");   
        //throw new System.NotImplementedException();
    }


}