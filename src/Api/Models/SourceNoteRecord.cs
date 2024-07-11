namespace Api.Models;

internal class SourceNoteRecord
{
    public SourceNoteRecord() { }
    public SourceNoteRecord(SourceNoteRecord original, string? chunk = null, int? chunkIndex = null)
    {
        NoteId = original.NoteId;
        NoteContent = original.NoteContent;
        NoteInHtml = original.NoteInHtml;
        NoteInText = original.NoteInText;
        NoteChunk = string.IsNullOrWhiteSpace(chunk) ? original.NoteChunk : chunk;
        NoteChunkOrder = chunkIndex == null ? original.NoteChunkOrder : chunkIndex;
        NoteChunkVector = original.NoteChunkVector;
        CSN = original.CSN;
        MRN = original.MRN;
        NoteType = original.NoteType;
        NoteStatus = original.NoteStatus;
        AuthorId = original.AuthorId;
        AuthorFirstName = original.AuthorFirstName;
        AuthorLastName = original.AuthorLastName;
        Department = original.Department;
    }
    public Guid? IndexRecordId { get; set; } = Guid.NewGuid();
    public long? NoteId { get; set; }
    public string? NoteContent { get; set; } = string.Empty;
    public string? NoteInHtml { get; set; } = string.Empty;
    public string? NoteInText { get; set; } = string.Empty;
    public string? NoteChunk { get; set; } = string.Empty;
    public int? NoteChunkOrder { get; set; }
    public List<float> NoteChunkVector { get; set; } = []; // ?
    public long? CSN { get; set; }
    public long? MRN { get; set; }
    public string? NoteType { get; set; } = string.Empty;
    public string? NoteStatus { get; set; } = string.Empty;
    public string? AuthorId { get; set; } = string.Empty;
    public string? AuthorFirstName { get; set; } = string.Empty;
    public string? AuthorLastName { get; set; } = string.Empty;
    public string? Department { get; set; } = string.Empty;
    public string? Gender { get; set; } = string.Empty;
    public DateTimeOffset? BirthDate { get; set; }

    public Dictionary<string, object?> ToDictionary()
    {
        var result = new Dictionary<string, object?>();

        foreach (var key in GetType().GetProperties())
        {
            if (key.Name == "NoteContent" || key.Name == "NoteInHtml" || key.Name == "NoteInText")
                continue;

            result.Add(key.Name, key.GetValue(this));
        }

        return result;
    }
}