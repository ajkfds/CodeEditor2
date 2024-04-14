﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CodeEditor2.Data;
using CodeEditor2.Setups;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CodeEditor2.Views;

public partial class MainView : UserControl
{
    ////private const string setupFileName = "CodeEditor2.json";

    public MainView()
    {
        Global.mainView = this;
        InitializeComponent();
        DataContext = new ViewModels.MainViewModel();

        timer.Interval = new TimeSpan(1);
        timer.Tick += Timer_Tick;
        timer.Start();

    }
    private void Timer_Tick(object? sender, EventArgs e)
    {
        // should launch afer mainwindow shown
        timer.Stop();
        // read setup file

        Global.ProgressWindow = new Tools.ProgressWindow();
//        Global.ProgressWindow.Show(Global.mainWindow);
        var _ = initialize();
    }

    private DispatcherTimer timer = new DispatcherTimer();

    private const string setupFileName = "CodeEditor2.json";

    private async Task initialize()
    {
        try
        {
            await Global.Setup.LoadSetup(setupFileName);
        }
        catch(Exception ex)
        {
            throw;
        }

//        Dispatcher.UIThread.Post(() => { Global.ProgressWindow.Close(); });
    }


    private void MenuItem_Open_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void MenuItem_Exit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
    private async void MenuItem_AddProjectPath_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Tools.InputWindow inputWinodw = new Tools.InputWindow("New Project Name","Input Project Absolute Path");
        await inputWinodw.ShowDialog(Global.mainWindow);
        if (inputWinodw.Cancel) return;
        string path = inputWinodw.InputText;
        if (!System.IO.Directory.Exists(path))
        {
            Controller.AppendLog("no folders found");
            return;
        }
        Global.StopParse = true;
        Controller.AppendLog("AddProject" + path, Avalonia.Media.Colors.DarkOrange);
        path = path.Replace('/', System.IO.Path.DirectorySeparatorChar);
        Data.Project newProject = Project.Create(path);
        await Controller.AddProject(newProject);
        Global.StopParse = false;
    }

    private async void MenuItem_AddProject_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var folders = await Global.mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Project Folder",
            AllowMultiple = false
        });

        if (folders.Count != 1)
        {
            Controller.AppendLog("no folders found");
            return;
        }

        IStorageFolder folder = folders[0];


        Global.StopParse = true;
        Controller.AppendLog("AddProject" + folder.Path.AbsolutePath,Avalonia.Media.Colors.DarkOrange);
        string path = folder.Path.AbsolutePath.Replace('/', System.IO.Path.DirectorySeparatorChar);
        Data.Project newProject = Project.Create(path);
        await Controller.AddProject(newProject);
        Global.StopParse = false;

    }

    private void MenuItem_SaveProjects_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Global.Setup.SaveSetup(setupFileName);
    }

    // View controller interface //////////////////////////////////////////


    //internal System.Windows.Forms.MenuStrip Controller_GetMenuStrip()
    //{
    //    return menuStrip;
    //}

    //// tabs
    //internal void Controller_AddTabPage(ajkControls.TabControl.TabPage tabPage)
    //{
    //    mainTab.TabPages.Add(tabPage);
    //}

    //internal void Controller_RemoveTabPage(ajkControls.TabControl.TabPage tabPage)
    //{
    //    mainTab.TabPages.Remove(tabPage);
    //}

    //// code editor

    //internal void Controller_RefreshCodeEditor()
    //{
    //    if (InvokeRequired)
    //    {
    //        editorPage.CodeEditor.Invoke(new Action(editorPage.CodeEditor.Refresh));
    //    }
    //    else
    //    {
    //        editorPage.CodeEditor.Refresh();
    //    }
    //}

    //////////////
    //private void addProject(Models.Editor.Data.Project project)
    //{
    //    Models.Common.Global.navigateView.AddProject(project);
    //    //Tools.ParseProjectForm pform = new Tools.ParseProjectForm(navigatePanel.GetPeojectNode(project.Name));
    //    //pform.ShowDialog(this);
    //}

}
