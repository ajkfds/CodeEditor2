﻿using ExCSS;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.Setups;
using Avalonia.Controls;
using System.Threading;

namespace CodeEditor2
{
    public static class Global
    {
        public static Dictionary<string, FileTypes.FileType> FileTypes = new Dictionary<string, FileTypes.FileType>();
        public static Dictionary<string, Project> Projects = new Dictionary<string, Project>();
        public static Dictionary<string, CodeEditor2Plugin.IPlugin> Plugins = new Dictionary<string, CodeEditor2Plugin.IPlugin>();

        public static Setup Setup = new Setup();
        //public static Dictionary<string, CodeEditor2Plugin.PluginSetup> PluginSetups = new Dictionary<string, CodeEditor2Plugin.PluginSetup>();
        internal static Views.CodeView codeView;
        internal static Views.MainView mainView;
        internal static Views.NavigateView navigateView;
        internal static Views.MainWindow mainWindow;
        internal static Views.LogView logView;
        internal static Views.InfoView infoView;

        internal static Window currentWindow;

        public static CodeDrawStyle DefaultDrawStyle = new CodeDrawStyle();

        public static System.Threading.Thread UIThread = null;

        public static bool Abort = false;

        public static Semaphore ParseSemaphore = new Semaphore(1, 1);

        //public static IWshRuntimeLibrary.WshShell WshShell = new IWshRuntimeLibrary.WshShell();

        //public static class IconImages
        //{
        //    public static ajkControls.Primitive.IconImage Terminal = new ajkControls.Primitive.IconImage(Properties.Resources.terminal);
        //    public static ajkControls.Primitive.IconImage Text = new ajkControls.Primitive.IconImage(Properties.Resources.text);
        //    public static ajkControls.Primitive.IconImage SaveFile = new ajkControls.Primitive.IconImage(Properties.Resources.saveFile);
        //    public static ajkControls.Primitive.IconImage Wave0 = new ajkControls.Primitive.IconImage(Properties.Resources.wave0);
        //    public static ajkControls.Primitive.IconImage Wave1 = new ajkControls.Primitive.IconImage(Properties.Resources.wave1);
        //    public static ajkControls.Primitive.IconImage Wave2 = new ajkControls.Primitive.IconImage(Properties.Resources.wave2);
        //    public static ajkControls.Primitive.IconImage Wave3 = new ajkControls.Primitive.IconImage(Properties.Resources.wave3);
        //    public static ajkControls.Primitive.IconImage Wave4 = new ajkControls.Primitive.IconImage(Properties.Resources.wave4);
        //    public static ajkControls.Primitive.IconImage Wave5 = new ajkControls.Primitive.IconImage(Properties.Resources.wave5);
        //    public static ajkControls.Primitive.IconImage Play = new ajkControls.Primitive.IconImage(Properties.Resources.play);
        //    public static ajkControls.Primitive.IconImage Pause = new ajkControls.Primitive.IconImage(Properties.Resources.pause);
        //    public static ajkControls.Primitive.IconImage Git = new ajkControls.Primitive.IconImage(Properties.Resources.tree);
        //    public static ajkControls.Primitive.IconImage NewBadge = new ajkControls.Primitive.IconImage(Properties.Resources.newBadge);
        //    public static ajkControls.Primitive.IconImage IgnoreBadge = new ajkControls.Primitive.IconImage(Properties.Resources.ignore);
        //    public static ajkControls.Primitive.IconImage Link = new ajkControls.Primitive.IconImage(Properties.Resources.link);
        //}

        //public static class ColorMap
        //{
        //    public static System.Drawing.Color DarkBackground = System.Drawing.Color.FromArgb(0x20, 0x38, 0x64);
        //    public static System.Drawing.Color LightBackground = System.Drawing.Color.FromArgb(0x32, 0x59, 0xa0);
        //    public static System.Drawing.Color SelectedBackground = System.Drawing.Color.FromArgb(32, 56, 100); //;System.Drawing.Color.FromArgb(0xa9, 0xba, 0xda);
        //    public static System.Drawing.Color Foreground = System.Drawing.Color.White;
        //}

        //// for debug
        //public static System.Runtime.Serialization.ObjectIDGenerator IdGenerator = new System.Runtime.Serialization.ObjectIDGenerator();
        public static bool StopParse = false;
    }
}
