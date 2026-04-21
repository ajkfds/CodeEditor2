using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeEditor2.LLM.Tools
{
    public class SearchFiles : LLMTool
    {
        public SearchFiles(Data.Project project) : base(project) { }
        /*
        ## search_files
        Description: 
        Parameters:
        - path: (required) 
        - regex: (required) The regular expression pattern to search for. Uses Rust regex syntax.
        - file_pattern: (optional) Glob pattern to filter files (e.g., '*.ts' for TypeScript files). If not provided, it will search all files (*).
        Usage:
        <search_files>
        <path>Directory path here</path>
        <regex>Your regex pattern here</regex>
        <file_pattern>file pattern here (optional)</file_pattern>
        </search_files>
         */
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "search_files"); }

        public override string XmlExample { get; } = """
            ```xml
            <search_files>
            <path>Directory path here</path>
            <regex>Your regex pattern here</regex>
            <file_pattern>file pattern here (optional)</file_pattern>
            </search_files>
            ```
            """;

        [Description("""
            Request to perform a regex search across files in a specified directory, providing context-rich results. 
            This tool searches for patterns or specific content across multiple files, displaying each match with encapsulating context. 
            IMPORTANT NOTE: Use this tool sparingly, and opt to explore the codebase using the \`list_files\` and \`read_file\` tools instead.
            """)]
        public async Task<string> Run(
        [Description("""
            The path of the directory to search in (relative to the project root directory. This directory will be recursively searched.
            """)]
        string path,
        [Description("""
            The regular expression pattern to search for. Uses Rust regex syntax.
            """)]
        string regex,
        [Description("""
            Glob pattern to filter files (e.g., '*.ts' for TypeScript files). If not provided, it will search all files (*).
            """)]
        string file_pattern
        )
        {
            try
            {
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                // 1. パスの正規化と安全性のチェック
                string searchPath = project.GetAbsolutePath(path);

                if (!searchPath.StartsWith(project.RootPath, StringComparison.OrdinalIgnoreCase))
                {
                    return "Error: Permission denied. Cannot read files outside of the project root.";
                }

                if (!Directory.Exists(searchPath))
                    return $"Error: Directory '{path}' not found.";

                // 2. ファイルのフィルタリング (Globbing)
                var matcher = new Matcher();
                matcher.AddInclude(string.IsNullOrWhiteSpace(file_pattern) ? "**/*" : $"**/{file_pattern}");

                // 無視するディレクトリ
                matcher.AddExclude("**/bin/**");
                matcher.AddExclude("**/obj/**");
                matcher.AddExclude("**/.git/**");

                var files = matcher.GetResultsInFullPath(searchPath);
                var results = new StringBuilder();
                var searchRegex = new Regex(regex, RegexOptions.Multiline | RegexOptions.Compiled);

                // 3. 検索の実行
                foreach (var file in files)
                {
                    // バイナリファイル等を避けるための簡易チェック（必要に応じて拡張）
                    if (IsBinaryFile(file)) continue;

                    var lines = await System.IO.File.ReadAllLinesAsync(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (searchRegex.IsMatch(lines[i]))
                        {
                            string relPath = Path.GetRelativePath(project.RootPath, file);
                            results.AppendLine($"--- {relPath} (Line {i + 1}) ---");

                            // 前後2行のコンテキストを表示
                            int start = Math.Max(0, i - 2);
                            int end = Math.Min(lines.Length - 1, i + 2);

                            for (int j = start; j <= end; j++)
                            {
                                string prefix = (j == i) ? ">> " : "   ";
                                results.AppendLine($"{prefix}{j + 1}: {lines[j]}");
                            }
                            results.AppendLine();
                        }
                    }

                    // 結果が長すぎる場合は制限
                    if (results.Length > 1500)
                    {
                        results.AppendLine("... Search truncated: too many results. Please refine your regex or path.");
                        return results.ToString();
                    }
                }

                return results.Length > 0 ? results.ToString() : "No matches found.";
            }
            catch (Exception ex)
            {
                return $"Error during search: {ex.Message}";
            }
        }

        private static bool IsBinaryFile(string path)
        {
            var buffer = new byte[1024];
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            var bytesRead = fs.Read(buffer, 0, buffer.Length);
            return buffer.Take(bytesRead).Any(b => b == 0);
        }

    }
}
