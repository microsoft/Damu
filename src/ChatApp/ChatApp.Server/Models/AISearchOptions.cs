namespace ChatApp.Server.Models;

public class AISearchOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string IndexName { get; set; } = string.Empty;
    public string SemanticConfigurationName { get; set; } = string.Empty;
}
