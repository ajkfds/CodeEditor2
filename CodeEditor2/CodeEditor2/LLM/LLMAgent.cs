using Avalonia.Threading;
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
        //        public string Role { get; set; } = "a highly skilled software engineer with extensive knowledge in many programming languages, frameworks, design patterns, and best practices";

        public async Task<string?> ParseResponceAsync(string responce, CancellationToken cancellationToken)
        {
            if (PersudoFunctionCallMode)
            {
                string? funcResult = await ParseExecutePersudoFunctionCall(responce, cancellationToken);
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
        private async Task<string?> ParseExecutePersudoFunctionCall(string responce, CancellationToken cancellationToken)
        {
            var match = Regex.Match(responce, @"<(?<tool>\w+)>(?<params>.*?)</\k<tool>>", RegexOptions.Singleline);

            if (match.Success)
            {
                try
                {
                    string toolName = match.Groups["tool"].Value;
                    AITool? selectedTool = Tools.Where((tool) => { return tool.Name == toolName; }).First();
                    if (selectedTool == null) return null;

                    AIFunctionArguments args = new AIFunctionArguments();
                    string innerContent = match.Groups["params"].Value;
                    var paramMatches = Regex.Matches(innerContent, @"<(?<key>\w+)>(?<value>.*?)</\k<key>>");
                    foreach (Match p in paramMatches)
                    {
                        args.Add(p.Groups["key"].Value, p.Groups["value"].Value);
                    }
                    AIFunction? aIFunction = selectedTool as AIFunction;
                    if (aIFunction == null) return "illgal function call";
                    object? ret = await aIFunction.InvokeAsync(args, cancellationToken);
                    string? s_ret = ret?.ToString();
                    if (s_ret != null)
                    {
                        return s_ret;
                    }
                }
                catch
                {
                    return "illagal function call";
                }
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
