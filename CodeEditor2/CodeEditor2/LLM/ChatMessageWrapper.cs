using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.LLM
{
    public class ChatMessageWrapper : ChatMessage
    {
        public ChatMessageWrapper() : base() { }

        public ChatMessageWrapper(ChatRole role, string? content) : base(role, content) { }

        public ChatMessageWrapper(ChatRole role, IList<AIContent> contents) : base(role, contents) { }

        public ChatMessageWrapper(ChatMessage chatMessage)
        {
            AuthorName = chatMessage.AuthorName;
            CreatedAt = chatMessage.CreatedAt;
            Role = chatMessage.Role;
            Contents = chatMessage.Contents;
            MessageId = chatMessage.MessageId;
            RawRepresentation = chatMessage.RawRepresentation;
            AdditionalProperties = chatMessage.AdditionalProperties;
        }
    }
}
