using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeEditor2.Data
{
    public class FileClassify
    {
        public FileClassify(Project project)
        {
            this.project = project;
            Refresh();
        }
        private Project project;
        public string AbsolutePath
        {
            get; private set;
        } = string.Empty;

        /// <summary>
        /// Re-resolves the path and re-reads the file, tolerating a missing one.
        /// </summary>
        /// <remarks>
        /// The path depends on <see cref="Project.RootPath"/>, which a headless
        /// host learns only after <c>initialize</c> - long after this object was
        /// constructed. Without a re-resolve the classification silently stayed
        /// empty in the LSP host (the editor happened to be fine because its
        /// project is created with the root already known).
        /// </remarks>
        public void Refresh()
        {
            commands.Clear();
            AbsolutePath = project.GetAbsolutePath(".fileClassify");
            if (System.IO.File.Exists(AbsolutePath)) loadFile();
        }

        public void Reload() => Refresh();

        private List<Item> commands = new List<Item>();
        private void loadFile()
        {
            string command = "";
            bool append = true;

            using (var sr = new System.IO.StreamReader(AbsolutePath))
            {
                while (!sr.EndOfStream)
                {
                    string? line = sr.ReadLine();
                    if (line == null) continue;
                    if (line.StartsWith("#")) continue;
                    if (line.Trim() == "") continue;

                    if (line.StartsWith("-"))
                    {
                        command = line.Substring(1).Trim();
                        append = false;
                    }
                    else if (line.StartsWith("+"))
                    {
                        command = line.Substring(1).Trim();
                        append = true;
                    }
                    else
                    {
                        if (command == "") continue;
                        string filter = line.Trim();
                        filter = filter.Replace('/', System.IO.Path.DirectorySeparatorChar);
                        Item item = new Item() { append = append, type = command, filter = filter };
                        commands.Add(item);
                    }
                }
            }
        }
        static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        }

        public bool HasDefinition()
        {
            if (commands.Count == 0) return false;
            return true;
        }
        public string? GetFileType(string relativePath, string? defaultType)
        {
            string? type = defaultType;

            foreach (Item item in commands)
            {
                bool isMatch = Regex.IsMatch(relativePath, WildcardToRegex(item.filter));

                if (isMatch)
                {
                    if (item.append)
                    {
                        type = item.type;
                    }
                    else
                    {
                        if (type == item.type)
                        {
                            type = null;
                        }
                    }
                }
            }
            return type;
        }

        private class Item
        {
            public required bool append;
            public required string type;
            public required string filter;
        }
    }
}
