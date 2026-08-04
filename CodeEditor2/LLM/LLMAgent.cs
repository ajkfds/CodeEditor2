using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.LLM
{
    public class LLMAgent
    {
        public LLMAgent()
        {
        }

        /// <summary>
        /// function call tools
        /// </summary>
        public List<AITool> Tools { get; } = new List<AITool>();
        /// <summary>
        /// function call is implemented in message
        /// </summary>
        public bool PersudoFunctionCallMode = false;
        /// <summary>
        /// If true, tool calls support an optional 'id' attribute and the result
        /// is wrapped in &lt;tool_result id="..."&gt;...&lt;/tool_result&gt; so the
        /// LLM can correlate each result with the originating call.
        /// If false (default), behavior is unchanged: results are appended as
        /// plain text and no id is read or emitted.
        /// </summary>
        public bool UseToolCallId = false;
        /// <summary>
        ///  base prompt for initial message
        /// </summary>
        public string BasePrompt { get; set; } = "";

        /// <summary>
        /// parameters to replace strings in BasePrompt
        /// </summary>
        public Dictionary<string, string> PromptParameters = new Dictionary<string, string>();

        public async Task<string?> ParseResponceAsync(string responce, CancellationToken cancellationToken, ChatControl? chatControl = null)
        {
            if (PersudoFunctionCallMode)
            {
                string? funcResult = await ParseExecutePersudoFunctionCallAsync(responce, cancellationToken, chatControl);
                return funcResult;
            }
            return null;
        }

        public Task<string> ProcessPromptAsync(string prompt)
        {
            return Task.FromResult(prompt);
        }

        public Task<string> GetBasePromptAsync(CancellationToken cancellationToken)
        {
            string basePrompt = BasePrompt;
            StringBuilder sb = new StringBuilder();

            sb.Append(BasePrompt);
            if (PersudoFunctionCallMode)
            {
                AppendPersudoFunctionCallInstruction(sb);
            }
            basePrompt = buildPrompt(sb);
            basePrompt = basePrompt.Replace("\r\n", "\n").Replace("\r", "\n");

            return Task.FromResult(basePrompt);
        }

        // Function Call
        private async Task<string?> ParseExecutePersudoFunctionCallAsync(string responce, CancellationToken cancellationToken, ChatControl? chatControl = null)
        {
            // Optional 'id' attribute on the opening tag (only meaningful when UseToolCallId is enabled).
            var matches = Regex.Matches(responce, @"<\s*(?<tool>\w+)(?:\s+id\s*=\s*""(?<id>[^""]*)"")?\s*>(?<params>.*?)</\s*\k<tool>\s*>", RegexOptions.Singleline);

            if (matches.Count>0)
            {
                StringBuilder sb = new StringBuilder();

                foreach (Match match in matches)
                {
                    string? toolCallId = null;
                    CancellationTokenSource? spinner_cts = null;
                    Task? spinnerTask = null;
                    try
                    {
                        string toolName = match.Groups["tool"].Value;
                        if (toolName == "reasoning" || toolName == "think") continue;

                        if (UseToolCallId && match.Groups["id"].Success)
                        {
                            toolCallId = match.Groups["id"].Value;
                        }

                        // Use FirstOrDefault so a missing tool name doesn't throw.
                        AITool? selectedTool = Tools.Where((tool) => { return tool.Name == toolName; }).FirstOrDefault();
                        if (selectedTool == null)
                        {
                            AppendToolResult(sb, toolCallId, $"Error: Unknown tool '{toolName}'. Available tools: {string.Join(", ", Tools.Select(t => t.Name))}");
                            continue;
                        }

                        // Notify tool call start
                        chatControl?.ToolCallStarted();


                        AIFunctionArguments args = new AIFunctionArguments();

                        string innerContent = match.Groups["params"].Value;
                        var paramMatches = Regex.Matches(innerContent, @"<\s*(?<key>\w+)\s*>(?<value>.*?)<\s*/\k<key>\s*>", RegexOptions.Singleline);
                        foreach (Match p in paramMatches)
                        {
                            // Avoid ArgumentException from duplicate keys; keep the last value.
                            if (args.ContainsKey(p.Groups["key"].Value))
                            {
                                args[p.Groups["key"].Value] = p.Groups["value"].Value;
                            }
                            else
                            {
                                args.Add(p.Groups["key"].Value, p.Groups["value"].Value);
                            }
                        }
                        Progress<string> progress = new Progress<string>((message) => { chatControl?.ToolCallStarted(); });
                        args["progress"] = progress;

                        AIFunction? aIFunction = selectedTool as AIFunction;
                        if (aIFunction == null)
                        {
                            AppendToolResult(sb, toolCallId, $"Error: Tool '{toolName}' is not invokable (not an AIFunction).");
                            continue;
                        }

                        spinner_cts = new CancellationTokenSource();
                        CancellationToken spinner_cancel = spinner_cts.Token;
                        spinnerTask = Task.Run(async () => {
                            try
                            {
                                while (!spinner_cancel.IsCancellationRequested)
                                {
                                    await Task.Delay(100, spinner_cancel); // 100ms待機を非同期で行う
                                    chatControl?.ToolCallStarted();
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                // キャンセル時は正常終了として扱う
                            }
                        }, spinner_cancel);

                        object? ret = await aIFunction.InvokeAsync(args, cancellationToken);

                        string? s_ret = ret?.ToString();
                        if (s_ret != null)
                        {
                            AppendToolResult(sb, toolCallId, s_ret);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendToolResult(sb, toolCallId, $"Error: failed to parse or execute function call '{match.Groups["tool"].Value}': {ex.GetType().Name}: {ex.Message}\n--- matched block ---\n{match.Value}");
                    }
                    finally
                    {
                        // Stop the spinner task on every exit path so we never leak it.
                        if (spinner_cts != null)
                        {
                            await spinner_cts.CancelAsync();
                        }
                        if (spinnerTask != null)
                        {
                            try { await spinnerTask; } catch { /* ignore spinner task cancellation */ }
                            spinnerTask.Dispose();
                        }
                        spinner_cts?.Dispose();
                        chatControl?.ToolCallEnded();
                    }
                }
                if (sb.Length == 0) return null;
                return sb.ToString();
            }
            return null;
        }

        /// <summary>
        /// Append a single tool result to the output buffer.
        /// When UseToolCallId is true and <paramref name="toolCallId"/> is non-empty,
        /// the result is wrapped in &lt;tool_result id="..."&gt;...&lt;/tool_result&gt;.
        /// Otherwise the result is appended as plain text (original behavior).
        /// </summary>
        private void AppendToolResult(StringBuilder sb, string? toolCallId, string result)
        {
            if (UseToolCallId && !string.IsNullOrEmpty(toolCallId))
            {
                sb.Append("<tool_result id=\"")
                  .Append(toolCallId)
                  .AppendLine("\">");
                // Indent the inner content slightly so the wrapper is easy to spot.
                foreach (string line in result.Replace("\r\n", "\n").Split('\n'))
                {
                    sb.Append("  ").AppendLine(line);
                }
                sb.AppendLine("</tool_result>");
            }
            else
            {
                sb.AppendLine(result);
            }
        }

        

        // Build Prompt
        private string buildPrompt(StringBuilder sb)
        {
            string prompt = sb.ToString();

            foreach (var keyValuePair in PromptParameters)
            {
                prompt = prompt.Replace("${" + keyValuePair.Key + "}", keyValuePair.Value);
            }
            prompt = prompt.Replace("\r\n", "\n").Replace("\r", "\n");
            return prompt;
        }
        private void AppendPersudoFunctionCallInstruction(StringBuilder sb)
        {

            sb.AppendLine("# Tools");
            sb.AppendLine("");

            if (UseToolCallId)
            {
                sb.AppendLine("You SHOULD attach an `id` attribute to EVERY tool call. " +
                              "The system wraps the matching result in <tool_result id=\"...\">...</tool_result> " +
                              "so you can correlate each result with the originating call. " +
                              "If you omit the `id`, the result is returned as plain text and you may lose track of which call produced it.");
                sb.AppendLine("");
                sb.AppendLine("Id rules:");
                sb.AppendLine("- Each id MUST be unique within a single turn. Do NOT reuse an id you have already used.");
                sb.AppendLine("- Prefer a short, monotonically increasing counter like `call_001`, `call_002`, ... and increment it for every new tool call.");
                sb.AppendLine("- The user message contains a hint of the next id to use (e.g. `Next tool call id: call_004`). Use that hint as the starting point for the next id and keep incrementing from there.");
                sb.AppendLine("- If multiple tools are called in one response, give every call a distinct id (e.g. `call_004`, `call_005`, ...).");
                sb.AppendLine("");
            }

            foreach (var tool in Tools)
            {
                AppendAIToolInstruction(sb, tool);
            }
        }

        public void AppendAIToolInstruction(StringBuilder sb, AITool tool)
        {
            if (tool is AIFunction aiFunc)
            {
                sb.AppendLine("## " + tool.Name);
                sb.AppendLine("Description: " + tool.Description);

                sb.AppendLine("Parameters:");

                JsonElement schema = aiFunc.JsonSchema;
                StringBuilder usage = new StringBuilder();

                if (schema.TryGetProperty("properties", out var properties))
                {
                    foreach (var prop in properties.EnumerateObject())
                    {
                        string name = prop.Name;
                        string type = prop.Value.GetProperty("type").GetString() ?? "unknown";

                        string? description = prop.Value.TryGetProperty("description", out var desc)
                            ? desc.GetString() : "no description";

                        bool isRequired = false;
                        if (schema.TryGetProperty("required", out var requiredList))
                        {
                            isRequired = requiredList.EnumerateArray().Any(x => x.GetString() == name);
                        }
                        sb.AppendLine("-" + name + ":" + (isRequired ? "(required)" : "(optional)") + description);
                        usage.AppendLine("<" + name + ">" + description + "</" + name + ">");
                    }
                }
                sb.AppendLine("Usage:");
                sb.AppendLine("```xml");
                if (UseToolCallId)
                {
                    sb.AppendLine("<" + tool.Name + " id=\"call_abc123\">");
                }
                else
                {
                    sb.AppendLine("<" + tool.Name + " >");
                }
                sb.Append(usage.ToString());
                sb.AppendLine("</" + tool.Name + " >");
                sb.AppendLine("```");
                sb.AppendLine("");
            }
        }
    }
}
