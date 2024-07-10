namespace Api.Functions;

public class QueryResult
{
    public string IndexRecordId { get; set; }
    public int NoteId { get; set; }
    public string NoteChunk { get; set; }
    public string NoteChunkOrder { get; set; }
    public int CSN { get; set; }
    public int MRN { get; set; }
    public string NoteType { get; set; }
    public string NoteStatus { get; set; }
    public string AuthorId { get; set; }
    public string AuthorFirstName { get; set; }
    public string AuthorLastName { get; set; }
    public string Department { get; set; }
    public string Gender { get; set; }
    public DateTime BirthDate { get; set; }
}

