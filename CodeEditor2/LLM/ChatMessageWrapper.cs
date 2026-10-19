using Microsoft.Extensions.AI;
using System.Collections.Generic;

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

        /// <summary>
        /// Optional informational line shown at the end of the message
        /// (e.g. the token usage of the response that produced it).
        /// It is displayed to the user as a footer line, outside of the message
        /// body, so it is never part of the message content sent to the LLM and
        /// never included in <see cref="ChatMessage.Text"/>.
        /// null or an empty string hides the line.
        /// </summary>
        public string? InfoMessage { get; set; }
    }
}
