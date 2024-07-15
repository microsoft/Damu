using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace ChatApp.Server.Models;

public class Message
{
    public string Id { get; set; } = string.Empty;
    [JsonConverter(typeof(StringEnumConverter))]
    public AuthorRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public ChatMessageContent ToChatMessageContent()
    {
        // todo: is there anything else we can do here, maybe with date?
        // or author? https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.chatmessagecontent?view=semantic-kernel-dotnet
        return new ChatMessageContent
        {
            Role = Role,
            Content = Content,
        };
    }
}

// todo: figure out how these marry up
public class HistoryMessage : Message
{
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Feedback { get; set; } = string.Empty;    
}
