using Avalonia.Controls.Documents;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.LLM.Tools
{
    public class ListFiles:LLMTool
    {
        /*
        ## list_files
        Description: Request to list files and directories within the specified directory. If recursive is true, it will list all files and directories recursively. If recursive is false or not provided, it will only list the top-level contents. Do not use this tool to confirm the existence of files you may have created, as the user will let you know if the files were created successfully or not.
        Parameters:
        - path: (required) The path of the directory to list contents for (relative to the current working directory ${cwd.toPosix()})
        - recursive: (optional) Whether to list files recursively. Use true for recursive listing, false or omit for top-level only.
        Usage:
        <list_files>
        <path>Directory path here</path>
        ${
	        focusChainSettings.enabled
		        ? `<task_progress>
        Checklist here (optional)
        </task_progress>`
		        : ""
        }
        </list_files>         
         */

        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "list_files"); }

        public override string XmlExample { get; } = """
            ```xml
            <list_files>
            <path>Directory path here</path>
            </list_files>         
            ```
            """;


        // エージェントが混乱しないよう、通常無視すべきディレクトリ
        private static readonly string[] ExcludedDirectories = { ".git", "node_modules", "bin", "obj", ".vs" };

        [Description("""
            Request to list files and directories within the specified directory. 
            If recursive is true, it will list all files and directories recursively.
            """)]
        public string Run(
        [Description("The path of the directory to list contents for (relative to the project root directory)")] 
        string path,
        [Description("Whether to list files recursively. Use true for recursive listing, false or omit for top-level only.")] 
        string recursive = "false")
        {
            try
            {
                CodeEditor2.Data.Project? project = GetProject();
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                // 1. パスの安全性を確認
                string targetPath = project.GetAbsolutePath(path);

                if (!targetPath.StartsWith(project.RootPath, StringComparison.OrdinalIgnoreCase))
                {
                    return "Error: Permission denied. Cannot read files outside of the project root.";
                }

                if (!Directory.Exists(targetPath))
                {
                    return $"Error: Directory not found at '{path}'.";
                }

                bool isRecursive = recursive.Equals("true", StringComparison.OrdinalIgnoreCase);
                var sb = new StringBuilder();
                sb.AppendLine($"Listing files in '{path}':");

                // 2. ファイルとディレクトリの取得
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = isRecursive,
                    AttributesToSkip = FileAttributes.System,
                    IgnoreInaccessible = true
                };

                // フィルタリングしながらリストを作成
                var entries = Directory.EnumerateFileSystemEntries(targetPath, "*", options)
                    .Where(entry => !ExcludedDirectories.Any(ex => entry.Contains(Path.DirectorySeparatorChar + ex + Path.DirectorySeparatorChar) || entry.EndsWith(Path.DirectorySeparatorChar + ex)));

                foreach (var entry in entries)
                {
                    // ルートディレクトリからの相対パスに変換して表示
                    string relativePath = Path.GetRelativePath(project.RootPath, entry);
                    bool isDir = Directory.Exists(entry);
                    sb.AppendLine($"{(isDir ? "[DIR] " : "[FILE]")} {relativePath}");
                }

                return sb.Length > 20000
                    ? sb.ToString().Substring(0, 20000) + "\n... (Listing truncated due to size)"
                    : sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

    }


}
