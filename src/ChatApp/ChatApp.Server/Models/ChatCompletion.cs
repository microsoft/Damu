namespace ChatApp.Server.Models;

public class ChatCompletion
{
    public string Id { get; set; }
    public string Model { get; set; }
    public DateTime Created { get; set; }
    public string Object { get; set; }
    public List<ChoiceModel> Choices { get; set; }
    public Dictionary<string, string> HistoryMetadata { get; set; }
    public string ApimRequestId { get; set; }
}

public class ChoiceModel
{
    public List<Message> Messages { get; set; }
}