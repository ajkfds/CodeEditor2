using CodeEditor2.Data;
using DynamicData.Experimental;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;

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

                // 1. 繝代せ縺ｮ豁｣隕丞喧縺ｨ螳牙・諤ｧ縺ｮ繝√ぉ繝・け
                string searchPath = project.GetAbsolutePath(path);

                if (!searchPath.StartsWith(project.RootPath, StringComparison.OrdinalIgnoreCase))
                {
                    return "Error: Permission denied. Cannot read files outside of the project root.";
                }

                if (!Directory.Exists(searchPath))
                    return $"Error: Directory '{path}' not found.";

                // 2. 繝輔ぃ繧､繝ｫ縺ｮ繝輔ぅ繝ｫ繧ｿ繝ｪ繝ｳ繧ｰ (Globbing)
                var matcher = new Matcher();
                matcher.AddInclude(string.IsNullOrWhiteSpace(file_pattern) ? "**/*" : $"**/{file_pattern}");

                // 辟｡隕悶☆繧九ョ繧｣繝ｬ繧ｯ繝医Μ
                matcher.AddExclude("**/bin/**");
                matcher.AddExclude("**/obj/**");
                matcher.AddExclude("**/.git/**");

                var files = matcher.GetResultsInFullPath(searchPath);
                var results = new StringBuilder();
                var searchRegex = new Regex(regex, RegexOptions.Multiline | RegexOptions.Compiled);

                // 3. 讀懃ｴ｢縺ｮ螳溯｡・
                foreach (var file in files)
                {
                    // 繝舌う繝翫Μ繝輔ぃ繧､繝ｫ遲峨ｒ驕ｿ縺代ｋ縺溘ａ縺ｮ邁｡譏薙メ繧ｧ繝・け・亥ｿ・ｦ√↓蠢懊§縺ｦ諡｡蠑ｵ・・
                    if (IsBinaryFile(file)) continue;

                    var lines = await System.IO.File.ReadAllLinesAsync(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (searchRegex.IsMatch(lines[i]))
                        {
                            string relPath = Path.GetRelativePath(project.RootPath, file);
                            results.AppendLine($"--- {relPath} (Line {i + 1}) ---");

                            // 蜑榊ｾ・陦後・繧ｳ繝ｳ繝・く繧ｹ繝医ｒ陦ｨ遉ｺ
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

                    // 邨先棡縺碁聞縺吶℃繧句ｴ蜷医・蛻ｶ髯・
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
