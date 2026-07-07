using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeEditor2.Data
{
    /// <summary>
    /// .parseRule ファイルを読み込み、パターンごとに適用するRuleを管理するクラス
    /// </summary>
    public class ParseRule
    {
        public ParseRule(Project project)
        {
            this.project = project;
            AbsolutePath = project.GetAbsolutePath(".parseRule");

            if (System.IO.File.Exists(AbsolutePath))
            {
                loadFile();
            }
        }

        private Project project;
        public string AbsolutePath { get; private set; }

        /// <summary>
        /// 適用ルールアイテム一覧
        /// </summary>
        private List<Item> commands = new List<Item>();

        public void Reload()
        {
            commands.Clear();
            if (System.IO.File.Exists(AbsolutePath))
            {
                loadFile();
            }
        }

        private void loadFile()
        {
            string ruleName = "";
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
                        ruleName = line.Substring(1).Trim();
                        append = false;
                    }
                    else if (line.StartsWith("+"))
                    {
                        ruleName = line.Substring(1).Trim();
                        append = true;
                    }
                    else
                    {
                        if (ruleName == "") continue;
                        string filter = line.Trim();
                        filter = filter.Replace('/', System.IO.Path.DirectorySeparatorChar);
                        Item item = new Item() { append = append, ruleName = ruleName, filter = filter };
                        commands.Add(item);
                    }
                }
            }
        }

        static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        }

        /// <summary>
        /// 定義があるかチェック
        /// </summary>
        public bool HasDefinition()
        {
            return commands.Count > 0;
        }

        /// <summary>
        /// 指定されたファイルパスに適用されるRule名を取得
        /// </summary>
        /// <param name="relativePath">ファイルの相対パス</param>
        /// <param name="defaultRule">デフォルトのRule名（一致しなかった場合に使用）</param>
        /// <returns>適用するRule名（見つからない場合はdefaultRule）</returns>
        public string? GetParseRule(string relativePath, string? defaultRule)
        {
            string? ruleName = defaultRule;

            foreach (Item item in commands)
            {
                bool isMatch = Regex.IsMatch(relativePath, WildcardToRegex(item.filter));

                if (isMatch)
                {
                    if (item.append)
                    {
                        ruleName = item.ruleName;
                    }
                    else
                    {
                        if (ruleName == item.ruleName)
                        {
                            ruleName = null;
                        }
                    }
                }
            }
            return ruleName;
        }

        /// <summary>
        /// アイテムクラス
        /// </summary>
        private class Item
        {
            public required bool append;
            public required string ruleName;
            public required string filter;
        }
    }
}
