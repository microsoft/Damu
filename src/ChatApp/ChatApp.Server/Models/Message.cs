namespace ChatApp.Server.Models;

public class Message
{
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

// todo: figure out how these marry up
public class HistoryMessage : Message
{
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Feedback { get; set; } = string.Empty;    
}
