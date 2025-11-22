using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Collections.Generic;

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
    }

    private void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        Global.Abort = true;
    }


}
