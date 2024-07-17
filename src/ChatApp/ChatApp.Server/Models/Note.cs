namespace ChatApp.Server.Models;

// consider shared library project?
public class Note
{
    public Guid? IndexRecordId { get; set; } = Guid.NewGuid();
    public long? NoteId { get; set; }
    public string? NoteContent { get; set; } = string.Empty;
    public long? CSN { get; set; }
    public long? MRN { get; set; }
    public string? NoteType { get; set; } = string.Empty;
    public string? NoteStatus { get; set; } = string.Empty;
    public string? AuthorId { get; set; } = string.Empty;
    public string? PatientFirstName { get; set; } = string.Empty;
    public string? PatientLastName { get; set; } = string.Empty;
    public string? AuthorFirstName { get; set; } = string.Empty;
    public string? AuthorLastName { get; set; } = string.Empty;
    public string? Department { get; set; } = string.Empty;
    public string? Gender { get; set; } = string.Empty;
    public DateTimeOffset? BirthDate { get; set; }
    // rowcount is temporary should be removed when ingestion architecture is updated
    public string? rowcount { get; set; }
}
