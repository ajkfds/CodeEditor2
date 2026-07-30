using CodeEditor2.NavigatePanel;
using System;
using System.Collections.Generic;
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


        public static async Task<File> CreateAsync(string relativePath, Project project, Item parent)
        {
            FileTypes.FileType? fileType = project.GetFileType(relativePath);
            if (fileType != null)
            {
                File file = await fileType.CreateFile(relativePath, project);
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

        public virtual Task PostSyncCheck()
        {
            Task.Run(async () => { await SyncCheck(); });
            return Task.CompletedTask;
        }

        public Tests.TestResult? TestResult { get; set; } = null;

        public Action<Task> GetTestResults;

        private async Task SyncCheck()
        {
            if (!System.IO.File.Exists(AbsolutePath))
            {
                IsDeleted = true;
                await OnDeletedExternallyAsync();
                return;
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


        public override async Task UpdateAsync()
        {
            await base.UpdateAsync();
        }

        public virtual Task FileChangedAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnDeletedExternallyAsync()
        {
            Remove();
            return Task.CompletedTask;
        }


    }
}
