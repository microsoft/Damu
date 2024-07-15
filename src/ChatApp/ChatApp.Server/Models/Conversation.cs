using System.Text.Json.Serialization;

namespace ChatApp.Server.Models;

// todo: change type from string to enum?
public class Conversation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonPropertyName("type")]
    public string Type { get; set; } = "conversation"; // other value types?
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new List<Message>();
}
