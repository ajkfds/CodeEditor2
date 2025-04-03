using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Joins;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    public class IgnoreList
    {
        public IgnoreList(Project project)
        {
            this.project = project;
            AbsolutePath = project.GetAbsolutePath(".ignoreList");

            if (System.IO.File.Exists(AbsolutePath)) loadFile();
        }
        private Project project;
        public string AbsolutePath
        {
            get; private set;
        }

        public void Reload()
        {
            commands.Clear();
            loadFile();
        }

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
        public bool IsMatched(string relativePath, out string type)
        {
            bool active = false;
            type = "";

            foreach (Item item in commands)
            {
                bool isMatch = Regex.IsMatch(relativePath, WildcardToRegex(item.filter));

                if (isMatch)
                {
                    if (item.append)
                    {
                        active = true;
                        type = item.type;
                    }
                    else
                    {
                        if (type == item.type)
                        {
                            active = false;
                        }
                    }
                }
            }
            return active;
        }

        private class Item
        {
            public required bool append;
            public required string type;
            public required string filter;
        }
    }
}
