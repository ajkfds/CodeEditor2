using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using CodeEditor2.Data;
using CodeEditor2.Setups;
using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;

namespace CodeEditor2.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        Title = Setups.Setup.ApplicationName;
        TitleText.Text = Setups.Setup.ApplicationName;
        if(Setups.Setup.GetIconImage!=null) SymbolImage.Source = Setups.Setup.GetIconImage();

        Setups.Setup.InitializeWindow(this);

        CreateNewSolutionButton.Click += CreateNewSolutionButton_Click;
        OpenSolutionButton.Click += OpenSolutionButton_Click;

        Global.Setup.LoadSetup();
        Global.Setup.Historys.Sort((a,b)=> b.LastAccessed.CompareTo(a.LastAccessed));

        foreach(Setups.Setup.History history in Global.Setup.Historys)
        {
            Avalonia.Controls.Button button = new Button() {HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch};
            {
                Tools.VerticalStackPanelConstructor verticalStackPanelConstructor = new Tools.VerticalStackPanelConstructor();
                button.Content = verticalStackPanelConstructor.StackPanel;

                verticalStackPanelConstructor.AppendText(history.Name, 18, Tools.VerticalStackPanelConstructor.Style.Bold);
                verticalStackPanelConstructor.AppendText(history.AbsolutePath);
                verticalStackPanelConstructor.AppendText("Last Accessed : "+history.LastAccessed.ToString(),7);
            }
            button.Click += Button_Click;
            historyTarget.Add(button, history);

            HistoryStackPanel.Children.Add(button);
        }
    }
    Dictionary<Button, Setups.Setup.History> historyTarget = new Dictionary<Button, Setup.History>();


    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Button? button = sender as Button;
        if (button == null) return;
        Setups.Setup.History history = historyTarget[button];

        Global.Solution.Name = history.Name;
        Global.Solution.AbsolutePath = history.AbsolutePath;
        openMainWindow();

        this.Close();
    }

    private async void OpenSolutionButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Solution File")
                {
                    Patterns = new[] { "*"+Solution.FileExtention },
                    MimeTypes = new[] { "text/plain" }
                },
            }
        });

        if (files.Count != 1) return;

        System.Uri uri = files[0].Path;

        Global.Solution.AbsolutePath = uri.LocalPath + Uri.UnescapeDataString(uri.Fragment);
        openMainWindow();

        this.Close();
    }

    private async void CreateNewSolutionButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var file = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Create New Solution File",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Solution File")
                {
                    Patterns = new[] { "*"+Solution.FileExtention },
                    MimeTypes = new[] { "text/plain" }
                },
            }
        });

        if (file == null) return;
        System.Uri uri = file.Path;

        Global.Solution.AbsolutePath = uri.LocalPath + Uri.UnescapeDataString(uri.Fragment);

        string name = "";
        name = System.IO.Path.GetFileName(Global.Solution.AbsolutePath);
        if (name.EndsWith(Solution.FileExtention))
        {
            name = name.Substring(0, name.Length - Solution.FileExtention.Length);
        }
        if (name == "") return;

        Global.Solution.Name = name;
        openMainWindow();

        string? path = System.IO.Path.GetDirectoryName(Global.Solution.AbsolutePath);
        if(path != null)
        {
            Data.Project newProject = Project.Create(path);
            await Controller.AddProject(newProject);
        }
        
        this.Close();
    }

    private async void openMainWindow()
    {
        Setups.Setup.History? history = Global.Setup.Historys
            .Find((x) =>
            {
                if (x.AbsolutePath == Global.Solution.AbsolutePath && x.Name == Global.Solution.Name)
                {
                    return true;
                }
                return false;
            });
        if(history == null)
        {
            Global.Setup.Historys.Add(
                new Setups.Setup.History()
                {
                    AbsolutePath = Global.Solution.AbsolutePath,
                    LastAccessed = DateTime.Now,
                    Name = Global.Solution.Name
                }); ;
        }
        else
        {
            history.LastAccessed = DateTime.Now;
        }
        Global.Setup.SaveSetup();

        MainWindow mainWindow = new MainWindow();
        mainWindow.Title = Setups.Setup.ApplicationName + " " + Global.Solution.Name;
        Global.currentWindow = mainWindow;

        initialize();
        mainWindow.Show();
        await mainWindow.MainView0.Initialize();
    }

    private void initialize()
    {
        // register text filetype
        FileTypes.TextFile textFileType = new FileTypes.TextFile();
        Global.FileTypes.Add(textFileType.ID, textFileType);
        FileTypes.FileClassifyFile fileClassifyFile = new FileTypes.FileClassifyFile();
        Global.FileTypes.Add(fileClassifyFile.ID, fileClassifyFile);

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