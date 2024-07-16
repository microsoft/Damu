namespace IndexOrchestration.Models;

public class QueryResult
{
    public string IndexRecordId { get; set; } = string.Empty;
    public int NoteId { get; set; } = 0;
    public string NoteChunk { get; set; } = string.Empty;
    public string NoteChunkOrder { get; set; } = string.Empty;
    public int CSN { get; set; } = 0;
    public int MRN { get; set; } = 0;
    public string NoteType { get; set; } = string.Empty;
    public string NoteStatus { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorFirstName { get; set; } = string.Empty;
    public string AuthorLastName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; } = DateTime.UtcNow;
}

