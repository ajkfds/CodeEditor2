using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CodeEditor2.Data;
using CodeEditor2.Setups;
using System;
using System.Collections.Generic;

namespace CodeEditor2.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        Title = Setups.Setup.ApplicationName;
        TitleText.Text = Setups.Setup.ApplicationName;
        if (Setups.Setup.GetIconImage != null) SymbolImage.Source = Setups.Setup.GetIconImage();

        Setups.Setup.InitializeWindow(this);

        CreateNewSolutionButton.Click += CreateNewSolutionButton_Click;
        OpenSolutionButton.Click += OpenSolutionButton_Click;

        Global.Setup.LoadSetup();
        // Remove history entries that lost their name. Older builds could add a
        // nameless entry (see openMainWindow) for the very same solution file, so
        // the list showed a blank/counterpart row and the renaming effort appeared
        // to be "lost".
        MergeHistories();
        SortHistories();

        createHistoryButtons();
    }

    /// <summary>
    /// Merges history entries that point to the same solution file and drops
    /// entries without a name, keeping the most recently accessed information.
    /// </summary>
    /// <remarks>
    /// History entries used to be identified by (AbsolutePath AND Name). Opening a
    /// solution through "Open Solution" left <see cref="Solution.Name"/> empty, so
    /// that path added a *second* entry for a file that already had a named entry:
    /// the same solution showed up twice, once with its name and once without, and
    /// the nameless duplicate was the one that got the newest LastAccessed.
    /// Merging by AbsolutePath keeps a single, named row per solution file.
    /// </remarks>
    private void MergeHistories()
    {
        Dictionary<string, Setups.Setup.History> merged =
            new Dictionary<string, Setups.Setup.History>(StringComparer.OrdinalIgnoreCase);

        foreach (Setups.Setup.History history in Global.Setup.Historys)
        {
            if (history == null) continue;
            if (history.AbsolutePath == "") continue;

            if (!merged.TryGetValue(history.AbsolutePath, out Setups.Setup.History? existing))
            {
                merged.Add(history.AbsolutePath, history);
                continue;
            }

            // Same solution file: keep the most recent access time and the first
            // non-empty name / pinned order.
            if (history.LastAccessed > existing.LastAccessed) existing.LastAccessed = history.LastAccessed;
            if (existing.Name == "" && history.Name != "") existing.Name = history.Name;
            if (existing.PinnedOrder == null) existing.PinnedOrder = history.PinnedOrder;
        }

        List<Setups.Setup.History> result = new List<Setups.Setup.History>();
        foreach (Setups.Setup.History history in merged.Values)
        {
            // A nameless entry cannot be displayed meaningfully; fall back to the
            // solution file name instead of dropping the entry entirely.
            if (history.Name == "") history.Name = GetDefaultHistoryName(history);
            result.Add(history);
        }

        Global.Setup.Historys = result;
    }

    /// <summary>
    /// Derives a display name from the solution file name ("foo.sln.json" -> "foo").
    /// </summary>
    private static string GetDefaultHistoryName(Setups.Setup.History history)
    {
        string name = System.IO.Path.GetFileName(history.AbsolutePath);
        if (name.EndsWith(Solution.FileExtention))
        {
            name = name.Substring(0, name.Length - Solution.FileExtention.Length);
        }
        if (name == "") name = history.AbsolutePath;
        return name;
    }

    /// <summary>
    /// Sorts the history list: pinned entries first (by PinnedOrder), then by the
    /// most recent access time.
    /// </summary>
    private void SortHistories()
    {
        Global.Setup.Historys.Sort((a, b) =>
        {
            bool aPinned = a.PinnedOrder != null;
            bool bPinned = b.PinnedOrder != null;
            if (aPinned != bPinned) return aPinned ? -1 : 1;
            if (aPinned && bPinned)
            {
                int order = a.PinnedOrder!.Value.CompareTo(b.PinnedOrder!.Value);
                if (order != 0) return order;
            }
            return b.LastAccessed.CompareTo(a.LastAccessed);
        });
    }

    private void createHistoryButtons()
    {
        HistoryStackPanel.Children.Clear();
        historyTarget.Clear();

        foreach (Setups.Setup.History history in Global.Setup.Historys)
        {
            Avalonia.Controls.Button button = new Button() { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
            {
                Tools.VerticalStackPanelConstructor verticalStackPanelConstructor = new Tools.VerticalStackPanelConstructor();
                button.Content = verticalStackPanelConstructor.StackPanel;

                // Never render an empty title: a nameless entry looked like the
                // stored name had disappeared. Derive a fallback from the path.
                string displayName = history.Name;
                if (displayName == "") displayName = GetDefaultHistoryName(history);
                verticalStackPanelConstructor.AppendText(displayName, 18, Tools.VerticalStackPanelConstructor.Style.Bold);
                verticalStackPanelConstructor.AppendText(history.AbsolutePath);
                verticalStackPanelConstructor.AppendText("Last Accessed : " + history.LastAccessed.ToString(), 7);
            }
            button.Click += Button_Click;
            historyTarget.Add(button, history);
            button.ContextMenu = createHistoryContextMenu(button);

            HistoryStackPanel.Children.Add(button);
        }
    }
    Dictionary<Button, Setups.Setup.History> historyTarget = new Dictionary<Button, Setup.History>();

    private Avalonia.Controls.ContextMenu createHistoryContextMenu(Button button)
    {
        Setups.Setup.History history = historyTarget[button];
        Avalonia.Controls.ContextMenu contextMenu = new Avalonia.Controls.ContextMenu();

        MenuItem renameItem = new MenuItem() { Header = "Rename" };
        renameItem.Click += async (sender, e) =>
        {
            Tools.InputWindow inputWindow = new Tools.InputWindow("Rename History", "New name", history.Name);
            await inputWindow.ShowDialog(this);
            if (inputWindow.Cancel) return;
            string newName = inputWindow.InputText;
            if (newName == "" || newName == history.Name) return;

            history.Name = newName;
            // Keep the in-memory Solution name in sync: the entry is looked up by
            // AbsolutePath afterwards, and an older build matched on (path + name),
            // which made a renamed entry look like it had been dropped.
            if (string.Equals(history.AbsolutePath, Global.Solution.AbsolutePath, StringComparison.OrdinalIgnoreCase))
            {
                Global.Solution.Name = newName;
            }
            Global.Setup.SaveSetup();

            createHistoryButtons();
        };
        contextMenu.Items.Add(renameItem);

        MenuItem deleteItem = new MenuItem() { Header = "Delete" };
        deleteItem.Click += async (sender, e) =>
        {
            Tools.YesNoWindow confirmWindow = new Tools.YesNoWindow("Delete History", "Delete \"" + history.Name + "\" from history ?", true, true);
            await confirmWindow.ShowDialog(this);
            if (!confirmWindow.Yes) return;

            Global.Setup.Historys.Remove(history);
            Global.Setup.SaveSetup();

            createHistoryButtons();
        };
        contextMenu.Items.Add(deleteItem);

        return contextMenu;
    }


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

    // Open exsisted solution.set solution and open mainwindow
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<exception handled>")]
    private async void OpenSolutionButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
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
            // "Open Solution" does not know the name stored in the history entry.
            // Without this the opened solution carried an empty name, which used to
            // create a nameless duplicate history entry for an already named file.
            // openMainWindow() replaces it with the stored name when the file is
            // already in the history.
            Global.Solution.Name = GetDefaultSolutionName(Global.Solution.AbsolutePath);
            openMainWindow();

            this.Close();
        }
        catch (Exception ex)
        {
            Tools.YesNoWindow alartWindow = new Tools.YesNoWindow("Error", "Failed to open solution file.\n" + ex.Message,true,false);
            await alartWindow.ShowDialog(this);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<exception handled>")]
    private async void CreateNewSolutionButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
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

            string name = GetDefaultSolutionName(Global.Solution.AbsolutePath);
            if (name == "") return;

            Global.Solution.Name = name;
            openMainWindow();

            string? path = System.IO.Path.GetDirectoryName(Global.Solution.AbsolutePath);
            if (path != null)
            {
                Data.Project newProject = await Project.CreateAsync(path);
                await Controller.addProjectAsync(newProject);
            }

            this.Close();
        }
        catch (Exception ex)
        {
            Tools.YesNoWindow alartWindow = new Tools.YesNoWindow("Error", "Failed to create solution file.\n" + ex.Message, true, false);
            await alartWindow.ShowDialog(this);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<保留中>")]
    private async void openMainWindow()
    {
        try
        {
            // update opened Solution history file
            //
            // Identify the entry by AbsolutePath only. Matching on (AbsolutePath AND
            // Name) used to add a second, nameless entry for a solution that was
            // already in the history: "Open Solution" does not know the stored name,
            // and after a rename Solution.Name no longer matched either. The rename
            // then appeared to be forgotten because the nameless duplicate received
            // the newest LastAccessed and was listed first.
            if (Global.Solution.Name == "")
            {
                Global.Solution.Name = GetDefaultSolutionName(Global.Solution.AbsolutePath);
            }

            Setups.Setup.History? history = Global.Setup.Historys
                .Find((x) => string.Equals(x.AbsolutePath, Global.Solution.AbsolutePath, StringComparison.OrdinalIgnoreCase));
            if (history == null)
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
                // Keep the previously stored (possibly renamed) display name.
                if (history.Name != "") Global.Solution.Name = history.Name;
            }
            Global.Setup.SaveSetup();


            MainWindow mainWindow = new MainWindow();
            mainWindow.Title = Setups.Setup.ApplicationName + " " + Global.Solution.Name;
            Global.currentWindow = mainWindow;

            initialize();
            mainWindow.Show();

            await mainWindow.MainView0.Initialize(); // load projects in solution
        }
        catch (Exception ex)
        {
            Tools.YesNoWindow alartWindow = new Tools.YesNoWindow("Error", "Failed to create solution file.\n" + ex.Message, true, false);
            await alartWindow.ShowDialog(this);
        }
    }

    /// <summary>
    /// Derives a solution display name from its file path
    /// ("foo.sln.json" -> "foo").
    /// </summary>
    private static string GetDefaultSolutionName(string absolutePath)
    {
        string name = System.IO.Path.GetFileName(absolutePath);
        if (name.EndsWith(Solution.FileExtention))
        {
            name = name.Substring(0, name.Length - Solution.FileExtention.Length);
        }
        return name;
    }

    private void initialize()
    {
        // register text filetype
        FileTypes.TextFile textFileType = new FileTypes.TextFile();
        Global.FileTypes.Add(textFileType.ID, textFileType);

        FileTypes.FileClassifyFile fileClassifyFile = new FileTypes.FileClassifyFile();
        Global.FileTypes.Add(fileClassifyFile.ID, fileClassifyFile);

        FileTypes.YamlFile yamlFile = new FileTypes.YamlFile();
        Global.FileTypes.Add(yamlFile.ID, yamlFile);

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
