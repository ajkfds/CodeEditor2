using AjkAvaloniaLibs.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CodeEditor2.Data;
using CodeEditor2.Setups;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace CodeEditor2.Views;

public partial class MainView : UserControl
{
    ////private const string setupFileName = "CodeEditor2.json";

    public MainView()
    {
        Global.mainView = this;
        InitializeComponent();
//        DataContext = new ViewModels.MainViewModel();

        SplitterColumn1.BorderBrush = new SolidColorBrush(Color.FromArgb(255,50,50,50));
        SplitterColumn1.BorderThickness = new Thickness(1, 0, 1, 0);

        {
            Image image = new Image();
            image.Source = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2/Assets/Icons/leftArrow.svg", Avalonia.Media.Colors.White);
            image.Width = FontSize;
            image.Height = FontSize;
            RenderOptions.SetBitmapInterpolationMode(image, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
            Button backButton = new Button() { Content = image, Background = this.Background};
            backButton.Click += BackButton_Click;
            ToolBar.Children.Add(backButton);
            {
                Image dropdownImage = new Image();
                dropdownImage.Source = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2/Assets/Icons/dropdown.svg", Avalonia.Media.Colors.White);
                dropdownImage.Width = FontSize;
                dropdownImage.Height = FontSize;
                RenderOptions.SetBitmapInterpolationMode(dropdownImage, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
                
                var backDropdownButton = new Button { Content = dropdownImage, Height = FontSize * 2, Background = this.Background };
                backDropdownButton.Margin = new Thickness(0, backDropdownButton.Margin.Top, 0, backDropdownButton.Margin.Bottom);
                backDropdownButton.Padding = new Thickness(0, backDropdownButton.Padding.Top, 0, backDropdownButton.Padding.Bottom);
                backDropdownButton.Click += BackDropdownButton_Click;
                backDropdownButton.ContextMenu = backContectMenu;
                ToolBar.Children.Add(backDropdownButton);
            }
        }
        {
            Image image = new Image();
            image.Source = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2/Assets/Icons/rightArrow.svg", Avalonia.Media.Colors.White);
            image.Width = FontSize;
            image.Height = FontSize;
            RenderOptions.SetBitmapInterpolationMode(image, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
            Button forwardBtn = new Button() { Content = image, Background = this.Background };
            forwardBtn.Click += ForwardBtn_Click;
            ToolBar.Children.Add(forwardBtn);
        }

        //        TabControl0.
        initializeMenuItem_File();

        timer.Interval = new TimeSpan(1);
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private void BackDropdownButton_Click(object? sender, RoutedEventArgs e)
    {
        backContectMenu.Items.Clear();
        foreach (TextReference textRef in selectHistories)
        {
            MenuItem menuItem = new MenuItem();
            ITextFile? textFile = textRef.TextFile;
            if (textFile == null) continue; // skip if text file is not available
            menuItem.Header = textRef.Caption;
            menuItem.Margin = new Thickness(2, 2, 2, 2);
            menuItem.Padding = new Thickness(2, 2, 2, 2);
            menuItem.Click += (s, e) =>
            {
                Controller.SelectText(textRef);
                currentHistoryIndex = selectHistories.IndexOf(textRef);
                backContectMenu.Close();
            };
            backContectMenu.Items.Add(menuItem);
        }

        backContectMenu.Open();
    }

    private ContextMenu backContectMenu = new ContextMenu();

    private void ForwardBtn_Click(object? sender, RoutedEventArgs e)
    {
        int index = currentHistoryIndex - 1;
        if(index < 0 || selectHistories.Count <= index) return;
        Controller.SelectText(selectHistories[index]);
        currentHistoryIndex--;
    }
   
    private void BackButton_Click(object? sender, RoutedEventArgs e)
    {
        int index = currentHistoryIndex+1;
        if (selectHistories.Count <= index) return;
        Controller.SelectText(selectHistories[index]);
        currentHistoryIndex++;
    }

    private Popup backDropdownMenu;

    private List<TextReference>　selectHistories = new List<TextReference>();
    private const int selectHistoryMaxCount = 20;
    private int currentHistoryIndex = -1;
    public void AddSelectHistory(TextReference textReference)
    {
        for(int i = 0;i<=currentHistoryIndex; i++)
        {
            selectHistories.RemoveAt(0);
        }
        currentHistoryIndex = -1; // reset current index when new history is added
        selectHistories.Insert(0,textReference);
        if(selectHistories.Count > selectHistoryMaxCount)
        {
            selectHistories.RemoveAt(0);
        }
    }

    private async void Timer_Tick(object? sender, EventArgs e)
    {
        // should launch after main window shown
        timer.Stop();
        // read setup file

        await initialize();
    }

    private DispatcherTimer timer = new DispatcherTimer();

    private const string setupFileName = "CodeEditor2.json";

    private async Task initialize()
    {
        try
        {
            if(!System.IO.File.Exists(setupFileName))
            {
                return;
            }
            await Global.Setup.LoadSetup(setupFileName);
        }
        catch(Exception ex)
        {
            throw;
        }
    }


    //private async void MenuItem_AddProjectPath_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    //{
    //    Tools.InputWindow inputWindow = new Tools.InputWindow("New Project Name","Input Project Absolute Path");
    //    await inputWindow.ShowDialog(Global.mainWindow);
    //    if (inputWindow.Cancel) return;
    //    string path = inputWindow.InputText;
    //    if (!System.IO.Directory.Exists(path))
    //    {
    //        Controller.AppendLog("no folders found");
    //        return;
    //    }
    //    Global.StopParse = true;
    //    Controller.AppendLog("AddProject" + path, Avalonia.Media.Colors.DarkOrange);
    //    path = path.Replace('/', System.IO.Path.DirectorySeparatorChar);
    //    Data.Project newProject = Project.Create(path);
    //    await Controller.AddProject(newProject);
    //    Global.StopParse = false;
    //}


//    private Views.BrowserWindow browserWindow;
    private async void MenuItem_Test_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
//        browserWindow = new BrowserWindow();
//        await browserWindow.ShowDialog(Global.mainWindow);
    }
    private async void MenuItem_Browser_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
//        Tools.BrowserWindow browserWindow = new Tools.BrowserWindow();
//        browserWindow.Show();
        //Global.mainWindow);
        //        browserWindow = new BrowserWindow();
        //        await browserWindow.ShowDialog(Global.mainWindow);
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

        Controller.AppendLog("AddProject" + folder.Path.AbsolutePath,Avalonia.Media.Colors.DarkOrange);
        string path;
        {
            path = folder.Path.AbsolutePath;
            path = System.Web.HttpUtility.UrlDecode(path);
            path = path.Replace('/', System.IO.Path.DirectorySeparatorChar);
        }

        Data.Project newProject = Project.Create(path);
        await Controller.AddProject(newProject);
    }

    // MenuItem File
    private void initializeMenuItem_File()
    {
        MenuItem_File_Open.Click += MenuItem_File_Open_Click;

        {
            MenuItem_File_Save.Click += MenuItem_File_Save_Click;
            MenuItem_File_Save.InputGesture = new KeyGesture(Key.S, KeyModifiers.Control);

            Image image = new Image();
            image.Source = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                        "CodeEditor2/Assets/Icons/tag.svg",
                        Avalonia.Media.Color.FromArgb(100, 100, 100, 100)
                        );
            image.Width = 12;
            image.Height = 12;
            MenuItem_File_Save.Icon = image;
        }

        MenuItem_File_SaveProjects.Click += MenuItem_File_SaveProjects_Click;

    }
    private void MenuItem_File_Open_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void MenuItem_File_SaveProjects_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Global.Setup.SaveSetup(setupFileName);
    }

    private void MenuItem_File_Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Controller.CodeEditor.Save();
        Controller.AppendLog("Saved");
    }
    private void MenuItem_File_Exit_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void MenuItem_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
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


}
