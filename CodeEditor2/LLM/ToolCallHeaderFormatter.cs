using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace CodeEditor2.LLM
{
    /// <summary>
    /// Renders the header of a tool-call chat item.
    /// </summary>
    public static class ToolCallHeaderFormatter
    {
        /// <summary>
        /// Builds the markdown shown for a running tool call.
        /// </summary>
        /// <param name="toolName">The tool name reported by the provider.</param>
        /// <param name="invocation">
        /// The arguments as the provider streamed them: pretty-printed JSON
        /// (see <c>OpenRouterChat.FormatInvocation</c>). Empty while the
        /// provider has not streamed any argument yet.
        /// </param>
        public static string Build(string? toolName, string? invocation)
        {
            if (string.IsNullOrEmpty(toolName) && string.IsNullOrEmpty(invocation))
            {
                return string.Empty;
            }

            Dictionary<string, string>? arguments = ParseArguments(invocation);

            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(toolName))
            {
                sb.Append("**Tool call: ").Append(toolName).Append("**");
            }

            string? body = arguments == null ? null : FormatKnownTool(toolName, arguments);
            if (body != null)
            {
                sb.Append('\n').Append('\n').Append(body);
            }
            else if (!string.IsNullOrEmpty(invocation))
            {
                // Unknown tool (a plugin's own, or one added later): keep the
                // raw arguments so nothing is hidden, just fenced as JSON.
                sb.Append("\n\n```json\n").Append(invocation).Append("\n```");
            }

            sb.Append("\n\n");
            return sb.ToString();
        }

        /// <summary>
        /// Parses the JSON argument text into a flat string dictionary.
        /// Returns null when the text is not a JSON object (the provider may
        /// still be streaming it, or the fallback "key=value" form was used).
        /// </summary>
        /// <remarks>
        /// A value that is not a JSON string is re-serialized compactly so a
        /// number / bool / nested object still reads as a value rather than as
        /// "System.Collections.Generic.Dictionary...".
        /// </remarks>
        private static Dictionary<string, string>? ParseArguments(string? invocation)
        {
            if (string.IsNullOrWhiteSpace(invocation)) return null;

            try
            {
                using JsonDocument document = JsonDocument.Parse(invocation);
                if (document.RootElement.ValueKind != JsonValueKind.Object) return null;

                Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (JsonProperty property in document.RootElement.EnumerateObject())
                {
                    result[property.Name] = property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString() ?? string.Empty
                        : property.Value.GetRawText();
                }
                return result;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Renders the arguments of a tool this class knows about.
        /// Returns null for an unknown tool, so the caller falls back to the raw
        /// invocation (a tool added by a plugin stays readable without changes
        /// here).
        /// </summary>
        /// <remarks>
        /// Showing the arguments as a bare JSON blob makes the common tools hard
        /// to read at a glance: the interesting part of <c>replace_in_file</c> is
        /// the path and the SEARCH/REPLACE diff, not the braces around them. Each
        /// case below therefore picks the few arguments that matter and renders
        /// them as labelled markdown. The full raw JSON is intentionally not
        /// repeated: the values are shown in full, so nothing is lost.
        /// </remarks>
        private static string? FormatKnownTool(string? toolName, Dictionary<string, string> arguments)
        {
            switch (toolName)
            {
                case "replace_in_file":
                    return FormatReplaceInFile(arguments);

                case "read_file":
                    return FormatReadFile(arguments);

                case "write_to_file":
                    return FormatWriteToFile(arguments);

                case "execute_command":
                    return FormatExecuteCommand(arguments);

                case "search_files":
                    return FormatSearchFiles(arguments);

                case "list_files":
                    return FormatListFiles(arguments);

                case "get_lib_definition":
                    return FormatSingleValue(arguments, "library_type_name", "library type");

                case "build_dotnet":
                    // no arguments: the header already names the tool
                    return string.Empty;

                default:
                    return null;
            }
        }

        /// <summary>
        /// <c>replace_in_file</c>: the path, then the SEARCH/REPLACE blocks.
        /// </summary>
        /// <remarks>
        /// The diff is the whole point of the call, so it is shown verbatim in a
        /// fenced block (no language tag: the content is SystemVerilog / C# /
        /// anything, and a wrong tag would colour it misleadingly). The
        /// SEARCH/REPLACE markers are kept, which is exactly how the tool asks
        /// the model to write them, so the reader can see what will match.
        /// </remarks>
        private static string FormatReplaceInFile(Dictionary<string, string> arguments)
        {
            StringBuilder sb = new StringBuilder();
            AppendField(sb, "path", Get(arguments, "path"));

            string diff = Get(arguments, "diff");
            if (diff.Length == 0)
            {
                // still streaming: AppendField marks the value as not streamed yet
                AppendField(sb, "diff", string.Empty);
                return sb.ToString().TrimEnd();
            }

            int blocks = CountDiffBlocks(diff);
            sb.Append("**diff**");
            if (blocks > 1) sb.Append(" (").Append(blocks).Append(" blocks)");
            sb.Append('\n').Append('\n').Append("```\n").Append(diff.TrimEnd('\n')).Append("\n```");
            return sb.ToString();
        }

        /// <summary>
        /// Counts the SEARCH/REPLACE blocks in <paramref name="diff"/>, so the
        /// header can say how many edits the call performs.
        /// </summary>
        private static int CountDiffBlocks(string diff)
        {
            int count = 0;
            foreach (string line in diff.Split('\n'))
            {
                if (line.TrimEnd('\r').TrimEnd().EndsWith("SEARCH", StringComparison.Ordinal)) count++;
            }
            return count == 0 ? 1 : count;
        }

        /// <summary>
        /// <c>read_file</c>: the path, plus the line range when one was given.
        /// </summary>
        private static string FormatReadFile(Dictionary<string, string> arguments)
        {
            StringBuilder sb = new StringBuilder();
            AppendField(sb, "path", Get(arguments, "path"));

            string startLine = Get(arguments, "start_line");
            string endLine = Get(arguments, "end_line");
            if (startLine.Length > 0 || endLine.Length > 0)
            {
                string range = (startLine.Length > 0 ? startLine : "1")
                    + " - "
                    + (endLine.Length > 0 ? endLine : "end");
                AppendField(sb, "lines", range);
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// <c>write_to_file</c>: the path, then the content.
        /// </summary>
        /// <remarks>
        /// The content is the complete file, so it can be long. It is shown in a
        /// collapsed-friendly fenced block; the item itself is not collapsed (the
        /// call must stay visible), so the block is capped and the remainder is
        /// summarized rather than flooding the chat.
        /// </remarks>
        private static string FormatWriteToFile(Dictionary<string, string> arguments)
        {
            StringBuilder sb = new StringBuilder();
            AppendField(sb, "path", Get(arguments, "path"));

            string content = Get(arguments, "content");
            if (content.Length == 0)
            {
                AppendField(sb, "content", "(none yet)");
                return sb.ToString().TrimEnd();
            }

            int lines = CountLines(content);
            sb.Append("**content** (").Append(lines).Append(lines == 1 ? " line)" : " lines)");
            sb.Append('\n').Append('\n').Append("```\n").Append(CapForDisplay(content)).Append("\n```");
            return sb.ToString();
        }

        /// <summary><c>execute_command</c>: the command line.</summary>
        private static string FormatExecuteCommand(Dictionary<string, string> arguments)
        {
            StringBuilder sb = new StringBuilder();
            AppendCodeField(sb, "command", Get(arguments, "command"));
            return sb.ToString().TrimEnd();
        }

        /// <summary><c>search_files</c>: the directory, the pattern and the file filter.</summary>
        private static string FormatSearchFiles(Dictionary<string, string> arguments)
        {
            StringBuilder sb = new StringBuilder();
            AppendField(sb, "path", Get(arguments, "path"));
            AppendCodeField(sb, "pattern", Get(arguments, "regex"));
            string filePattern = Get(arguments, "file_pattern");
            if (filePattern.Length > 0) AppendCodeField(sb, "file pattern", filePattern);
            return sb.ToString().TrimEnd();
        }

        /// <summary><c>list_files</c>: the directory, plus whether it recurses.</summary>
        private static string FormatListFiles(Dictionary<string, string> arguments)
        {
            StringBuilder sb = new StringBuilder();
            AppendField(sb, "path", Get(arguments, "path"));
            string recursive = Get(arguments, "recursive");
            if (recursive.Length > 0) AppendField(sb, "recursive", recursive);
            return sb.ToString().TrimEnd();
        }

        /// <summary>A tool whose whole call is one named argument.</summary>
        private static string FormatSingleValue(Dictionary<string, string> arguments, string key, string label)
        {
            StringBuilder sb = new StringBuilder();
            AppendField(sb, label, Get(arguments, key));
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Appends a <c>**label**: value</c> line. An empty value is rendered as
        /// "(not streamed yet)" so a partially streamed call still shows which
        /// arguments exist.
        /// </summary>
        private static void AppendField(StringBuilder sb, string label, string value)
        {
            sb.Append("**").Append(label).Append("**: ")
              .Append(value.Length == 0 ? "(not streamed yet)" : value)
              .Append('\n');
        }

        /// <summary>Appends a labelled inline-code field (paths / patterns / commands).</summary>
        private static void AppendCodeField(StringBuilder sb, string label, string value)
        {
            sb.Append("**").Append(label).Append("**: ");
            if (value.Length == 0)
            {
                sb.Append("(not streamed yet)");
            }
            else
            {
                // A fence would break the surrounding markdown when the value
                // contains a backtick (a shell command often does), so inline
                // code with a matching delimiter count is used instead.
                sb.Append('`').Append(value).Append('`');
            }
            sb.Append('\n');
        }

        /// <summary>Number of lines in <paramref name="text"/> (at least 1).</summary>
        private static int CountLines(string text)
        {
            int lines = 1;
            foreach (char c in text)
            {
                if (c == '\n') lines++;
            }
            return lines;
        }

        /// <summary>
        /// Trims a long value for the chat header. The tool call item stays
        /// visible for the whole turn, so an unbounded paste of a large file
        /// would push everything else off screen; the full text is still what the
        /// tool receives (this only affects the display).
        /// </summary>
        private static string CapForDisplay(string text)
        {
            const int maxLines = 40;
            const int maxChars = 4000;

            string trimmed = text.TrimEnd('\n');
            if (trimmed.Length <= maxChars && CountLines(trimmed) <= maxLines)
            {
                return trimmed;
            }

            StringBuilder sb = new StringBuilder();
            int line = 0;
            foreach (string sourceLine in trimmed.Split('\n'))
            {
                if (line >= maxLines || sb.Length >= maxChars) break;
                sb.Append(sourceLine.TrimEnd('\r')).Append('\n');
                line++;
            }
            sb.Append("... (")
              .Append(CountLines(trimmed) - line)
              .Append(" more lines not shown; the tool received the full content)");
            return sb.ToString();
        }

        /// <summary>
        /// Reads an argument by its exact name, returning "" when it is absent.
        /// </summary>
        /// <remarks>
        /// The names are the tool's C# parameter names (they are what
        /// <c>AIFunctionFactory.Create</c> sends), so no alias handling is
        /// needed. A missing argument means the provider has not streamed it yet
        /// (or the tool omitted an optional one), which the callers render as
        /// "(not streamed yet)" instead of hiding the field.
        /// </remarks>
        private static string Get(Dictionary<string, string> arguments, string key)
        {
            if (arguments.TryGetValue(key, out string? value) && value != null) return value;
            return string.Empty;
        }
    }
}
