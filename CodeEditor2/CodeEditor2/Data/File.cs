﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.NavigatePanel;


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
            // check registered filetype
            foreach (var fileType in Global.FileTypes.Values)
            {
                if (fileType.IsThisFileType(relativePath, project)) return fileType.CreateFile(relativePath, project);
            }

            File fileItem = new File();
            fileItem.Project = project;
            fileItem.RelativePath = relativePath;
            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                fileItem.Name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                fileItem.Name = relativePath;
            }

            fileItem.Parent = parent;

            if (FileCreated != null) FileCreated(fileItem);
            return fileItem;
        }

        public long ObjectID
        {
            get
            {
                bool firstTime;
                return Global.ObjectIDGenerator.GetId(this,out firstTime);
            }
        }
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

        protected override NavigatePanelNode createNode()
        {
            return new FileNode(this);
        }

        public FileTypes.FileType? FileType
        {
            get { return null; }
        }

        public override void Update()
        {
            base.Update();
        }
    }
}
