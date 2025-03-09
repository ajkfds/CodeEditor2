using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CodeEditor2.Tools;

public partial class BrowserWindow : Window
{
    public BrowserWindow()
    {
        InitializeComponent();

        OnLoaded+= BrowserWindow_OnLoaded;
    }

    private void BrowserWindow_OnLoaded(object? sender, System.EventArgs e)
    {
        
        //throw new System.NotImplementedException();
    }


}