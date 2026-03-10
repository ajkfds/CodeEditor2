using CodeEditor2.Data;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.LLM.Tools
{
    public class ReplaceInFile: LLMTool
    {
        public ReplaceInFile(Data.Project project) : base(project) { }
        /* 
       reference from Cline prompt 

       ## replace_in_file
       Description: Request to replace sections of content in an existing file using SEARCH/REPLACE blocks that define exact changes to specific parts of the file. This tool should be used when you need to make targeted changes to specific parts of a file.
       Parameters:
       - path: (required) The path of the file to modify (relative to the current working directory ${cwd.toPosix()})
       - diff: (required) One or more SEARCH/REPLACE blocks following this exact format:
         \`\`\`
         ------- SEARCH
         [exact content to find]
         =======
         [new content to replace with]
         +++++++ REPLACE
         \`\`\`
         Critical rules:
         1. SEARCH content must match the associated file section to find EXACTLY:
            * Match character-for-character including whitespace, indentation, line endings
            * Include all comments, docstrings, etc.
         2. SEARCH/REPLACE blocks will ONLY replace the first match occurrence.
            * Including multiple unique SEARCH/REPLACE blocks if you need to make multiple changes.
            * Include *just* enough lines in each SEARCH section to uniquely match each set of lines that need to change.
            * When using multiple SEARCH/REPLACE blocks, list them in the order they appear in the file.
         3. Keep SEARCH/REPLACE blocks concise:
            * Break large SEARCH/REPLACE blocks into a series of smaller blocks that each change a small portion of the file.
            * Include just the changing lines, and a few surrounding lines if needed for uniqueness.
            * Do not include long runs of unchanging lines in SEARCH/REPLACE blocks.
            * Each line must be complete. Never truncate lines mid-way through as this can cause matching failures.
         4. Special operations:
            * To move code: Use two SEARCH/REPLACE blocks (one to delete from original + one to insert at new location)
            * To delete code: Use empty REPLACE section
       ${focusChainSettings.enabled ? `- task_progress: (optional) A checklist showing task progress after this tool use is completed. (See 'Updating Task Progress' section for more details)` : ""}
       Usage:
       <replace_in_file>
       <path>File path here</path>
       <diff>
       Search and replace blocks here
       </diff>
       ${
           focusChainSettings.enabled
               ? `<task_progress>
       Checklist here (optional)
       </task_progress>`
               : ""
       }
       </replace_in_file>         
        */
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "replace_in_file"); }

        public override string XmlExample { get; } = """
            ```xml
            <replace_in_file>
            <path>File path here</path>
            <diff>
            Search and replace blocks here
            </diff>
            </replace_in_file>         
            ```
            """;


        [Description("""
            Request to replace sections of content in an existing file using SEARCH/REPLACE blocks that define exact changes to specific parts of the file. 
            This tool should be used when you need to make targeted changes to specific parts of a file.
            譖ｴ譁ｰ縺ｯ縺ｪ繧九∋縺冗ｴｰ蛻・喧縺励∬､・焚蝗槭↓蛻・￠縺ｦ譖ｴ譁ｰ縺吶ｋ縺薙→縲・            """)]
        public async Task<string> Run(
        [Description("The path of the file to modify (relative to the project root directory")]
        string path,
        [Description("""
            One or more SEARCH/REPLACE blocks following this exact format:
            ```
            ------- SEARCH
            [exact content to find]
            =======
            [new content to replace with]
            +++++++ REPLACE
            ```
            Critical rules:
            1. SEARCH content must match the associated file section to find EXACTLY:
               * Match character-for-character including whitespace, indentation, line endings
               * Include all comments, docstrings, etc.
            2. SEARCH/REPLACE blocks will ONLY replace the first match occurrence.
               * Including multiple unique SEARCH/REPLACE blocks if you need to make multiple changes.
               * Include *just* enough lines in each SEARCH section to uniquely match each set of lines that need to change.
               * When using multiple SEARCH/REPLACE blocks, list them in the order they appear in the file.
            3. Keep SEARCH/REPLACE blocks concise:
               * Break large SEARCH/REPLACE blocks into a series of smaller blocks that each change a small portion of the file.
               * Include just the changing lines, and a few surrounding lines if needed for uniqueness.
               * Do not include long runs of unchanging lines in SEARCH/REPLACE blocks.
               * Each line must be complete. Never truncate lines mid-way through as this can cause matching failures.
            4. Special operations:
               * To move code: Use two SEARCH/REPLACE blocks (one to delete from original + one to insert at new location)
               * To delete code: Use empty REPLACE section
            """)]
            

        string diff,
        CancellationToken cancellationToken
        )
        {
            try
            {
                if (project == null) return "Failed to execute tool.No project selected.";

                // 1. 螳牙・縺ｪ繝代せ縺ｮ隗｣豎ｺ
                string fullPath = project.GetAbsolutePath(path);
                if (!fullPath.StartsWith(project.RootPath, StringComparison.OrdinalIgnoreCase))
                    return "Error: Permission denied.";

                if (!System.IO.File.Exists(fullPath))
                    return $"Error: File not found at '{path}'.";

                // 2. 繝輔ぃ繧､繝ｫ蜀・ｮｹ縺ｮ隱ｭ縺ｿ霎ｼ縺ｿ・域隼陦後さ繝ｼ繝峨ｒ豁｣隕丞喧縺励※謇ｱ縺・・縺後さ繝・ｼ・                string fileContent = System.IO.File.ReadAllText(fullPath).Replace("\r\n", "\n");

                cancellationToken.ThrowIfCancellationRequested();

                // 3. SEARCH/REPLACE 繝悶Ο繝・け縺ｮ繝代・繧ｹ
                // 豁｣隕剰｡ｨ迴ｾ縺ｧ繝悶Ο繝・け繧呈歓蜃ｺ縺励∪縺・                var blockRegex = new Regex(@"------- SEARCH\r?\n(.*?)\r?\n=======\r?\n(.*?)\r?\n\+\+\+\+\+\+\+ REPLACE", RegexOptions.Singleline);
                var matches = blockRegex.Matches(diff.Replace("\r\n", "\n"));

                if (matches.Count == 0)
                    return "Error: No valid SEARCH/REPLACE blocks found. Please check your format.";

                string updatedContent = fileContent;

                foreach (Match match in matches)
                {
                    string searchContent = match.Groups[1].Value;
                    string replaceContent = match.Groups[2].Value;

                    // 4. 蜴ｳ蟇・ｸ閾ｴ縺ｮ繝√ぉ繝・け
                    // 豕ｨ諢・ LLM縺ｯ譎ゅ・隼陦後さ繝ｼ繝峨ｒ豺ｷ蜷後☆繧九◆繧√《tring.Replace繧定ｩｦ縺ｿ繧句燕縺ｫ蟄伜惠繧堤｢ｺ隱・                    if (!updatedContent.Contains(searchContent))
                    {
                        return $"Error: Could not find the EXACT original content in '{path}'. Make sure whitespace and indentation match perfectly.";
                    }

                    // 譛蛻昴・1邂・園縺ｮ縺ｿ鄂ｮ謠幢ｼ域欠遉ｺ縺ｮ繝ｫ繝ｼ繝ｫ騾壹ｊ・・                    int index = updatedContent.IndexOf(searchContent);
                    updatedContent = updatedContent.Remove(index, searchContent.Length).Insert(index, replaceContent);
                }

                // 5. 菫晏ｭ・                System.IO.File.WriteAllText(fullPath, updatedContent);

                await Task.Delay(0);
                return $"Success: Applied {matches.Count} change(s) to '{path}'.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
            /*
            謾ｹ陦後さ繝ｼ繝峨・鄂: LLM縺檎函謌舌☆繧・\n 縺ｨ縲仝indows荳翫・繝輔ぃ繧､繝ｫ縺ｮ \r\n 縺御ｸ閾ｴ縺帙★繝槭ャ繝√Φ繧ｰ縺ｫ螟ｱ謨励☆繧九％縺ｨ縺後ｈ縺上≠繧翫∪縺吶・            繧医ｊ蝣・欧縺ｫ縺吶ｋ縺ｪ繧峨《earchContent 縺ｨ fileContent 縺ｮ荳｡譁ｹ縺九ｉ \r 繧帝勁蜴ｻ縺励※豈碑ｼ・☆繧九°縲´LM縺ｫ縲悟ｸｸ縺ｫLF・・n・峨〒蜃ｺ蜉帙○繧医阪→繧ｷ繧ｹ繝・Β繝励Ο繝ｳ繝励ヨ縺ｧ蠕ｹ蠎輔＆縺帙ｋ縺ｮ縺悟ｮ夂浹縺ｧ縺吶・            繧､繝ｳ繝・Φ繝医・諢溷ｺｦ: LLM縺ｯ譎ゅ・ち繝悶→繧ｹ繝壹・繧ｹ繧帝俣驕輔∴縺ｾ縺吶ゅｂ縺励・繝・メ繝ｳ繧ｰ螟ｱ謨励′螟夂匱縺吶ｋ蝣ｴ蜷医・縲《tring.Contains 縺ｮ莉｣繧上ｊ縺ｫ縲檎ｩｺ逋ｽ縺ｮ蟾ｮ逡ｰ繧堤┌隕悶☆繧区ｭ｣隕剰｡ｨ迴ｾ縲阪ｒ蜍慕噪縺ｫ逕滓・縺励※繝槭ャ繝√Φ繧ｰ縺輔○繧区焔豕輔ｂ縺ゅｊ縺ｾ縺吶・            繝悶Ο繝・け縺ｮ鬆・ｺ・ 蠑墓焚縺ｮ隱ｬ譏弱↓縺ゅｋ騾壹ｊ縲∬､・焚縺ｮ鄂ｮ謠帙′縺ゅｋ蝣ｴ蜷医・縲後ヵ繧｡繧､繝ｫ縺ｮ蜈磯ｭ縺九ｉ鬆・↓縲榊・逅・＠縺ｪ縺・→縲∫ｽｮ謠帛ｾ後・繧､繝ｳ繝・ャ繧ｯ繧ｹ縺後ぜ繝ｬ縺ｦ莠域悄縺帙〓蝣ｴ謇繧呈嶌縺肴鋤縺医ｋ繝ｪ繧ｹ繧ｯ縺後≠繧翫∪縺吶・            
             */
        }
    }
}
