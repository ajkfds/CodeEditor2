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
            更新はなるべく細分化し、複数回に分けて更新すること。
            """)]
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

                // 1. 安全なパスの解決
                string fullPath = project.GetAbsolutePath(path);
                if (!fullPath.StartsWith(project.RootPath, StringComparison.OrdinalIgnoreCase))
                    return "Error: Permission denied.";

                if (!System.IO.File.Exists(fullPath))
                    return $"Error: File not found at '{path}'.";

                // 2. ファイル内容の読み込み（改行コードを正規化して扱うのがコツ）
                string fileContent = System.IO.File.ReadAllText(fullPath).Replace("\r\n", "\n");

                cancellationToken.ThrowIfCancellationRequested();

                // 3. SEARCH/REPLACE ブロックのパース
                // 正規表現でブロックを抽出します
                var blockRegex = new Regex(@"------- SEARCH\r?\n(.*?)\r?\n=======\r?\n(.*?)\r?\n\+\+\+\+\+\+\+ REPLACE", RegexOptions.Singleline);
                var matches = blockRegex.Matches(diff.Replace("\r\n", "\n"));

                if (matches.Count == 0)
                    return "Error: No valid SEARCH/REPLACE blocks found. Please check your format.";

                string updatedContent = fileContent;

                foreach (Match match in matches)
                {
                    string searchContent = match.Groups[1].Value;
                    string replaceContent = match.Groups[2].Value;

                    // 4. 厳密一致のチェック
                    // 注意: LLMは時々改行コードを混同するため、string.Replaceを試みる前に存在を確認
                    if (!updatedContent.Contains(searchContent))
                    {
                        return $"Error: Could not find the EXACT original content in '{path}'. Make sure whitespace and indentation match perfectly.";
                    }

                    // 最初の1箇所のみ置換（指示のルール通り）
                    int index = updatedContent.IndexOf(searchContent);
                    updatedContent = updatedContent.Remove(index, searchContent.Length).Insert(index, replaceContent);
                }

                // 5. 保存
                System.IO.File.WriteAllText(fullPath, updatedContent);

                await Task.Delay(0);
                return $"Success: Applied {matches.Count} change(s) to '{path}'.";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
            /*
            改行コードの罠: LLMが生成する \n と、Windows上のファイルの \r\n が一致せずマッチングに失敗することがよくあります。
            より堅牢にするなら、searchContent と fileContent の両方から \r を除去して比較するか、LLMに「常にLF（\n）で出力せよ」とシステムプロンプトで徹底させるのが定石です。
            インデントの感度: LLMは時々タブとスペースを間違えます。もしマッチング失敗が多発する場合は、string.Contains の代わりに「空白の差異を無視する正規表現」を動的に生成してマッチングさせる手法もあります。
            ブロックの順序: 引数の説明にある通り、複数の置換がある場合は「ファイルの先頭から順に」処理しないと、置換後のインデックスがズレて予期せぬ場所を書き換えるリスクがあります。             
             */
        }
    }
}
