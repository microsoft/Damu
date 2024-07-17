namespace Api;

internal class SourceNoteRecord
{
    public SourceNoteRecord() { }
    public SourceNoteRecord(SourceNoteRecord original, string? chunk = null, int? chunkIndex = null)
    {
        NoteId = original.NoteId;
        NoteContent = original.NoteContent;
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
        FilePath = original.FilePath;
        Title = original.Title;
        Url = original.Url;
        // is there an easier way to do a deep copy but ignore some properties?
    }
    public string? IndexRecordId { get; set; } = string.Empty;
    public long? NoteId { get; set; }
    public string? NoteContent { get; set; } = string.Empty;
    public string? NoteChunk { get; set; } = string.Empty;
    public int? NoteChunkOrder { get; set; }
    public List<float> NoteChunkVector { get; set; } = [];
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

    public string? FilePath { get; set; } = string.Empty;
    public string? Title { get; set; } = string.Empty;
    public string? Url { get; set; } = string.Empty;

    public Dictionary<string, object?> ToDictionaryForIndexing()
    {
        var result = new Dictionary<string, object?>();

        foreach (var key in this.GetType().GetProperties())
        {
            if (string.Equals(key.Name, nameof(NoteContent), StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(key.Name, key.GetValue(this));
        }

        return result;
    }
}