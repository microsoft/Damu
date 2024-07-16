namespace ChatApp.Server.Models;

public class ChatCompletion
{
    public string Id { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string Object { get; set; } = string.Empty;
    public List<ChoiceModel> Choices { get; set; } = [];
    public Dictionary<string, string> HistoryMetadata { get; set; } = [];
    public string ApimRequestId { get; set; } = string.Empty;
}

public class ChoiceModel
{
    public List<Message> Messages { get; set; } = [];
}
