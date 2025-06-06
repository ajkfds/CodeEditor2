﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using CodeEditor2.NavigatePanel;
using DynamicData.Binding;
using DynamicData.Kernel;


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
            if (project.FileClassify.HasDefinition())
            {
                project.FileClassify.IsMatched(relativePath, out string type);
                if (Global.FileTypes.ContainsKey(type))
                {
                    FileTypes.FileType fileType = Global.FileTypes[type];
                    return fileType.CreateFile(relativePath, project);
                }
            }

            foreach (var fileType in Global.FileTypes)
            {
                if (fileType.Value.IsThisFileType(relativePath, project)) return fileType.Value.CreateFile(relativePath, project);
            }

           
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
