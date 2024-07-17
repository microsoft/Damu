using System.ComponentModel;
using Azure.Core;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ChatApp.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace ChatApp.Server.Services;

public class AzureSearchService(SearchClient searchClient)
{   
    public async Task<ToolContentResponse> QueryDocumentsAsync(
        string? query = null,
        float[]? embedding = null)
    {
        if (query is null && embedding is null)
        {
            throw new ArgumentException("Either query or embedding must be provided");
        }

        string semanticConfigurationName = "damu-semantic-config";
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
            doc.Document.TryGetValue("PatientName", out var patientNameValue);
            doc.Document.TryGetValue("MRN", out var mrnValue);

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

            if (titleValue is string title && 
                filePathValue is string filePath &&
                urlValue is string url &&
                chunkOrderValue is int chunkOrder &&
                patientNameValue is string patientName &&
                mrnValue is string mrn &&
                contentValue is string content)
            {
                content = content.Replace('\r', ' ').Replace('\n', ' ');
                sb.Add(new SupportingContentRecord(title, content, url, filePath, chunkOrder.ToString(), patientName, mrn));
            }
        }

        return new ToolContentResponse(sb, [query]);
        
    }

}
