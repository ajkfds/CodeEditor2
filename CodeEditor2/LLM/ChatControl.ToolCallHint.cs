
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeEditor2.LLM;

/// <summary>
/// Partial implementation of ChatControl providing tool-call hint support.
/// Appends a summary of the tool calls found in the previous LLM response
/// (tool name + key parameter) to the next command as a system-hint block,
/// so the LLM can keep track of what it just did without expanding the
/// full tool results.
/// </summary>
public partial class ChatControl
{
    /// <summary>
    /// Extract tool calls (tool name + key parameter) from an LLM response text.
    /// Uses the same regex as LLMAgent.ParseExecutePersudoFunctionCallAsync so the
    /// extraction matches what will actually be executed.
    /// The "key param" is the first key/value pair inside the tool block.
    /// </summary>
    private static List<(string ToolName, string KeyParam)> ExtractToolCalls(string response)
    {
        var result = new List<(string, string)>();
        if (string.IsNullOrEmpty(response)) return result;

        var matches = Regex.Matches(
            response,
            "<\\s*(?<tool>\\w+)(?:\\s+id\\s*=\\s*\"(?<id>[^\"]*)\")?\\s*>(?<params>.*?)</\\s*\\k<tool>\\s*>",
            RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            string toolName = match.Groups["tool"].Value;
            if (toolName == "reasoning" || toolName == "think") continue;

            // Key parameter = first key/value pair in the tool block
            string keyParam = "";
            var paramMatch = Regex.Match(
                match.Groups["params"].Value,
                "<\\s*(?<key>\\w+)\\s*>(?<value>.*?)</\\s*\\k<key>\\s*>",
                RegexOptions.Singleline);
            if (paramMatch.Success)
            {
                string value = paramMatch.Groups["value"].Value;
                // Collapse whitespace / newlines so the hint stays compact
                value = Regex.Replace(value, "\\s+", " ").Trim();
                if (value.Length > 80) value = value.Substring(0, 80) + "...";
                keyParam = paramMatch.Groups["key"].Value + ": " + value;
            }

            result.Add((toolName, keyParam));
        }
        return result;
    }

    /// <summary>
    /// Append a system-hint block listing the previous tool calls (tool name + key
    /// parameter) to the tool-result command so the LLM can see which tool calls it
    /// made in the previous response without expanding the full results.
    /// Returns the command unchanged when UseToolCallId is disabled or no tool call exists.
    /// </summary>
    private string AppendToolCallHintIfNeeded(string command, string previousResponse)
    {
        if (agent == null) return command;
        if (!agent.UseToolCallId) return command;

        var toolCalls = ExtractToolCalls(previousResponse);
        if (toolCalls.Count == 0) return command;

        StringBuilder sb = new StringBuilder();
        sb.Append(command);
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("<system-hint>");
        sb.AppendLine("Previous tool calls:");
        foreach (var (toolName, keyParam) in toolCalls)
        {
            sb.AppendLine(keyParam.Length == 0
                ? "- " + toolName
                : "- " + toolName + " (" + keyParam + ")");
        }
        sb.Append("</system-hint>");
        return sb.ToString();
    }
}
