namespace ChatApp.Server.Models;

public class CosmosOptions
{
    public string CosmosEndpoint { get; set; } = string.Empty;
    public string CosmosKey { get; set; } = string.Empty;
    public string CosmosDatabaseId { get; set; } = string.Empty;
    public string CosmosContainerId { get; set; } = string.Empty;
}
