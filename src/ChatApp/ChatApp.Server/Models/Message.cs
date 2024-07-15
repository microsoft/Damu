namespace ChatApp.Server.Models;

public class Message
{
    public Message() { }
    public Message(string conversationId, string userId, Dictionary<string, object> inputMessage)
    {
        ConversationId = conversationId;
        UserId = userId;
        // todo: revisit this logic
        Role = inputMessage["role"].ToString() ?? string.Empty;
        Content = inputMessage["content"].ToString() ?? string.Empty;
    }

    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "message";
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string ConversationId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty; // is this supposed to be a string? or is it thumbs up/down count?
}
