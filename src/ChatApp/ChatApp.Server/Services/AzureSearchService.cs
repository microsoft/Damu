using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ChatApp.Server.Models;
using System.Text.Json;

namespace ChatApp.Server.Services;

public class AzureSearchService(SearchClient searchClient, IConfiguration config)
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
        if (query is null && embedding is null)
        {
            throw new ArgumentException("Either query or embedding must be provided");
        }

        string semanticConfigurationName = config["AZURE_SEARCH_SEMANTIC_CONFIGURATION_NAME"] ?? "damu-semantic-config";
        bool useSemanticCaptions = false;

        var options = new SearchOptions
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
                SemanticConfigurationName = semanticConfigurationName,
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
        };

        SearchResults<SearchDocument> searchResult = await searchClient.SearchAsync<SearchDocument>(query, options);

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
