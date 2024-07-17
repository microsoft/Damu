namespace ChatApp.Server;

public class StorageOptions
{
    public string BlobStorageEndpoint { get; set; } = string.Empty;
    public string BlobStorageConnectionString { get; set; } = string.Empty;
    public string BlobStorageContainerName { get; set; } = string.Empty;
}
