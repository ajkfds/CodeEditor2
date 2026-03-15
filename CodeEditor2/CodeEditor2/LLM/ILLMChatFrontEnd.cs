using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


    public interface ILLMChatFrontEnd
    {
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
    }
}
