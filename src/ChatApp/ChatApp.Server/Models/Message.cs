namespace ChatApp.Server.Models;

public class Message
{
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
