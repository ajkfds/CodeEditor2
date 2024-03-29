﻿using Avalonia;
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

    private const string setupFileName = "CodeEditor2.json";

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Tools.ProgressWindow progressWindow = new Tools.ProgressWindow("CodeEditor2", "Loading...", 100);

            desktop.MainWindow = progressWindow;
            Global.currentWindow = progressWindow;
            progressWindow.Show();

            MainWindow mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };

            //for (double i = 0; i <= 100; i=i+10)
            //{
            //    await Task.Delay(1);
            //    progressWindow.ProgressValue = i;
            //}
            await Task.Delay(1);

            initialize(progressWindow);

            // read setup file
            if (System.IO.File.Exists(setupFileName))
            {
                Global.Setup.LoadSetup(setupFileName);
                await Task.Delay(1);
            }

            //var task = Task.Run(() =>
            //{
            //    initialize(progressWindow);
            //});

            desktop.MainWindow = mainWindow;
            Global.currentWindow = mainWindow;
            mainWindow.Show();
            progressWindow.Close();
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

    private void initialize(Tools.ProgressWindow progressWindow)
    {
        // register text filetype
        FileTypes.TextFile textFileType = new FileTypes.TextFile();
        Global.FileTypes.Add(textFileType.ID, textFileType);

        // load pulgins
        List<CodeEditor2Plugin.IPlugin> plugins = new List<CodeEditor2Plugin.IPlugin>();
        foreach (var plugin in Global.Plugins.Values)
        {
            plugins.Add(plugin);
        }
        Global.Plugins.Clear();

        while (true)
        {
            int registered = 0;
            foreach (var plugin in plugins)
            {
                if (!Global.Plugins.ContainsKey(plugin.Id))
                {
                    bool complete = plugin.Register();
                    if (complete)
                    {
                        registered++;
                        Global.Plugins.Add(plugin.Id, plugin);
                        Controller.AppendLog("Loading plugin ... " + plugin.Id);
                    }
                }
            }
            if (registered == 0) break;
        }

        // initialize pulgins
        List<string> initilalizedPulginName = new List<string>();
        while (true)
        {
            int initialized = 0;
            foreach (string pluginName in Global.Plugins.Keys)
            {
                if (initilalizedPulginName.Contains(pluginName)) continue;
                if (Global.Plugins[pluginName].Initialize())
                {
                    initialized++;
                    Controller.AppendLog("Initializing plugin ... " + pluginName);
                    initilalizedPulginName.Add(pluginName);
                }
            }
            if (initialized == 0) break;
        }


    }
}
