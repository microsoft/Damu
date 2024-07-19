using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ChatApp.Server.Models;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ChatApp.Server.Services;

public class AzureSearchService(SearchClient searchClient, IOptions<AISearchOptions> options)
{
    private string[] _indexContentFields = [
        "CSN",
        "NoteType",
        "NoteStatus",
        "AuthorId",
        "AuthorFirstName",
        "AuthorLastName",
        "Department",
        "Gender",
        "BirthDate",
        "NoteId",
        "PatientFirstName",
        "PatientLastName",
        "MRN"
        ];

    public async Task<ToolContentResponse> QueryDocumentsAsync(
        string? query = null,
        float[]? embedding = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options?.Value?.SemanticConfigurationName);

        if (query is null && embedding is null)
            throw new ArgumentException("Either query or embedding must be provided");

        var response = await searchClient.SearchAsync<SearchDocument>(query, new SearchOptions
        {
            Filter = "",
            Size = 5,
            Select = {
               "*"
            },
            IncludeTotalCount = true,
            QueryType = SearchQueryType.Full,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = options?.Value.SemanticConfigurationName,
                QueryCaption = new(QueryCaptionType.None)
            },
            VectorSearch = new()
            {
                Queries = {
                new VectorizableTextQuery(text: query)
                {
                        KNearestNeighborsCount = 5,
                        Fields = { "NoteChunkVector"},
                        Exhaustive = true
                    }
                },
            }
        });

        if (response.Value == null)
            throw new Exception(response.GetRawResponse().ReasonPhrase);

        var searchResult = response.Value;

        var sb = new List<SupportingContentRecord>();
        await foreach (var doc in searchResult.GetResultsAsync())
        {
            // "FilePath": "notes/1000054825.json",
            // "Title": "1000054825.json",
            // "Url": "notes/1000054825.json"
            doc.Document.TryGetValue("NoteChunkOrder", out var chunkOrderValue);

            doc.Document.TryGetValue("Title", out var titleValue);
            doc.Document.TryGetValue("FilePath", out var filePathValue);
            doc.Document.TryGetValue("Url", out var urlValue);

            // parse the search results into SupportingContentRecord
            string? contentValue;
            try
            {
                doc.Document.TryGetValue("NoteChunk", out var value);
                contentValue = (string)value;
            }
            catch (ArgumentNullException)
            {
                contentValue = null;
            }

            // put all other sortable and retrievable fields into 'additionalcontent'
            var indexProps = doc.Document.ToDictionary().Where(d => _indexContentFields.Contains(d.Key)).ToDictionary();

            if (titleValue is string title && 
                filePathValue is string filePath &&
                urlValue is string url &&
                chunkOrderValue is int chunkOrder &&
                contentValue is string content)
            {
                content = content.Replace('\r', ' ').Replace('\n', ' ');
                sb.Add(new SupportingContentRecord(title, content, url, filePath, chunkOrder.ToString(), JsonSerializer.Serialize(indexProps)));
            }
        }

        return new ToolContentResponse(sb, [query]);

    }

}
