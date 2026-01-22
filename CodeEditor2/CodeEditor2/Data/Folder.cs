using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.NavigatePanel;

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

        public override async Task UpdateAsync()
        {
            string absolutePath = Project.GetAbsolutePath(RelativePath);

            // get folder contents
            string[] absoluteFilePaths = new string[] { };
            try
            {
                await Task.Run(() =>
                {
                    absoluteFilePaths = System.IO.Directory.GetFiles(absolutePath);
                });
            }
            catch
            {
                // path is not exist
                IsDeleted = true;
                Items.Clear();
                return;
            }
            string[] absoluteFolderPaths = System.IO.Directory.GetDirectories(absolutePath);

            List<Item> currentItems = new List<Item>();

            // add new files
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

                if (absoluteFilePath.EndsWith(".lnk")) // windows link file
                {
                    if (System.OperatingSystem.IsWindows())
                    {
                        //// https://github.com/securifybv/ShellLink
                        //Securify.ShellLink.Shortcut shortcut = Securify.ShellLink.Shortcut.ReadFromFile(absoluteFilePath);
                        //string absoluteLinkPath = shortcut.LinkTargetIDList.Path;
                        //if ((shortcut.LinkFlags | Securify.ShellLink.Flags.LinkFlags.HasRelativePath) == 0) // don't have relative path
                        //{ // add relative path
                        //    string basePath = Project.GetAbsolutePath(RelativePath);
                        //    if (!basePath.EndsWith(System.IO.Path.DirectorySeparatorChar)) basePath += System.IO.Path.DirectorySeparatorChar;
                        //    Uri u1 = new Uri(basePath);
                        //    Uri u2 = new Uri(absoluteLinkPath);
                        //    Uri relativeUri = u1.MakeRelativeUri(u2);
                        //    string relativeLinkPath = Uri.UnescapeDataString(relativeUri.ToString());
                        //    shortcut.LinkFlags |= Securify.ShellLink.Flags.LinkFlags.HasRelativePath;
                        //    shortcut.StringData.WorkingDir = ".";
                        //    shortcut.StringData.RelativePath = relativeLinkPath;
                        //    shortcut.WriteToFile(absoluteFilePath);
                        //}

                        //if (shortcut.FileAttributes.HasFlag(Securify.ShellLink.Flags.FileAttributesFlags.FILE_ATTRIBUTE_DIRECTORY))
                        //{ // directory
                        //    Folder item = Create(Project.GetRelativePath(absoluteLinkPath), Project, this);
                        //    item.Link = true;
                        //    if (Project != null && Project.ignoreList.Contains(item.Name))
                        //    {
                        //        continue;
                        //    }
                        //    if (items.ContainsKey(item.Name))
                        //    {
                        //        currentItems.Add(items[item.Name]);
                        //        continue;
                        //    }
                        //    items.Add(item.Name, item);
                        //    currentItems.Add(item);
                        //    item.Update();
                        //    continue;
                        //}
                    }
                }

                {
                    File item = File.Create(Project.GetRelativePath(absoluteFilePath), Project, this);
                    items.Add(item.Name, item);
                    currentItems.Add(item);
                }
            }

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
                //if(item is Link)
                //{
                //    string linkItemPath = Project.GetAbsolutePath((item as Link).LinkRelativePath);
                //    if (!absoluteFilePaths.Contains(linkItemPath) && !absoluteFolderPaths.Contains(linkItemPath))
                //    {
                //        removeItems.Add(item);
                //    }
                //}
                //else
                //{
                //    string absoluteItemPath = Project.GetAbsolutePath(item.RelativePath);
                //    if (!absoluteFilePaths.Contains(absoluteItemPath) && !absoluteFolderPaths.Contains(absoluteItemPath))
                //    {
                //        removeItems.Add(item);
                //    }
                //}
                if (!currentItems.Contains(item))
                {
                    removeItems.Add(item);
                }
            }

            foreach (Item item in removeItems)
            {
                item.IsDeleted = true;
//                items.Remove(item.Name);
//                item.Dispose();
            }

            items.Sort((a, b) =>
            {
                return string.Compare(a.Name, b.Name);
            });
        }

        protected override NavigatePanelNode CreateNode()
        {
            return new FolderNode(this);
        }
    }
}
