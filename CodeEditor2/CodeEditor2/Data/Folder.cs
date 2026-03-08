using CodeEditor2.NavigatePanel;
using CodeEditor2.Tools;
using DynamicData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    public class Folder : Item
    {
        protected Folder() : base()
        { }
        public static Folder Create(string relativePath, Project project, Item parent)
        {
            string name;
            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                name = relativePath;
            }

            Folder folder = new Folder()
            {
                Name = name,
                Project = project,
                RelativePath = relativePath
            };


            folder.Parent = parent;
            //project.RegisterProjectItem(folder);

            return folder;
        }

        public bool Link { get; set; } = false;
        public File? SearchFile(string relativePath)
        {
            string[] pathList = relativePath.Split(new char[] { System.IO.Path.DirectorySeparatorChar });
            if (pathList.Length == 0) return null;

            foreach (Item item in items.Values)
            {
                File? file = item as File;
                if (file != null)
                {
                    if (file.Name == pathList[0] && pathList.Length == 1) return file;
                    return null;
                }

                Folder? folder = item as Folder;
                if (folder != null &&  folder.Name == pathList[0])
                {
                    if (pathList.Length == 1) return null;
                    return folder.SearchFile(relativePath.Substring(pathList[0].Length + 1));
                }
            }
            return null;
        }
        public File? SearchFile(Func<File, bool> match)
        {
            foreach (Item item in items.Values)
            {
                File? file = item as File;
                if (file != null)
                {
                    if (match(file)) return file;
                    continue;
                }

                Folder? folder = item as Folder;
                if(folder != null)
                {
                    File? folderFile = folder.SearchFile(match);
                    if (folderFile != null) return folderFile;
                    continue;
                }
            }
            return null;
        }

        private bool firstAccess = true;
        //非同期待機
        private readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);
        public override async Task UpdateAsync()
        {
            //await _fileSemaphore.WaitAsync();
            // 待ち時間 0 でトライ。入れなければ false が返る
            if (!await _fileSemaphore.WaitAsync(0))
            {
                // すでに実行中のため、何もせずリターン
                return;
            }
            try
            {
                string absolutePath = Project.GetAbsolutePath(RelativePath);

                // get folder contents
                List<string> absoluteFilePaths = new List<string>();
                List<string> absoluteFolderPaths = new List<string>();
                try
                {
                    (absoluteFilePaths, absoluteFolderPaths) = await DataAccess.GetFolderContents(Project, RelativePath);
                }
                catch
                {
                    // path is not exist
                    IsDeleted = true;
                    Items.Clear();
                    Remove();
                    return;
                }
                await updateItems(absoluteFilePaths, absoluteFolderPaths);
            }
            finally
            {
               _fileSemaphore.Release();
            }
        }

        public override Task PostUIUpdateAsync()
        {
            Task.Run(async () =>
            {
                if (NavigatePanelNode != null) await NavigatePanelNode.UpdateAsync();
                foreach (Item item in Items.Values)
                {
                    await item.PostUIUpdateAsync();
                }
            });
            return Task.CompletedTask;
        }

        private async Task updateItems(List<string> absoluteFilePaths,List<string> absoluteFolderPaths)
        {
            firstAccess = false;

            List<Item> currentItems = new List<Item>();

            // add new files
            List<Task> tasks = new List<Task>();
            foreach (string absoluteFilePath in absoluteFilePaths)
            {
                string relativePath = Project.GetRelativePath(absoluteFilePath);
                string name;
                if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
                {
                    name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
                }
                else
                {
                    name = relativePath;
                }

                Project? project = this as Project;
                if (project != null && project.ignoreList.Contains(name))
                {
                    continue;
                }

                if (items.ContainsKey(name))
                {
                    if (items[name] is File)
                    {
                        ((File)items[name]).CheckFileType();
                    }
                    if (items[name].IsDeleted) continue;
                    currentItems.Add(items[name]);
                    continue;
                }

                tasks.Add(Task.Run(async () =>
                {
                    File item = await File.CreateAsync(Project.GetRelativePath(absoluteFilePath), Project, this);

                    lock (items) { items.Add(item.Name, item); }
                    lock (currentItems) { currentItems.Add(item); }
                }));
                
            }
            await Task.WhenAll(tasks);

            // add new folders
            foreach (string absoluteFolderPath in absoluteFolderPaths)
            {
                // skip invisible folder
                string body = absoluteFolderPath;
                if (body.Contains(System.IO.Path.DirectorySeparatorChar)) body = body.Substring(body.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
                if (body.StartsWith(".")) continue;

                if (items.ContainsKey(body))
                {
                    currentItems.Add(items[body]);
                    continue;
                }

                Folder folder = Create(Project.GetRelativePath(absoluteFolderPath), Project, this);
                Project? project = this as Project;
                if (project != null && project.ignoreList.Contains(folder.Name))
                {
                    continue;
                }
                items.Add(folder.Name, folder);
                currentItems.Add(folder);
                await folder.UpdateAsync();
            }

            // remove unused items
            List<Item> removeItems = new List<Item>();
            foreach (Item item in items.Values)
            {
                if (!currentItems.Contains(item))
                {
                    removeItems.Add(item);
                }
            }

            foreach (Item item in removeItems)
            {
                item.IsDeleted = true;
                items.Remove(item.Name);
            }

            items.Sort((a, b) =>
            {
                return string.Compare(a.Name, b.Name);
            });

        }

        public async Task InitializeHierarchy(Project project, Item parent, FileIO.FileNode node)
        {
            List<string> absoluteFilePaths = new List<string>();
            List<string> absoluteFolderPaths = new List<string>();
            foreach(FileIO.FileNode subNode in node.Nodes.Values)
            {
                if (subNode.IsDirectory)
                {
                    absoluteFolderPaths.Add(subNode.Name);
                }
                else
                {
                    absoluteFilePaths.Add(subNode.Name);
                }
            }
            await updateItems(absoluteFilePaths, absoluteFolderPaths);

            foreach (FileIO.FileNode subNode in node.Nodes.Values)
            {
                if (!subNode.IsDirectory) continue;
                if (items.ContainsKey(subNode.Name))
                {
                    Folder? folder = items[subNode.Name] as Folder;
                    if (folder == null) continue;
                    await folder.InitializeHierarchy(project, this, subNode);
                }
            }

        }


        protected override NavigatePanelNode CreateNode()
        {
            return new FolderNode(this);
        }
    }
}
