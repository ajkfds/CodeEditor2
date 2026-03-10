using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CodeEditor2.LLM.Tools
{
    public class ExecuteCommand : LLMTool
    {
        public ExecuteCommand(Data.Project project) : base(project) { }
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "execute_command"); }

        public override string XmlExample { get; } = """
            ```xml
            <execute_command>
            <command>Your command here</command>
            </execute_command>         
            ```
            """;

        private static readonly HashSet<string> AllowedCommands = new() { "dotnet", "git", "ls", "dir" };


        [Description("""
            Request to execute a CLI command on the system. 
            Use this when you need to perform system operations or run specific commands to accomplish any step in the user's task. 
            You must tailor your command to the user's system and provide a clear explanation of what the command does. 
            For command chaining, use the appropriate chaining syntax for the user's shell. 
            Prefer to execute complex CLI commands over creating executable scripts, as they are more flexible and easier to run. 
            Commands will be executed in the current project root directory.
            
            `,
            """)]
        public async Task<string> Run(
            [Description("""
            The CLI command to execute.
            This should be valid for the current operating system.
            Ensure the command is properly formatted and does not contain any harmful instructions.
            荳蠎ｦ縺ｫ蜊倅ｸ縺ｮ繧ｳ繝槭Φ繝峨・螳溯｡後＠縺玖ｨｱ蜿ｯ縺輔ｌ縺ｾ縺帙ｓ縲・&繧・ヱ繧､繝礼ｭ峨ｒ菴ｿ縺｣縺ｦ隍・焚縺ｮ繧ｳ繝槭Φ繝峨ｒ蜷梧凾縺ｫ螳溯｡後＠縺ｪ縺・〒縺上□縺輔＞縲・
            
            """)]
            string command
            )
        {
            try
            {
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                bool isSafe;
                string reason;

                (isSafe,reason) = ValidateCommand(command);
                if (!isSafe)
                {
                    return reason;
                }

                string mainCommand = command.Split(' ')[0].ToLower();
                if (!AllowedCommands.Contains(mainCommand))
                {
                    return $"Error: The command '{mainCommand}' is not in the allowlist.";
                }


                mainCommand = command.Split(' ')[0];
                string argument = command.Substring(mainCommand.Length).TrimStart();


                CodeEditor2.Tools.YesNoWindow yesNoWindow = new CodeEditor2.Tools.YesNoWindow("execure_command request",$"Do you want to execute the following command? :{command}");
                await yesNoWindow.ShowDialog(CodeEditor2.Controller.GetMainWindow());
                if(!yesNoWindow.Yes) return "command_execute rejected by user";

                CommandParser parser = new CommandParser();

                string result = await parser.RunCommandAsync(command, project.RootPath, AllowedCommands);

                return result;
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



        public static (bool IsSafe, string Reason) ValidateCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return (false, "Command is empty.");

            // 1. 邨ｶ蟇ｾ縺ｫ遖∵ｭ｢縺吶ｋ繧ｭ繝ｼ繝ｯ繝ｼ繝・(繧ｷ繧ｹ繝・Β遐ｴ螢顔ｳｻ)
            var blackList = new[]
            {
                "rm -rf", "rd /s", "format", "mkfs",
                "chmod -R 777", "chown",
                "del /f /s /q", "shutdown", "reboot"
            };

            foreach (var forbidden in blackList)
            {
                if (command.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                    return (false, $"Forbidden keyword detected: {forbidden}");
            }

            // 2. 迚ｹ讓ｩ譏・ｼ縺ｮ遖∵ｭ｢
            var sudoList = new[] { "sudo", "runas", "su " };
            if (sudoList.Any(s => command.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
                return (false, "Elevation of privilege (sudo/runas) is not allowed.");

            // 3. 迺ｰ蠅・､画焚縺ｮ隱ｭ縺ｿ蜿悶ｊ/譖ｸ縺崎ｾｼ縺ｿ (OS縺ｫ繧医▲縺ｦ險俶ｳ輔′逡ｰ縺ｪ繧九◆繧∵ｳｨ諢・
            if (command.Contains("printenv") || command.Contains("set ") || command.Contains("export "))
                return (false, "Direct environment variable manipulation is restricted.");

            // 4. 繝代う繝励ｄ繝ｪ繝繧､繝ｬ繧ｯ繝医↓繧医ｋ螟夜Κ繝輔ぃ繧､繝ｫ縺ｸ縺ｮ譖ｸ縺崎ｾｼ縺ｿ/隱ｭ縺ｿ霎ｼ縺ｿ縺ｮ蛻ｶ髯・
            // 繝励Ο繧ｸ繧ｧ繧ｯ繝亥､悶・驥崎ｦ√ヵ繧｡繧､繝ｫ (passwd, shadows, win.ini遲・ 縺ｸ縺ｮ繧｢繧ｯ繧ｻ繧ｹ繧堤ｰ｡譏薙メ繧ｧ繝・け
            var sensitivePaths = new[] { "/etc/", "/dev/", "C:\\Windows\\", "%WINDIR%" };
            foreach (var path in sensitivePaths)
            {
                if (command.Contains(path, StringComparison.OrdinalIgnoreCase))
                    return (false, $"Access to system sensitive path detected: {path}");
            }

            // 5. 繝阪ャ繝医Ρ繝ｼ繧ｯ邨檎罰縺ｮ繝繧ｦ繝ｳ繝ｭ繝ｼ繝・螳溯｡・(螟夜Κ繧ｹ繧ｯ繝ｪ繝励ヨ縺ｮ螳溯｡碁亟豁｢)
            var downloadTools = new[] { "curl", "wget", "powershell iwr", "bitsadmin" };
            if (downloadTools.Any(tool => command.Contains(tool, StringComparison.OrdinalIgnoreCase)))
            {
                // 險ｱ蜿ｯ縺吶ｋ繝峨Γ繧､繝ｳ縺後≠繧句ｴ蜷医・縺薙％縺ｧ繝帙Ρ繧､繝医Μ繧ｹ繝亥愛螳壹ｒ陦後≧
                return (false, "Network download commands are restricted.");
            }

            return (true, "Success");
        }

    }

}
