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


        // 繧ｨ繝ｼ繧ｸ繧ｧ繝ｳ繝医′豺ｷ荵ｱ縺励↑縺・ｈ縺・・壼ｸｸ辟｡隕悶☆縺ｹ縺阪ョ繧｣繝ｬ繧ｯ繝医Μ
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
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                // 1. 繝代せ縺ｮ螳牙・諤ｧ繧堤｢ｺ隱・
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

                // 2. 繝輔ぃ繧､繝ｫ縺ｨ繝・ぅ繝ｬ繧ｯ繝医Μ縺ｮ蜿門ｾ・
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = isRecursive,
                    AttributesToSkip = FileAttributes.System,
                    IgnoreInaccessible = true
                };

                // 繝輔ぅ繝ｫ繧ｿ繝ｪ繝ｳ繧ｰ縺励↑縺後ｉ繝ｪ繧ｹ繝医ｒ菴懈・
                var entries = Directory.EnumerateFileSystemEntries(targetPath, "*", options)
                    .Where(entry => !ExcludedDirectories.Any(ex => entry.Contains(Path.DirectorySeparatorChar + ex + Path.DirectorySeparatorChar) || entry.EndsWith(Path.DirectorySeparatorChar + ex)));

                foreach (var entry in entries)
                {
                    // 繝ｫ繝ｼ繝医ョ繧｣繝ｬ繧ｯ繝医Μ縺九ｉ縺ｮ逶ｸ蟇ｾ繝代せ縺ｫ螟画鋤縺励※陦ｨ遉ｺ
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
