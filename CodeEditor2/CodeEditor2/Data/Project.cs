﻿using CodeEditor2.Setups;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json.Serialization;
using System.Timers;
using System.Text.Json;

namespace CodeEditor2.Data
{
    public class Project : Folder
    {
        protected Project() :base() { }

        public Setup CreateSetup()
        {
            return new Setup(this);
        }
        public class Setup
        {
            [JsonConstructor]
            public Setup() { }

            public Setup(Project project)
            {
                this.RootPath = project.RootPath;
                this.Name = project.Name;
                this.IgnoreList = project.ignoreList;

                this.ProjectProperties.Clear();
                foreach(var projectProperty in project.ProjectProperties)
                {
                    this.ProjectProperties.Add(projectProperty.Key,projectProperty.Value.CreateSetup());
                }
            }

            [JsonInclude]
            public string Name;
            [JsonInclude]
            public string RootPath;
            [JsonInclude]
            public List<string> IgnoreList;
            [JsonInclude]
            public Dictionary<string, ProjectProperty.Setup> ProjectProperties = new Dictionary<string, ProjectProperty.Setup>();
        }

        public static Project Create(string rootPath)
        {
            Project project = new Project();
            string path;
            if (rootPath.EndsWith(System.IO.Path.DirectorySeparatorChar))
            {
                path = rootPath;
            }
            else
            {
                path = rootPath + System.IO.Path.DirectorySeparatorChar;
            }
            string? actualPath = System.IO.Path.GetDirectoryName(path);
            if (actualPath == null) throw new Exception();
            if (!System.IO.Directory.Exists(actualPath))
            {
                throw new Exception();
            }

            project.RootPath = actualPath;
            if (project.RootPath.Contains(Path.DirectorySeparatorChar))
            {
                project.Name = project.RootPath.Substring(project.RootPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                project.Name = project.RootPath;
            }

            initProject(project);
            return project;
        }

        public static Project Create(Setup setup)
        {
            Project project = new Project();
            project.Name = setup.Name;
            project.RootPath = setup.RootPath;
            project.ignoreList = setup.IgnoreList;
//            case "PluginProperties":
//               readProjectProperties(reader);
//                break;

                initProject(project);
            return project;
        }

        private static void initProject(Project project)
        {
            project.RelativePath = "";
            project.Project = project;

            if (Created != null) Created(project);
            project.startFileSystemWatcher();
        }

        public override void Dispose()
        {
            stopFileSystemWatcher();
            base.Dispose();
        }

        private void stopFileSystemWatcher()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
            fileSystemWatcher.Dispose();
            fileSystemWatcher = null;
        }

        public static Action<Project>? Created;

        public Dictionary<string, ProjectProperty> ProjectProperties = new Dictionary<string, ProjectProperty>();

        public string RootPath { get; protected set; }
        public new string Name { get; protected set; }

        public List<string> ignoreList = new List<string>();


        // get parse target
        //        private List<Item> parseItems = new List<Item>();

        private Dictionary<string, Item> parseItems = new Dictionary<string, Item>();
        public Item? FetchReparseTarget()
        {
            lock (parseItems)
            {
                if (parseItems.Count == 0) return null;
                string key = parseItems.Keys.First();
                Item item = parseItems[key];

                //if (item == null) return null;
                //while (
                //    (item as TextFile) != null &&
                //    (item as TextFile).ParsedDocument != null &&
                //    (item as TextFile).IsCodeDocumentCashed &&
                //    (item as TextFile).CodeDocument != null &&
                //    (item as TextFile).ParsedDocument.Version == (item as TextFile).CodeDocument.Version
                //    )
                //{
                //    parseItems.Remove(key);
                //    key = parseItems.Keys.First<string>();
                //    item = parseItems[key];
                //    if (item == null) return null;
                //}
                //parseItems.Remove(key);
                //return item;
            }
            return null;
        }

        public void AddReparseTarget(Item item)
        {
            lock (parseItems)
            {
                if (!parseItems.ContainsKey(item.ID))
                {
                    System.Diagnostics.Debug.Print("entry add parse:" + item.ID);
                    parseItems.Add(item.ID, item);
                }
            }
        }

        // path control
        public string GetAbsolutePath(string relativePath)
        {
            string basePath = RootPath;
            if (!basePath.EndsWith(Path.DirectorySeparatorChar)) basePath += Path.DirectorySeparatorChar;
            string filePath = relativePath;

            basePath = basePath.Replace("%", "%25");
            filePath = filePath.Replace("%", "%25");

            Uri u1 = new Uri(basePath);
            Uri u2 = new Uri(u1, filePath);
            string absolutePath = u2.LocalPath;
            absolutePath = absolutePath.Replace("%25", "%");

            return absolutePath;
        }

        public string GetRelativePath(string fullPath)
        {
            string basePath = RootPath;
            if (!basePath.EndsWith(Path.DirectorySeparatorChar)) basePath += Path.DirectorySeparatorChar;
            Uri u1 = new Uri(basePath);
            Uri u2 = new Uri(fullPath);
            Uri relativeUri = u1.MakeRelativeUri(u2);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            relativePath = relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);
            return relativePath;
        }


        // Seve and Load Project Setups ///////////////////////////////////////////////////////////////////
        // save and load project setups into json file

        //#region
        //public void SaveSetup(AjkAvaloniaLibs.Libs.Json.JsonWriter writer)
        //{
        //    writer.writeKeyValue("RootPath", RootPath);
        //    using (var blockWriter = writer.GetObjectWriter("IgnoreList"))
        //    {
        //        foreach (string ignore in ignoreList)
        //        {
        //            blockWriter.writeKeyValue("Ignore", ignore);
        //        }
        //    }

        //    using (var blockWriter = writer.GetObjectWriter("PluginProperties"))
        //    {
        //        foreach (KeyValuePair<string, ProjectProperty> propertyKvp in ProjectProperties)
        //        {
        //            using (var propertyWriter = blockWriter.GetObjectWriter(propertyKvp.Key))
        //            {
        //                propertyKvp.Value.SaveSetup(propertyWriter);
        //            }
        //        }
        //    }
        //}



        //private void readProjectProperties(AjkAvaloniaLibs.Libs.Json.JsonReader jsonReader)
        //{
        //    using (var reader = jsonReader.GetNextObjectReader())
        //    {
        //        while (true)
        //        {
        //            string key = reader.GetNextKey();
        //            if (key == null) break;

        //            if (!ProjectProperties.ContainsKey(key))
        //            {
        //                reader.SkipValue();
        //                continue;
        //            }
        //            ProjectProperty property = ProjectProperties[key];
        //            property.LoadSetup(reader);
        //        }
        //    }
        //}

        //#endregion


        // file system watcher ////////////////////////////////////////////////////////////////////////////
        // detect file change and raise events

        #region FileSystemWatcher

        protected FileSystemWatcher fileSystemWatcher;
        protected Timer fsTimer = new Timer();
        protected void startFileSystemWatcher()
        {
            fileSystemWatcher = new FileSystemWatcher();
            fileSystemWatcher.Path = RootPath;
            fileSystemWatcher.NotifyFilter =
                NotifyFilters.LastWrite
                | NotifyFilters.FileName
                | NotifyFilters.DirectoryName
                ;
            fileSystemWatcher.Filter = "";
            fileSystemWatcher.IncludeSubdirectories = true;

            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
            fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;

            fileSystemWatcher.EnableRaisingEvents = true;

            fsTimer.Interval = 10;
            //            fsTimer.Tick += fsTimer_Tick;
            fsTimer.Start();
        }

        Dictionary<string, FileSystemEventArgs> fileSystemEvents = new Dictionary<string, FileSystemEventArgs>();
        private void addFileSystemEvent(FileSystemEventArgs e)
        {
            lock (fileSystemEvents)
            {
                while (fileSystemEvents.ContainsKey(e.FullPath))
                {
                    FileSystemEventArgs prevE = fileSystemEvents[e.FullPath];
                    switch (prevE.ChangeType)
                    {
                        case WatcherChangeTypes.Changed:
                            fileSystemEvents.Remove(prevE.FullPath);
                            break;
                        case WatcherChangeTypes.Created:
                            fileSystemEvents.Remove(prevE.FullPath);
                            break;
                        case WatcherChangeTypes.Deleted:
                            fileSystemEvents.Remove(prevE.FullPath);
                            break;
                        case WatcherChangeTypes.Renamed:
                            fileSystemEvents.Remove(prevE.FullPath);
                            break;
                    }
                }
                fileSystemEvents.Add(e.FullPath, e);
                //                fsTimer.Enabled = true;
            }
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            addFileSystemEvent(e);
        }

        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            addFileSystemEvent(e);
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            addFileSystemEvent(e);
        }

        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            addFileSystemEvent(e);
        }

        private void fsTimer_Tick(object sender, EventArgs e)
        {
            lock (fileSystemEvents)
            {
                //while (fileSystemEvents.Count != 0)
                //{
                //    System.IO.FileSystemEventArgs fs = fileSystemEvents.Values.FirstOrDefault();
                //    fileSystemEvents.Remove(fs.FullPath);
                //    {
                //        Controller.AppendLog(fs.Name + " changed");
                //        return;
                //        string relativePath = GetRelativePath(fs.FullPath);
                //        Data.File file = GetItem(relativePath) as Data.File;
                //        if (file == null) return;
                //        Data.ITextFile textFile = file as Data.ITextFile;
                //        if (textFile == null) return;
                //        if (textFile.Dirty)
                //        {
                //            Controller.AppendLog(fs.FullPath + " conflict!");
                //        }
                //        else
                //        {
                //            DateTime lastWriteTime = System.IO.File.GetLastWriteTime(fs.FullPath);
                //            if (textFile.LoadedFileLastWriteTime != lastWriteTime)
                //            {
                //                textFile.LoadFormFile();
                //                textFile.Update();
                //            }
                //        }
                //    }

                //}
            }
        }

        #endregion


    }
}