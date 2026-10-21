using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.LLM
{
    /*
    ChartControl
        SetModel(ILLMChatFrontEnd chatModel, LLMAgent? agent)
        
        ILLMChartFrontEndとagentを設定して動作させる chat UI

            ILLMChatFrontEnd : LLMアクセスラッパー
            agent : LLM agent動作に必要な各種設定/callbackをもつ
        


     
    */


    /// <summary>
    /// Notification describing one native (API) tool call lifecycle event
    /// (start or finish) raised by the chat frontend while a function-calling
    /// model turn streams. The chat UI consumes these to render tool calls
    /// exactly like the pseudo (message-based) tool calls, which are signalled
    /// directly by <see cref="LLMAgent"/> instead:
    /// a spinner is shown while the tool runs and the result text is appended
    /// (and kept in the history) when it finishes.
    /// </summary>
    public class ToolCallNotificationEventArgs : System.EventArgs
    {
        /// <summary>
        /// Provider-assigned id of the tool call. Used to pair the start and the
        /// finish notification of the same call. When the provider does not
        /// supply one, the frontend synthesises an id so pairing still works.
        /// </summary>
        public string CallId { get; }

        /// <summary>Name of the function being invoked (empty for a finish event).</summary>
        public string ToolName { get; }

        /// <summary>True for a "tool started" event, false for a "tool finished" event.</summary>
        public bool IsStarting { get; }

        /// <summary>Result text of the tool execution. Only set for a finish event.</summary>
        public string? Result { get; }

        public ToolCallNotificationEventArgs(string callId, string toolName, bool isStarting, string? result = null)
        {
            CallId = callId ?? "";
            ToolName = toolName ?? "";
            IsStarting = isStarting;
            Result = result;
        }
    }

    public interface ILLMChatFrontEnd
    {
        /// <summary>
        /// Raised by the chat frontend while a native (API) function-calling turn
        /// streams: once when each tool call starts and once when it finishes.
        /// This lets the chat UI display tool calls (spinner while running, result
        /// text + history entry when finished) even though the tool loop runs
        /// inside the frontend (MEAI function-invocation middleware) rather than
        /// in the agent's message parser.
        /// Implementers that never perform native tool calls may leave the event
        /// unused (never raised). The chat UI subscribes defensively.
        /// </summary>
        event System.EventHandler<ToolCallNotificationEventArgs>? ToolCallNotification;

        /// <summary>
        /// The currently selected model (null when not yet initialized)
        /// </summary>
        ModelItem? CurrentModel { get; }

        /// <summary>
        /// Human-readable summary of the token usage of the most recent LLM
        /// response (e.g. "tokens: 1234 in / 567 out / 1801 total").
        /// null when the provider did not report usage.
        /// This is display-only metadata and is never part of the message
        /// content sent to the LLM.
        /// </summary>
        string? LastUsageInfo { get; }

        IAsyncEnumerable<string> GetAsyncCollectionChatResult(string command, IList<AITool>? tools, CancellationToken cancellation);
        Task<string> GetAsyncChatResult(string command, IList<AITool>? tools, CancellationToken cancellationToken);

        Task SaveMessagesAsync(string filePath);
        Task LoadMessagesAsync(string filePath);

        List<ChatMessageWrapper> ChatMessageWrappers { get; }
        Task ResetAsync();

        /// <summary>
        /// Gets the list of available models
        /// </summary>
        List<ModelItem> GetAvailableModels();

        /// <summary>
        /// Sets the current model
        /// </summary>
        /// <param name="modelItem">The model to set as current</param>
        Task SetModelAsync(ModelItem modelItem);

        /// <summary>
        /// Attempts to reconnect to the LLM service.
        /// Returns true if reconnection was successful, false otherwise.
        /// </summary>
        /// <returns>Task with boolean indicating success</returns>
        Task<bool> TryReconnectAsync();

        /// <summary>
        /// Removes the last user message from history (used when retrying after connection failure).
        /// </summary>
        void RemoveLastUserMessage();
    }
}
