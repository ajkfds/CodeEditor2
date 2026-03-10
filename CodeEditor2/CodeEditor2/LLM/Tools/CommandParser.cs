using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeEditor2.LLM.Tools
{
    public class CommandParser
    {
        public enum SeparatorType { None, Sync, And, Or } // ; , &&, ||

        public class CommandChain
        {
            public List<string> PipeCommands { get; set; } = new(); // ["ls -la", "grep .txt"]
            public SeparatorType NextSeparator { get; set; } = SeparatorType.None;
        }
        public async Task<string> RunCommandAsync(string fullCommand,string rootPath,HashSet<string> allowedCommands)
        {
            // 1. 繧ｳ繝槭Φ繝峨ｒ繝代・繧ｹ縺励※讒矩蛹悶☆繧・
            var chains = ParseFullCommandLine(fullCommand);
            var finalOutput = new StringBuilder();
            int lastExitCode = 0;

            foreach (var chain in chains)
            {
                // 蜑阪・繧ｳ繝槭Φ繝峨・邨先棡縺ｫ蝓ｺ縺･縺丞ｮ溯｡悟愛螳・
                if (chain.NextSeparator == SeparatorType.And && lastExitCode != 0) break;
                // (蠢・ｦ√↓蠢懊§縺ｦ SeparatorType.Or 縺ｮ螳溯｣・ｂ蜿ｯ閭ｽ)

                // 繝代う繝励Λ繧､繝ｳ繧貞ｮ溯｡・
                var (output, exitCode) = await ExecutePipeChainAsync(chain.PipeCommands,rootPath,allowedCommands);
                finalOutput.Append(output);
                lastExitCode = exitCode;

                if (chain.NextSeparator == SeparatorType.None) break;
            }

            finalOutput.AppendLine();
            finalOutput.AppendLine("command complete, ExitCode:"+lastExitCode.ToString());

            return finalOutput.ToString();
        }

        private List<CommandChain> ParseFullCommandLine(string input)
        {
            var chains = new List<CommandChain>();
            // && 繧・; 縺ｧ蛻・牡縺吶ｋ豁｣隕剰｡ｨ迴ｾ (繧ｯ繧ｩ繝ｼ繝亥・縺ｯ辟｡隕・
            // 邁｡譏鍋沿縺ｨ縺励※縲√∪縺壹・蜊倡ｴ斐↑蛻・牡縺ｧ螳溯｣・ｾ九ｒ遉ｺ縺励∪縺・
            var patterns = Regex.Split(input, @"(&&|;)");

            var currentChain = new CommandChain();
            foreach (var part in patterns)
            {
                var trimmed = part.Trim();
                if (trimmed == "&&")
                {
                    currentChain.NextSeparator = SeparatorType.And;
                    chains.Add(currentChain);
                    currentChain = new CommandChain();
                }
                else if (trimmed == ";")
                {
                    currentChain.NextSeparator = SeparatorType.Sync;
                    chains.Add(currentChain);
                    currentChain = new CommandChain();
                }
                else
                {
                    // 繝代う繝励〒蛻・牡縺励※譬ｼ邏・
                    currentChain.PipeCommands = trimmed.Split('|').Select(p => p.Trim()).ToList();
                }
            }
            chains.Add(currentChain);
            return chains;
        }

        private async Task<(string Output, int ExitCode)> ExecutePipeChainAsync(List<string> pipeParts,string rootPath, HashSet<string> allowedCommands)
        {
            var processes = new List<Process>();
            var outputBuilder = new StringBuilder();
            int lastExitCode = 0;

            try
            {
                Stream lastOutputStream = null;

                for (int i = 0; i < pipeParts.Count; i++)
                {
                    var tokens = ParseArguments(pipeParts[i]);
                    if (tokens.Count == 0) continue;

                    var cmd = tokens[0];
                    var args = string.Join(" ", tokens.Skip(1));

                    // 繧ｻ繧ｭ繝･繝ｪ繝・ぅ・夊ｨｱ蜿ｯ繝ｪ繧ｹ繝育・蜷・
                    if (!allowedCommands.Contains(cmd.ToLower()))
                        return ($"Error: '{cmd}' is not allowed.", -1);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = cmd,
                        Arguments = args,
                        WorkingDirectory = rootPath,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                        CreateNoWindow = true
                    };

                    var proc = Process.Start(startInfo);
                    processes.Add(proc);

                    // 繝代う繝励・謗･邯・(蜑阪・繝励Ο繧ｻ繧ｹ縺ｮ蜃ｺ蜉帙ｒ莉翫・繝励Ο繧ｻ繧ｹ縺ｮ蜈･蜉帙∈)
                    if (lastOutputStream != null)
                    {
                        _ = CopyStreamAsync(lastOutputStream, proc.StandardInput.BaseStream);
                    }

                    lastOutputStream = proc.StandardOutput.BaseStream;
                }

                // 譛蠕後・繝励Ο繧ｻ繧ｹ縺ｮ螳御ｺ・ｒ蠕・■縲∝・蜉帙ｒ繧ｭ繝｣繝励メ繝｣
                var lastProc = processes.Last();
                var outTask = lastProc.StandardOutput.ReadToEndAsync();
                var errTask = lastProc.StandardError.ReadToEndAsync();

                await Task.WhenAll(outTask, errTask);
                await lastProc.WaitForExitAsync();

                lastExitCode = lastProc.ExitCode;
                return (outTask.Result + errTask.Result, lastExitCode);
            }
            catch (Exception ex)
            {
                return ($"Execution Error: {ex.Message}\n", -1);
            }
            finally
            {
                foreach (var p in processes) p.Dispose();
            }
        }
        /// <summary>
        /// 繧ｳ繝槭Φ繝峨Λ繧､繝ｳ譁・ｭ怜・繧偵ヨ繝ｼ繧ｯ繝ｳ・医・繝ｭ繧ｰ繝ｩ繝蜷阪→蠑墓焚・峨↓蛻・ｧ｣縺励∪縺吶・
        /// 繝繝悶Ν繧ｯ繧ｩ繝ｼ繝・・繧ｷ繝ｧ繝ｳ蜀・・繧ｹ繝壹・繧ｹ繧剃ｿ晄戟縺励√お繧ｹ繧ｱ繝ｼ繝玲枚蟄励↓繧ょｯｾ蠢懊＠縺ｾ縺吶・
        /// </summary>
        private List<string> ParseArguments(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) return new List<string>();

            var results = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            bool isEscaped = false;

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];

                // 繧ｨ繧ｹ繧ｱ繝ｼ繝玲枚蟄・(\) 縺ｮ蜃ｦ逅・
                if (c == '\\' && !isEscaped)
                {
                    isEscaped = true;
                    continue;
                }

                if (isEscaped)
                {
                    current.Append(c);
                    isEscaped = false;
                    continue;
                }

                // 繧ｯ繧ｩ繝ｼ繝・・繧ｷ繝ｧ繝ｳ (") 縺ｮ蜃ｦ逅・
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                    // 繧ｯ繧ｩ繝ｼ繝・・繧ｷ繝ｧ繝ｳ閾ｪ菴薙・繝医・繧ｯ繝ｳ縺ｫ蜷ｫ繧√↑縺・ｴ蜷医・ continue
                    // 繧ゅ＠蠑墓焚縺ｨ縺励※蜷ｫ繧√ｋ蠢・ｦ√′縺ゅｋ蝣ｴ蜷医・ current.Append(c)
                    current.Append(c);
                    continue;
                }

                // 繧ｹ繝壹・繧ｹ縺ｮ蜃ｦ逅・ｼ医け繧ｩ繝ｼ繝・・繧ｷ繝ｧ繝ｳ螟悶・縺ｿ蛹ｺ蛻・ｊ縺ｨ縺励※謇ｱ縺・ｼ・
                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        results.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            // 谿九▲縺滓枚蟄怜・繧定ｿｽ蜉
            if (current.Length > 0)
            {
                results.Add(current.ToString());
            }

            return results;
        }
        private async Task CopyStreamAsync(Stream input, Stream output)
        {
            try
            {
                using (output) await input.CopyToAsync(output);
            }
            catch { /* 繝代う繝励′髢峨§縺溷ｴ蜷医・蜃ｦ逅・*/ }
        }
    }
}
