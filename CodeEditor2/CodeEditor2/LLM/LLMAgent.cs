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
            var matches = Regex.Matches(responce, @"<\s*(?<tool>\w+)\s*>(?<params>.*?)</\s*\k<tool>\s*>", RegexOptions.Singleline);

            if (matches.Count>0)
            {
                StringBuilder sb = new StringBuilder();

                foreach (Match match in matches)
                {
                    try
                    {
                        string toolName = match.Groups["tool"].Value;
                        if (toolName == "reasoning" || toolName == "think") continue;

                        AITool? selectedTool = Tools.Where((tool) => { return tool.Name == toolName; }).First();
                        if (selectedTool == null) return null;

                        // Notify tool call start
                        chatControl?.ToolCallStarted();
                        

                        AIFunctionArguments args = new AIFunctionArguments();

                        string innerContent = match.Groups["params"].Value;
                        var paramMatches = Regex.Matches(innerContent, @"<\s*(?<key>\w+)\s*>(?<value>.*?)<\s*/\k<key>\s*>", RegexOptions.Singleline);
                        foreach (Match p in paramMatches)
                        {
                            args.Add(p.Groups["key"].Value, p.Groups["value"].Value);
                        }
                        Progress<string> progress = new Progress<string>((message) => { chatControl?.ToolCallStarted(); });
                        args.Add("progress", progress);

                        AIFunction? aIFunction = selectedTool as AIFunction;
                        CancellationTokenSource spinner_cts = new CancellationTokenSource();
                        CancellationToken spinner_cancel = spinner_cts.Token;
                        if (aIFunction == null) return "illgal function call";

                        Task task = Task.Run(async () => {
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

                        await spinner_cts.CancelAsync();
                        await task; // これなら例外を気にせず待てる
                        task.Dispose();
                        
                        string? s_ret = ret?.ToString();
                        if (s_ret != null)
                        {
                            sb.AppendLine(s_ret);
                        }
                    }
                    catch
                    {
                        sb.AppendLine("failed to parse or execute function call:" + match.Value);
                    }
                    finally
                    {
                        chatControl?.ToolCallEnded();
                    }
                }
                if (sb.Length == 0) return null;
                return sb.ToString();
            }
            return null;
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
                sb.AppendLine("<" + tool.Name + " >");
                sb.Append(usage.ToString());
                sb.AppendLine("</" + tool.Name + " >");
                sb.AppendLine("```");
                sb.AppendLine("");
            }
        }
    }
}
