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

            Folder folder = new Folder();
            folder.Project = project;
            folder.RelativePath = relativePath;

            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                folder.Name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                folder.Name = relativePath;
            }

            folder.Parent = parent;
            //project.RegisterProjectItem(folder);

            return folder;
        }

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
                    return null;
                }

                Folder? folder = item as Folder;
                if(folder != null)
                {
                    return folder.SearchFile(match);
                }
            }
            return null;
        }

        public override void Update()
        {
            string absolutePath = Project.GetAbsolutePath(RelativePath);

            // get folder contents
            string[] absoluteFilePaths;
            try
            {
                absoluteFilePaths = System.IO.Directory.GetFiles(absolutePath);
            }
            catch
            {
                // path is not exist

                System.Diagnostics.Debugger.Break();
                return;
            }
            string[] absoluteFolderPaths = System.IO.Directory.GetDirectories(absolutePath);

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

                if (!items.ContainsKey(name))
                {
                    //if (absoluteFilePath.EndsWith(".lnk")) // windows link file
                    //{
                    //    Link item = Link.Create(Project.GetRelativePath(absoluteFilePath), Project, this);
                    //    if(item != null) items.Add(item.Name, item);
                    //}
                    //else
                    {
                        File item = File.Create(Project.GetRelativePath(absoluteFilePath), Project, this);
                        items.Add(item.Name, item);
                    }
                }
            }

            // add new folders
            foreach (string absoluteFolderPath in absoluteFolderPaths)
            {
                // skip invisible folder
                string body = absoluteFolderPath;
                if (body.Contains(System.IO.Path.DirectorySeparatorChar)) body = body.Substring(body.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
                if (body.StartsWith(".")) continue;

                if (!items.ContainsKey(body))
                {
                    Folder item = Create(Project.GetRelativePath(absoluteFolderPath), Project, this);
                    Project? project = this as Project;
                    if (project != null && project.ignoreList.Contains(item.Name))
                    {
                        continue;
                    }
                    items.Add(item.Name, item);
                    item.Update();
                }
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
                {
                    string absoluteItemPath = Project.GetAbsolutePath(item.RelativePath);
                    if (!absoluteFilePaths.Contains(absoluteItemPath) && !absoluteFolderPaths.Contains(absoluteItemPath))
                    {
                        removeItems.Add(item);
                    }
                }
            }

            foreach (Item item in removeItems)
            {
                items.Remove(item.Name);
                item.Dispose();
            }

        }

        protected override NavigatePanelNode createNode()
        {
            return new FolderNode(this);
        }
    }
}
