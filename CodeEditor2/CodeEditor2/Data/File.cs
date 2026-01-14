using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.FileTypes;
using CodeEditor2.NavigatePanel;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using Svg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


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
                IsDeleted = true;
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
        public record FileStatus(long Size, DateTime LastWriteTimeUtc);
        public FileStatus? CashedStatus { get; set; } = null;


        public override async Task UpdateAsync()
        {
            await base.UpdateAsync();
            if (!System.IO.File.Exists(AbsolutePath))
            {
                IsDeleted = true;
                await OnDeletedExternallyAsync();
                return;
            }

            var info = new FileInfo(AbsolutePath);
            var newState = new FileStatus(info.Length, info.LastWriteTimeUtc);
            if (CashedStatus == null)
            {
                CashedStatus = newState;
            }
            else
            {
                if(CashedStatus.LastWriteTimeUtc < newState.LastWriteTimeUtc || CashedStatus.Size != newState.Size)
                {
                    await OnChangedExternallyAsync();
                    CashedStatus = newState;
                }
            }
        }

        public virtual Task OnDeletedExternallyAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnChangedExternallyAsync()
        {
            return Task.CompletedTask;
        }

    }
}
