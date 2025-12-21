using CodeEditor2.Setups;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json.Serialization;
using System.Timers;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using CodeEditor2.FileTypes;
using Avalonia.Threading;
using System.Threading;

namespace CodeEditor2.Data
{
    /// <summary>
    /// Project item object. 
    /// It is associated with a specific folder and treats the files/folders under that folder as project items. 
    /// It maintains the setup for each project and has the functionality to save the project's setup to a JSON file.
    /// </summary>
    public class Project : Folder
    {
        [SetsRequiredMembers]
        protected Project(string name,string rootPath,string relativePath) :base()
        {
            Name = name;
            RootPath = rootPath;
            RelativePath = relativePath;
            Project = this;
            FileClassify = new FileClassify(this);
        }

        public Dictionary<string, ProjectProperty> ProjectProperties = new Dictionary<string, ProjectProperty>();
        public required string RootPath { get; init; }

        public Setup CreateSetup()
        {
            Setup setup = new Setup(this);
            return setup;
        }

        [Newtonsoft.Json.JsonIgnore]
        public FileClassify FileClassify { get; set; }

        // setup object to convert project to json file
        public class Setup
        {
            [JsonConstructor]
            public Setup() { }

            [SetsRequiredMembers]
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
            public required string Name;
            [JsonInclude]
            public required string RootPath;
            [JsonInclude]
            public required List<string> IgnoreList;
            [JsonInclude]
            public Dictionary<string, ProjectProperty.Setup> ProjectProperties = new Dictionary<string, ProjectProperty.Setup>();
        }

        public static Project Create(string rootPath)
        {
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

            string name;
            if (actualPath.Contains(Path.DirectorySeparatorChar))
            {
                name = actualPath.Substring(actualPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                name = actualPath;
            }

            Project project = new Project(name, actualPath, "");

            initProject(project,null);
            return project;
        }

        public static Project Create(Setup setup)
        {
            Project project = new Project(setup.Name, setup.RootPath, "");

            project.ignoreList = setup.IgnoreList;

            initProject(project,setup);
            return project;
        }

        public FileTypes.FileType? GetFileType(string relativePath)
        {
            // check registered filetype
            string? fileTypeKey = null;
            foreach (var fileType in Global.FileTypes)
            {
                if (fileType.Value.IsThisFileType(relativePath, this))
                {
                    fileTypeKey = fileType.Key;
                }
            }

            if (FileClassify.HasDefinition())
            {
                fileTypeKey = FileClassify.GetFileType(relativePath, fileTypeKey);
            }

            if (fileTypeKey != null && Global.FileTypes.ContainsKey(fileTypeKey))
            {
                FileTypes.FileType fileType = Global.FileTypes[fileTypeKey];
                return fileType;
            }
            return null;
        }

        public static Action<Project,Setup?>? Created;
        private static void initProject(Project project,Setup? setup)
        {
            if (Created != null) Created(project,setup);
            project.startFileSystemWatcher();
        }

        public override void Dispose()
        {
            stopFileSystemWatcher();
            base.Dispose();
        }

        private void stopFileSystemWatcher()
        {
            if (fileSystemWatcher == null) return;
            fileSystemWatcher.EnableRaisingEvents = false;
            fileSystemWatcher.Dispose();
            fileSystemWatcher = null;
        }


        public List<string> ignoreList = new List<string>();



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


        // file system watcher ////////////////////////////////////////////////////////////////////////////
        // detect file change and raise events

        #region FileSystemWatcher

        protected FileSystemWatcher? fileSystemWatcher;
        protected void startFileSystemWatcher()
        {
            fileSystemWatcher = new FileSystemWatcher();
            fileSystemWatcher.Path = RootPath;
            if (System.OperatingSystem.IsWindows())
            {
                fileSystemWatcher.NotifyFilter =
                    NotifyFilters.LastWrite
                    | NotifyFilters.FileName
                    | NotifyFilters.DirectoryName;
            }
            fileSystemWatcher.Filter = "";
            fileSystemWatcher.IncludeSubdirectories = true;

            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
            fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;

            fileSystemWatcher.EnableRaisingEvents = true;
        }


        private System.Threading.Timer changeDebounceTimer;
        private const int debounceTime_ms = 100;
        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath.Contains(System.IO.Path.DirectorySeparatorChar + ".git" + System.IO.Path.DirectorySeparatorChar)) return;
            if (e.FullPath.Contains(System.IO.Path.DirectorySeparatorChar + ".cashe" + System.IO.Path.DirectorySeparatorChar)) return;

            changeDebounceTimer?.Dispose();
            changeDebounceTimer = new System.Threading.Timer( _ =>
            {
                FileChanged(sender, e);
            }, null, 100, Timeout.Infinite);
        }

        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
        }

        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            changeDebounceTimer?.Dispose();
            changeDebounceTimer = new System.Threading.Timer(_ =>
            {
                FileDeleted(sender, e);
            }, null, 100, Timeout.Infinite);
        }

        private async void FileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (e.FullPath == FileClassify.AbsolutePath)
                {
                    FileClassify.Reload();
                    return;
                }

                Controller.AppendLog(e.Name + " changed");
                string relativePath = GetRelativePath(e.FullPath);
                Data.File? file = GetItem(relativePath) as Data.File;
                if (file == null) return;
                Data.ITextFile? textFile = file as Data.ITextFile;
                if (textFile == null) return;
                await Dispatcher.UIThread.InvokeAsync(async () => { await fileChaned(textFile); });
            }
            catch
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }
        }
        private async Task fileChaned(Data.ITextFile textFile)
        {
            if (textFile.Dirty)
            {
                Controller.AppendLog(textFile.RelativePath + " file edit conflict!", Avalonia.Media.Colors.Red);
                await textFile.UpdateAsync();
            }
            else
            {
                await textFile.UpdateAsync();
                //DateTime lastWriteTime = System.IO.File.GetLastWriteTime(GetAbsolutePath(textFile.RelativePath));
                //if (textFile.LoadedFileLastWriteTime != lastWriteTime)
                //{
                //    textFile.LoadFormFile();

                //    // fire and forget
                //}
            }
        }

        private void FileDeleted(object sender, FileSystemEventArgs e)
        {
            Controller.AppendLog(e.Name + " deleted");
            string relativePath = GetRelativePath(e.FullPath);
            Data.File? file = GetItem(relativePath) as Data.File;
            if (file == null) return;
            file.IsDeleted = true;
            Data.ITextFile? textFile = file as Data.ITextFile;
            if (textFile == null) return;
            Dispatcher.UIThread.Post(() => fileChaned(textFile));
        }
        #endregion


    }
}