using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatApp.Server.Models;

public class Message
{    
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public ChatMessageContent ToChatMessageContent()
    {
        // todo: is there anything else we can do here, maybe with date?
        // or author? https://learn.microsoft.com/en-us/dotnet/api/microsoft.semantickernel.chatmessagecontent?view=semantic-kernel-dotnet
        return new ChatMessageContent
        {
            Role = Enum.Parse<AuthorRole>(Role),
            Content = Content,
        };
    }
}

// todo: figure out how these marry up
public class HistoryMessage : Message
{
    public string Type { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Feedback { get; set; } = string.Empty;
}
