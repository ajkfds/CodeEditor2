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

        Controller.NavigatePanel.OpenInExploererClicked += menuItem_OpenInExplorer_Click;

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
    private void menuItem_OpenInExplorer_Click(NavigatePanel.NavigatePanelNode node)
    {
        if (node is NavigatePanel.FolderNode)
        {
            NavigatePanel.FolderNode? folderNode = node as NavigatePanel.FolderNode;
            if (folderNode == null) throw new System.Exception();
            Data.Folder? folder = folderNode.Folder;
            if (folder == null || folder.Project == null) return;
            string folderPath = folder.Project.GetAbsolutePath(folder.RelativePath).Replace('\\', System.IO.Path.DirectorySeparatorChar);

            if (System.OperatingSystem.IsLinux())
            {
                System.Diagnostics.Process.Start("nautilus " + folderPath + " &");
            }
            else
            {
                System.Diagnostics.Process.Start("EXPLORER.EXE", folderPath);
            }
        }
        else if (node is NavigatePanel.FileNode)
        {
            NavigatePanel.FileNode? fileNode = node as NavigatePanel.FileNode;
            if (fileNode == null) throw new System.Exception();
            Data.File? file = fileNode.FileItem;
            if (file == null || file.Project == null) return;
            string filePath = file.Project.GetAbsolutePath(file.RelativePath).Replace('\\', System.IO.Path.DirectorySeparatorChar);

            if (System.OperatingSystem.IsLinux())
            {
            }
            else
            {
                System.Diagnostics.Process.Start("EXPLORER.EXE", "/select,\"" + file.Project.GetAbsolutePath(file.RelativePath) + "\"");
            }
        }
    }

}
