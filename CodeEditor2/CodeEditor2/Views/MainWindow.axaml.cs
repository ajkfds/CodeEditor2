using Avalonia.Controls;

namespace CodeEditor2.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        Global.mainWindow = this;

        InitializeComponent();
    }

}
