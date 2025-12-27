using Avalonia.Controls;
using Avalonia.Media;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using CodeEditor2.Setups;
using ExCSS;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2
{
    public static class Global
    {
        public static Dictionary<string, FileTypes.FileType> FileTypes = new Dictionary<string, FileTypes.FileType>();
        public static Dictionary<string, Project> Projects = new Dictionary<string, Project>();
        public static Dictionary<string, CodeEditor2Plugin.IPlugin> Plugins = new Dictionary<string, CodeEditor2Plugin.IPlugin>();


        public static Dictionary<string, Func<JsonElement, JsonSerializerOptions, ProjectProperty.Setup>> ProjectPropertyDeserializers
            = new Dictionary<string, Func<JsonElement, JsonSerializerOptions, ProjectProperty.Setup>>();

        public static Solution Solution = new Solution();
        public static Setup Setup = new Setup();

        internal static Views.CodeView codeView;
        internal static Views.MainView mainView;
        internal static Views.NavigateView navigateView;
        internal static Views.MainWindow mainWindow;
        internal static Views.LogView logView;
        internal static Views.InfoView infoView;

        public static ObjectIDGenerator ObjectIDGenerator = new ObjectIDGenerator();

        internal static Window currentWindow;

        public static CodeDrawStyle DefaultDrawStyle = new CodeDrawStyle();

        public static System.Threading.Thread? UIThread = null;

        public static bool Abort = false;

        public static int count = 0;

        public static bool StopParse = false;
        public static bool ActivateCashe = false;


        public static MenuItem CreateMenuItem (string header,string name)
        {
            MenuItem menuItem = new MenuItem();
            menuItem.Header = header;
            menuItem.Name = name;
            menuItem.FontFamily = "Cascadia Mono,Consolas,Menlo,Monospace";
//            menuItem.FontSize = 11;
            menuItem.FontStyle = Avalonia.Media.FontStyle.Normal;
            menuItem.FontWeight = Avalonia.Media.FontWeight.Normal;
            menuItem.MinHeight = 12;
//            menuItem.Height = 16;
            menuItem.Padding = new Avalonia.Thickness(0, 0, 0, 0);
            menuItem.Margin = new Avalonia.Thickness(0, 0, 0, 0);
            return menuItem;
        }

        public static MenuItem CreateMenuItem(string header, string name,string imagePath,Avalonia.Media.Color iconColor)
        {
            return CreateMenuItem(
                header, 
                name,
                AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                        imagePath,
                        iconColor
                        ), 
                iconColor
                );
        }
        public static MenuItem CreateMenuItem(string header, string name, Avalonia.Media.IImage imageSource, Avalonia.Media.Color iconColor)
        {
            MenuItem menuItem = CreateMenuItem(header, name);
            Image image = new Image();
            RenderOptions.SetBitmapInterpolationMode(image, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
            image.Source = imageSource;
            image.Margin = new Avalonia.Thickness(2, 2, 2, 2);
            //            image.Width = 12;
            //            image.Height = 12;
            menuItem.Icon = image;

            return menuItem;
        }
 
    }
}
