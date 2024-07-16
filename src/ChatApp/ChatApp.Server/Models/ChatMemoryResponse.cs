namespace ChatApp.Server.Models;

public class ChatMemoryResponse
{
    public List<Citation> Citations { get; set; } = [];
    public List<string> Intent { get; set; } = [];
}

public class Citation
{
    public string Content { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Filepath { get; set; } = string.Empty;
    public string ChunkId { get; set; } = string.Empty;
}
