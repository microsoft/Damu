using Azure.Search.Documents.Models;

namespace ChatApp.Server.Services;

public class MinimalSearchResult
{
    public MinimalSearchResult() { }
    public MinimalSearchResult(double relevancyScore, SearchDocument fullSearchResult)
    {
        Score = relevancyScore;

        fullSearchResult.TryGetValue(nameof(IndexRecordId), out var indexRecordIdObj);
        if (indexRecordIdObj is string indexRecordId)
            IndexRecordId = indexRecordId;
        fullSearchResult.TryGetValue(nameof(NoteId), out var noteIdObj);
        if (noteIdObj is long noteId)
            NoteId = noteId;
        fullSearchResult.TryGetValue(nameof(CSN), out var csnObj);
        if (csnObj is long csn)
            CSN = csn;
        fullSearchResult.TryGetValue(nameof(MRN), out var mrnObj);
        if (mrnObj is long mrn)
            MRN = mrn;
        fullSearchResult.TryGetValue(nameof(NoteType), out var noteTypeObj);
        if (noteTypeObj is string noteType)
            NoteType = noteType;
        fullSearchResult.TryGetValue(nameof(NoteStatus), out var noteStatusObj);
        if (noteStatusObj is string noteStatus)
            NoteStatus = noteStatus;
        fullSearchResult.TryGetValue(nameof(AuthorId), out var authorIdObj);
        if (authorIdObj is string authorId)
            AuthorId = authorId;
        fullSearchResult.TryGetValue(nameof(PatientFirstName), out var patientFirstNameObj);
        if (patientFirstNameObj is string patientFirstName)
            PatientFirstName = patientFirstName;
        fullSearchResult.TryGetValue(nameof(PatientLastName), out var patientLastNameObj);
        if (patientLastNameObj is string patientLastName)
            PatientLastName = patientLastName;
        fullSearchResult.TryGetValue(nameof(AuthorFirstName), out var authorFirstNameObj);
        if (authorFirstNameObj is string authorFirstName)
            AuthorFirstName = authorFirstName;
        fullSearchResult.TryGetValue(nameof(AuthorLastName), out var authorLastNameObj);
        if (authorLastNameObj is string authorLastName)
            AuthorLastName = authorLastName;
        fullSearchResult.TryGetValue(nameof(Department), out var departmentObj);
        if (departmentObj is string department)
            Department = department;
        fullSearchResult.TryGetValue(nameof(Gender), out var genderObj);
        if (genderObj is string gender)
            Gender = gender;
        fullSearchResult.TryGetValue(nameof(BirthDate), out var birthDateObj);
        if (birthDateObj is DateTimeOffset birthDate)
            BirthDate = birthDate;
    }

    public double Score { get; set; }
    public string IndexRecordId { get; set; } = string.Empty;
    public long? NoteId { get; set; }
    public long? CSN { get; set; }
    public long? MRN { get; set; }
    public string? NoteType { get; set; }
    public string? NoteStatus { get; set; }
    public string? AuthorId { get; set; }
    public string? PatientFirstName { get; set; }
    public string? PatientLastName { get; set; }
    public string? AuthorFirstName { get; set; }
    public string? AuthorLastName { get; set; }
    public string? Department { get; set; }
    public string? Gender { get; set; }
    public DateTimeOffset? BirthDate { get; set; }
};
