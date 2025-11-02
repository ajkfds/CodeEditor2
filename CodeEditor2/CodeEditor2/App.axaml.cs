using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using CodeEditor2.ViewModels;
using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodeEditor2;

public partial class App : Application
{
    public override void Initialize()
    {
        System.Threading.Thread.CurrentThread.Name = "UI";
        Global.UIThread = System.Threading.Thread.CurrentThread;

        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Views.SplashWindow splashWindow = new SplashWindow();

            desktop.MainWindow = splashWindow;
            splashWindow.Show();
        }
        //else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        //{
        //    singleViewPlatform.MainView = new MainView
        //    {
        //        DataContext = new MainViewModel()
        //    };
        //}

        base.OnFrameworkInitializationCompleted();
    }

}
