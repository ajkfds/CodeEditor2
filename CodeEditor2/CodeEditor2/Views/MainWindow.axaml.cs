using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeEditor2.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Global.mainWindow = this;

        InitializeComponent();
        Setups.Setup.InitializeWindow(this);

        Loaded += MainWindow_Loaded;

        Closing += MainWindow_Closing;
        Activated += MainWindow_Activated;
    }

    private async void MainWindow_Activated(object? sender, System.EventArgs e)
    {
        try
        {
            await MainWindow_ActivatedAsync(sender, e);
        }
        catch
        {
            if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
        }
    }

    private async Task MainWindow_ActivatedAsync(object? sender, System.EventArgs e)
    {
        Data.File? file = Controller.NavigatePanel.GetSelectedFile();
        if (file != null) await file.UpdateAsync();

        Data.TextFile? textFile = Controller.CodeEditor.GetTextFile();
        if(textFile != null && textFile != file)
        {
            await textFile.UpdateAsync();
        }
    }



    private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MainView0.FileTypeView.UpdateVisual();
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        Global.Abort = true;
    }


}
