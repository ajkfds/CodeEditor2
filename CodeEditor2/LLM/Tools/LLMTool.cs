using Microsoft.Extensions.AI;
using System;
using System.Text;

namespace CodeEditor2.LLM.Tools
{
    // base cass to implement LLM tool call
    public abstract class LLMTool
    {
        public LLMTool(Data.Project project)
        {
            this.project = project;
        }
        protected Data.Project project;
        public abstract AIFunction GetAIFunction();

        public virtual string XmlExample { get; } = "";

        // 出力サイズ上限の既定値 (256KB)
        public const int DefaultMaxOutputBytes = 256 * 1024;

        private int maxOutputBytes = DefaultMaxOutputBytes;

        /// <summary>
        /// 1 回の tool 呼び出しで LLM に返す出力のサイズ上限 (バイト、UTF-8 換算)。
        /// 上限を超える場合はアラート (警告メッセージ) を先頭に付けたうえで、
        /// 出力の「前側 (先頭から上限バイトまで)」のみを返す (後側は切り捨てる)。
        /// 0 以下を設定した場合は既定値 (<see cref="DefaultMaxOutputBytes"/> = 256KB) に戻す。
        /// </summary>
        public int MaxOutputBytes
        {
            get => maxOutputBytes;
            set => maxOutputBytes = value > 0 ? value : DefaultMaxOutputBytes;
        }

        /// <summary>
        /// 出力が <see cref="MaxOutputBytes"/> を超えている場合、アラート (警告) を先頭に付け、
        /// 出力の前側 (先頭から上限バイトまで) のみを返す。超えていなければそのまま返す。
        /// 各 tool の戻り値は必ずこのメソッドを通すことで、LLM のコンテキスト消費を一定以下に抑える。
        /// (read_file のみ「上限超過時は内容を返さない」独自仕様のため適用しない)
        /// </summary>
        /// <param name="output">tool の生の出力</param>
        /// <param name="toolName">アラート表示用の tool 名 (例: "execute_command")</param>
        protected string LimitOutput(string output, string toolName)
        {
            if (string.IsNullOrEmpty(output)) return output;

            long totalBytes = Encoding.UTF8.GetByteCount(output);
            if (totalBytes <= MaxOutputBytes) return output;

            int cutChars = GetCharCountForByteBudget(output, MaxOutputBytes);
            string head = output.Substring(0, cutChars);

            return BuildTooLargeAlert(toolName, totalBytes, cutChars, head);
        }

        /// <summary>
        /// 出力先頭から UTF-8 で最大 <paramref name="byteBudget"/> バイトに収まる文字数を返す。
        /// サロゲートペア (絵文字等) を分断しないよう、途中で切れた場合は 1 文字戻す。
        /// </summary>
        protected static int GetCharCountForByteBudget(string text, int byteBudget)
        {
            int bytes = 0;
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                // サロゲートペアは 2 char で 1 コードポイント (UTF-8 で 4 バイト)
                bool isSurrogatePair = char.IsHighSurrogate(c)
                    && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]);
                int charCount = isSurrogatePair ? 2 : 1;
                int byteCount = Encoding.UTF8.GetByteCount(text.AsSpan(i, charCount));

                if (bytes + byteCount > byteBudget) break;

                bytes += byteCount;
                i += charCount;
            }

            // 念のためサロゲート分断を回避 (i はコードポイント境界なので通常は不要)
            if (i > 0 && i < text.Length && char.IsHighSurrogate(text[i - 1])) i--;
            return i;
        }

        /// <summary>
        /// 上限超過時のアラート (警告) + 出力前側を組み立てる。
        /// </summary>
        protected string BuildTooLargeAlert(string toolName, long totalBytes, int returnedChars, string head)
        {
            string sizeText = FormatBytes(totalBytes);
            string limitText = FormatBytes(MaxOutputBytes);
            string returnedText = FormatBytes(Encoding.UTF8.GetByteCount(head));

            var sb = new StringBuilder();
            sb.Append($"[WARNING] {toolName}: the output is too large and was truncated. ")
              .Append($"total={sizeText}, returned={returnedText} (head only), limit={limitText} ({MaxOutputBytes} bytes), ")
              .Append($"returnedChars={returnedChars}. The tail of the output was dropped. ")
              .Append("Narrow the output by a more specific query (e.g. build only the affected project with -clp:ErrorsOnly, ")
              .Append("git log -n <count>, or a more specific path / regex) if you need the dropped part.")
              .Append('\n')
              .Append("--- output (head) ---\n")
              .Append(head)
              .Append("\n--- end of truncated output ---\n");

            return sb.ToString();
        }

        protected static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} bytes";
            if (bytes < 1024L * 1024L) return $"{bytes / 1024.0:0.#} KB";
            if (bytes < 1024L * 1024L * 1024L) return $"{bytes / (1024.0 * 1024.0):0.#} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.#} GB";
        }
    }
}
