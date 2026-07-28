using Microsoft.Extensions.AI;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace CodeEditor2.LLM.Tools
{
    public class ReadFile : LLMTool
    {
        public ReadFile(Data.Project project) : base(project) { }
        /*
        ## read_file
        Description: Request to read the contents of a file at the specified path. Use this when you need to examine the contents of an existing file you do not know the contents of, for example to analyze code, review text files, or extract information from configuration files. Automatically extracts raw text from PDF and DOCX files. May not be suitable for other types of binary files, as it returns the raw content as a string.
        Parameters:
        - path: (required) The path of the file to read (relative to the current working directory ${cwd.toPosix()})
        - start_line: (optional) The line number to start reading from (1-based). If not specified, reading starts from the beginning of the file.
        - end_line: (optional) The line number to end reading at (1-based, inclusive). If not specified, reading continues to the end of the file.
        Usage:
        <read_file>
        <path>File path here</path>
        ${
            focusChainSettings.enabled
                ? `<task_progress>
        Checklist here (optional)
        </task_progress>`
                : ""
        }
        </read_file>
         */
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "read_file"); }

        public override string XmlExample { get; } = """
            ```xml
            <read_file>
            <path>File path here</path>
            <start_line>1</start_line>
            <end_line>100</end_line>
            </read_file>
            ```
            """;

        // Cline 互換: 出力は全行に "<行番号> | <内容>" の形式でプレフィックスを付与する。
        // 全体読み込み時のみ適用するサイズ上限 (範囲指定がある場合は緩和)。
        private const int MaxFileBytes = 10 * 1024 * 1024; // 10MB hard limit (full read only)

        [Description("""
            Request to read the contents of a file at the specified path.
            Use this when you need to examine the contents of an existing file you do not know the contents of,
            for example to analyze code, review text files, or extract information from configuration files.
            May not be suitable for other types of binary files, as it returns the raw content as a string.
            """)]
        public string Run(
            [Description("The path of the file to read (relative to the project root directory)")] string path,
            [Description("""
                (optional) The line number to start reading from (1-based).
                If not specified, reading starts from the beginning of the file.
                """)]
            string start_line = null,
            [Description("""
                (optional) The line number to end reading at (1-based, inclusive).
                If not specified, reading continues to the end of the file.
                """)]
            string end_line = null)
        {
            try
            {
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                // 1. パスの正規化と安全性のチェック
                string fullPath = project.GetAbsolutePath(path);
                if (!fullPath.StartsWith(project.RootPath, StringComparison.OrdinalIgnoreCase))
                    return "Error: Permission denied. Cannot read files outside of the project root.";

                if (!System.IO.File.Exists(fullPath))
                    return $"Error: File not found at path '{path}'.";

                // 2. 行範囲のパースと正規化
                int? startLineNum = ParseOptionalPositiveInt(start_line);
                if (startLineNum == null && !string.IsNullOrWhiteSpace(start_line))
                    return $"Error: Invalid value for start_line: '{start_line}'. Must be a positive integer.";

                int? endLineNum = ParseOptionalPositiveInt(end_line);
                if (endLineNum == null && !string.IsNullOrWhiteSpace(end_line))
                    return $"Error: Invalid value for end_line: '{end_line}'. Must be a positive integer.";

                if (startLineNum.HasValue && endLineNum.HasValue && startLineNum.Value > endLineNum.Value)
                    return $"Error: start_line ({startLineNum}) must not be greater than end_line ({endLineNum}).";

                // 3. ファイル内容の読み込み (UTF-8, BOM 自動判別)
                var fileInfo = new FileInfo(fullPath);

                // 全体読み込み時のサイズ上限 (範囲指定があれば緩和する)
                bool hasRange = startLineNum.HasValue || endLineNum.HasValue;
                if (!hasRange && fileInfo.Length > MaxFileBytes)
                    return $"Error: File is too large to read in full (limit: {MaxFileBytes} bytes). " +
                           "Please specify start_line and/or end_line to read a portion of the file.";

                string originalText;
                using (var reader = new System.IO.StreamReader(fullPath, System.Text.Encoding.UTF8, true))
                {
                    originalText = reader.ReadToEnd();
                }

                // 4. 行に分割 (改行コードを LF に正規化)
                string normalizedText = originalText.Replace("\r\n", "\n").Replace("\r", "\n");
                // 末尾が改行で終わっている場合は Split('\n') で空要素が末尾に付くので除去
                string[] lines = normalizedText.Split('\n');
                if (lines.Length > 0 && lines[lines.Length - 1].Length == 0)
                    Array.Resize(ref lines, lines.Length - 1);

                int totalLines = lines.Length;

                // 5. 読み込み範囲の決定 (1-based, inclusive)
                int effectiveStart = startLineNum ?? 1;
                int effectiveEnd = endLineNum ?? totalLines;
                if (effectiveStart < 1) effectiveStart = 1;
                if (effectiveEnd > totalLines) effectiveEnd = totalLines;

                if (effectiveStart > totalLines)
                    return $"Error: start_line ({effectiveStart}) exceeds the total number of lines in the file ({totalLines}).";

                // 6. 行番号付きで出力 (Cline 互換: "<n> | <content>")
                var sb = new StringBuilder();
                sb.Append("```\n");
                for (int i = effectiveStart; i <= effectiveEnd; i++)
                {
                    sb.Append(i).Append(" | ").Append(lines[i - 1]).Append('\n');
                }
                sb.Append("```\n");

                return sb.ToString();
            }
            catch (UnauthorizedAccessException)
            {
                return "Error: Access to the path is denied.";
            }
            catch (IOException ex)
            {
                return $"Error: An I/O error occurred while reading the file: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: An unexpected error occurred: {ex.Message}";
            }
        }

        // null/空文字なら null を返す。
        // 数値として解釈できればその値、解釈できなければ null を返す (呼び出し側でエラー判定)。
        private static int? ParseOptionalPositiveInt(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            if (!int.TryParse(raw.Trim(), System.Globalization.NumberStyles.Integer,
                              System.Globalization.CultureInfo.InvariantCulture, out int value))
                return null;
            if (value < 1) return null;
            return value;
        }
    }
}
