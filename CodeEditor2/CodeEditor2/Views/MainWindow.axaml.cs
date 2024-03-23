using Avalonia.Controls;

namespace CodeEditor2.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Global.mainWindow = this;

        InitializeComponent();

        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        Global.Abort = true;
    }
}
