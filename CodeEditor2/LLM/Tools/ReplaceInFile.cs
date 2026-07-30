using Microsoft.Extensions.AI;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.LLM.Tools
{
    public class ReplaceInFile : LLMTool
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
            <<<<<<< SEARCH
            [exact content to find]
            =======
            [new content to replace with]
            >>>>>>> REPLACE
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
                if (project == null) return "Failed to execute tool. No project selected.";

                // 1. 安全なパスの解決
                string fullPath = project.GetAbsolutePath(path);
                if (!fullPath.StartsWith(project.RootPath, StringComparison.OrdinalIgnoreCase))
                    return "Error: Permission denied.";

                if (!System.IO.File.Exists(fullPath))
                    return $"Error: File not found at '{path}'.";

                // 2. ファイル内容とエンコーディング/改行コードの判定
                // 元のファイルの改行コードを保持するために記録します
                string originalText = System.IO.File.ReadAllText(fullPath);
                bool isCrLf = originalText.Contains("\r\n");

                // 比較・置換処理はすべて LF (\n) に正規化して行います
                string fileContent = originalText.Replace("\r\n", "\n");
                string diffNormalized = diff.Replace("\r\n", "\n");

                cancellationToken.ThrowIfCancellationRequested();

                // 3. SEARCH/REPLACE ブロックのパース
                // 空のブロック（削除・先頭挿入など）にも対応できるよう、改行をオプショナル（\n?）にします
                var blockRegex = new Regex(@"<<<<<<< SEARCH\n(.*?)\n?=======\n(.*?)\n?>>>>>>> REPLACE", RegexOptions.Singleline);
                var matches = blockRegex.Matches(diffNormalized);

                if (matches.Count == 0)
                    return "Error: No valid SEARCH/REPLACE blocks found. Please check your formatting exactly.";

                string updatedContent = fileContent;

                foreach (Match match in matches)
                {
                    string searchContent = match.Groups[1].Value;
                    string replaceContent = match.Groups[2].Value;

                    // 4. 厳密一致のチェックと自己修復ヒントの提供
                    if (!updatedContent.Contains(searchContent))
                    {
                        // ヒントとなる類似コードブロックを探索
                        string hint = FindSimilarCodeBlock(updatedContent, searchContent);

                        return $"Error: Could not find the EXACT original content in '{path}'. " +
                               $"Make sure you included ALL whitespace, indentation, and comments exactly as they appear in the file. " +
                               $"Do not truncate lines.{hint}";
                    }
                    //// 4. 厳密一致のチェック
                    //if (!updatedContent.Contains(searchContent))
                    //{
                    //    // LLMへの親切なエラーメッセージ（自己修復を促す）
                    //    return $"Error: Could not find the EXACT original content in '{path}'. " +
                    //           $"Make sure you included ALL whitespace, indentation, and comments exactly as they appear in the file. " +
                    //           $"Do not truncate lines.";
                    //}

                    // 最初の1箇所のみ置換
                    int index = updatedContent.IndexOf(searchContent);
                    updatedContent = updatedContent.Remove(index, searchContent.Length).Insert(index, replaceContent);

                    // ※ここでは updatedContent.Replace("\r\n", "\n") は不要です（既にLF正規化済みのため）
                }

                // 5. 元の改行コードに復元
                if (isCrLf)
                {
                    updatedContent = updatedContent.Replace("\n", "\r\n");
                }

                // 6. 保存（元のエンコーディングを極力維持するため、元のファイルのEncodingを使用するか、BOM付き/無しを意識する）
                // UTF-8 (BOMあり) を維持したい場合は、File.WriteAllTextにエンコーディングを指定します
                var encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false); // 必要に応じてBOMを判定
                System.IO.File.WriteAllText(fullPath, updatedContent, encoding);

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

        // 類似するコードブロックを探索してヒント文字列を生成するメソッド
        private string FindSimilarCodeBlock(string fileContent, string searchContent)
        {
            var fileLines = fileContent.Split('\n');
            var searchLines = searchContent.Split('\n');
            int searchLineCount = searchLines.Length;

            // 検索行数がファイル行数より多い場合などはスキップ
            if (searchLineCount == 0 || fileLines.Length < searchLineCount)
                return string.Empty;

            int bestDistance = int.MaxValue;
            string bestMatch = null;

            // ファイル全体を1行ずつスライドしながら、同じ行数のブロックを比較する
            for (int i = 0; i <= fileLines.Length - searchLineCount; i++)
            {
                // 検索ブロックと同じ行数を切り出す
                var windowLines = new string[searchLineCount];
                Array.Copy(fileLines, i, windowLines, 0, searchLineCount);
                string candidate = string.Join("\n", windowLines);

                // レーベンシュタイン距離を計算
                int distance = ComputeLevenshteinDistance(searchContent, candidate);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestMatch = candidate;
                }
            }

            // 類似度（0.0 〜 1.0）を計算
            int maxLength = Math.Max(searchContent.Length, bestMatch?.Length ?? 1);
            double similarity = 1.0 - ((double)bestDistance / maxLength);

            // 類似度が一定以上（例：50%以上）の場合のみヒントとして返す
            // 全く関係ないコードを返してLLMを混乱させないための閾値
            if (similarity > 0.5)
            {
                return $"\n\nDid you mean:\n<<<<<<< SEARCH\n{bestMatch}\n=======\n\n(Similarity: {similarity:P0})";
            }

            return string.Empty;
        }

        // Myers' Bit-Vector Algorithm for Levenshtein distance
        // O(n * m / w) where w is word size (64 bits), significantly faster than standard DP
        // Reference: "A Fast Bit-Vector Algorithm for Approximate String Matching" by Myers
        private int ComputeLevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;

            // For very short strings, use standard DP for efficiency
            if (m <= 0 || n <= 0)
            {
                return m > 0 ? m : n;
            }

            // Use standard DP for short strings (faster due to less overhead)
            if (m <= 64 || n <= 64)
            {
                return ComputeLevenshteinDistanceDP(s, t);
            }

            // Myers' bit-vector algorithm for longer strings
            // Precompute character bitmasks for pattern string s
            ulong[] charMasks = new ulong[256];
            for (int i = 0; i < 256; i++)
            {
                charMasks[i] = 0;
            }

            for (int i = 0; i < n; i++)
            {
                char c = s[i];
                charMasks[(byte)c] |= (1UL << i);
            }

            // Initialize
            ulong Pv = ~0UL;           // Previous vector
            ulong Mv = 0UL;            // Match vector
            ulong Score = (ulong)m;    // Current score

            // Main loop
            for (int j = 0; j < m; j++)
            {
                char tChar = t[j];
                ulong charMask = charMasks[(byte)tChar];

                // Compute the new vectors
                // Eq = charMask; (characters that match)
                // Xv = Mv | ~ (Eq | currentRow);
                // Xh = (((Eq & Pv) + Pv) ^ Pv) | Eq;
                
                ulong Eq = charMask;
                ulong Xv = Mv | ~(Eq | ((ulong)Score - 1));
                ulong Xh = (((Eq & Pv) + Pv) ^ Pv) | Eq;

                // Update Mv (horizontal vectors)
                ulong Ph = Mv | ~(Xh | ((ulong)Score));
                ulong Mh = Score + (~Xh & ((ulong)Score - 1));

                // Compute score for this column
                // Score = (Score + 1) & ~Mh | Xh ^ Pv ^ Mv;
                if ((Mh & (1UL << (n - 1))) != 0)
                    Score--;
                if ((Ph & (1UL << (n - 1))) != 0)
                    Score++;

                // Update vectors for next iteration
                // Pv = (Mh << 1) | ~(Xh << 1 | Eq | ((ulong)Score - 1));
                // Mv = (Xh << 1) & (Mh << 1);
                Pv = (Mh << 1) | ~(Xh << 1 | Eq | ((ulong)Score - 1));
                Mv = (Xh << 1) & (Mh << 1);
            }

            return (int)Score;
        }

        // Standard DP algorithm for short strings (optimal for small inputs)
        private int ComputeLevenshteinDistanceDP(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;

            // 1次元配列2つで2次元配列の代用（row-swapping technique）
            int[] previousRow = new int[m + 1];
            int[] currentRow = new int[m + 1];

            // 初期化: previousRow = [0, 1, 2, ..., m]
            for (int j = 0; j <= m; j++)
            {
                previousRow[j] = j;
            }

            for (int i = 1; i <= n; i++)
            {
                currentRow[0] = i;

                for (int j = 1; j <= m; j++)
                {
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    currentRow[j] = Math.Min(
                        Math.Min(previousRow[j] + 1, currentRow[j - 1] + 1),
                        previousRow[j - 1] + cost);
                }

                // 行を交換
                int[] temp = previousRow;
                previousRow = currentRow;
                currentRow = temp;
            }

            // 最後の交換後のpreviousRowが結果を含む
            return previousRow[m];
        }
    }
}
