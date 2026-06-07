using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;


namespace CodeEditor2.LLM
{
    // ツール実行時の情報を伝達するためのカスタムイベント引数
    public class ToolExecutionEventArgs : EventArgs
    {
        public string ToolName { get; }
        public IDictionary<string, object?>? Arguments { get; }
        public string? Result { get; } // 完了時のみセットされる

        public ToolExecutionEventArgs(string toolName, IDictionary<string, object?>? arguments, string? result = null)
        {
            ToolName = toolName;
            Arguments = arguments;
            Result = result;
        }
    }
    public interface IStatefulChatAgent
    {
        // ==========================================
        // 1. モデルの切り替え・設定
        // ==========================================
    
        /// <summary>
        /// 現在使用しているモデル名（途中で変更可能にする）
        /// </summary>
        string ModelId { get; set; }

        /// <summary>
        /// エージェント全体のデフォルトオプション（Temperatureなど）
        /// </summary>
        ChatOptions DefaultOptions { get; set; }

        // ==========================================
        // 2. messages（状態）の保存・復帰・管理
        // ==========================================

        /// <summary>
        /// 現在の会話履歴の読み取り専用ビュー
        /// </summary>
        IReadOnlyList<ChatMessage> ChatHistory { get; }

        /// <summary>
        /// 外部（DB等）から取得した履歴をロードして状態を復帰する
        /// </summary>
        void LoadHistory(IEnumerable<ChatMessage> history);

        /// <summary>
        /// 現在のセッション（履歴）をクリアする
        /// </summary>
        void ClearHistory();

        // ==========================================
        // 3. Function Calling (Tools) 対応
        // ==========================================

        /// <summary>
        /// エージェントにツール（関数）を登録する
        /// </summary>
        void RegisterTool(AITool tool);

        /// <summary>
        /// 登録されているツールを全て解除する
        /// </summary>
        void ClearTools();

        // ==========================================
        // 4. メッセージ送信とストリーミング動作
        // ==========================================

        /// <summary>
        /// 通常のメッセージ送信（一括で応答を受け取る）
        /// </summary>
        Task<ChatResponse> SendMessageAsync(
            string message,
            ChatOptions? overrideOptions = null,
            System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// ストリーミングメッセージ送信（チャンクごとに応答を受け取る）
        /// </summary>
        IAsyncEnumerable<ChatResponseUpdate> SendMessageStreamAsync(
            string message,
            ChatOptions? overrideOptions = null,
            System.Threading.CancellationToken cancellationToken = default);
    }
}
