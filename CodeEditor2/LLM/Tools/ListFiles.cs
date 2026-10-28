using Microsoft.Extensions.AI;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace CodeEditor2.LLM.Tools
{
    public class ListFiles : LLMTool
    {
        public ListFiles(Data.Project project) : base(project) { }
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
            Note: there is a size limit on the returned listing. If it exceeds the limit,
            a warning is prepended and only the beginning (first part) of the listing is returned;
            the tail is truncated. In that case, list a narrower path or disable recursive listing.
            """)]
        public string Run(
        [Description("The path of the directory to list contents for (relative to the project root directory)")]
        string path,
        [Description("Whether to list files recursively. Use true for recursive listing, false or omit for top-level only.")]
        string recursive = "false")
        {
            try
            {
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

                // 内部スキャン上限: 大量エントリでもメモリを肥大させないよう、
                // 上限バイトの 2 倍を超えた時点で列挙を打ち切る (最終的な切り詰めは LimitOutput が行う)。
                int scanLimitChars = MaxOutputBytes * 2;

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

                bool scanTruncated = false;
                foreach (var entry in entries)
                {
                    // ルートディレクトリからの相対パスに変換して表示
                    string relativePath = Path.GetRelativePath(project.RootPath, entry);
                    bool isDir = Directory.Exists(entry);
                    sb.AppendLine($"{(isDir ? "[DIR] " : "[FILE]")} {relativePath}");
                    if (sb.Length > scanLimitChars) { scanTruncated = true; break; }
                }

                // 出力サイズ上限 (LLMTool.MaxOutputBytes) を適用。超過時はアラート + 前側のみ返す
                string limited = LimitOutput(sb.ToString(), "list_files");
                if (scanTruncated && !limited.Contains("[WARNING]"))
                {
                    // 走査打ち切りが起きたが LimitOutput の閾値には届かなかった場合の注記
                    limited += "\n... (Listing truncated due to size)";
                }
                return limited;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }


    }


}
