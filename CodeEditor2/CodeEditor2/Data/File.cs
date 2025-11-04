using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using CodeEditor2.FileTypes;
using CodeEditor2.NavigatePanel;
using DynamicData.Binding;
using DynamicData.Kernel;
using Svg;


namespace CodeEditor2.Data
{
    public class File : Item
    {
        protected File() : base() 
        {
            FileWeakReferences.Add(new WeakReference<File>(this));
        }

        public static List<WeakReference<File>> FileWeakReferences = new List<WeakReference<File>>();

        public static File Create(string relativePath, Project project, Item parent)
        {
            FileTypes.FileType? fileType = project.GetFileType(relativePath);
            if(fileType != null)
            {
                File file = fileType.CreateFile(relativePath, project);
                file.FileType = fileType;
                return file;
            }

            // undefined file
            string name;
            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                name = relativePath;
            }

            File fileItem = new File()
            {
                Project = project,
                RelativePath = relativePath,
                Name = name
            };

            fileItem.Parent = parent;

            if (FileCreated != null) FileCreated(fileItem);
            return fileItem;
        }

        public void CheckFileType()
        {
            FileTypes.FileType? fileType = Project.GetFileType(RelativePath);
            if (FileType != fileType)
            {
                DisposeRequested = true;
            }
        }
        public FileTypes.FileType? FileType { get; set; }
        public string AbsolutePath
        {
            get
            {
                return Project.GetAbsolutePath(RelativePath);
            }
        }

        public bool IsSameAs(File file)
        {
            if (RelativePath != file.RelativePath) return false;
            if (Project != file.Project) return false;
            return true;
        }

        public static Action<File>? FileCreated;

        protected override NavigatePanelNode CreateNode()
        {
            return new FileNode(this);
        }

        public override void Update()
        {
            base.Update();
        }

    }
}
