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
            // parse the search results into SupportingContentRecord
            doc.Document.TryGetValue("sourcepage", out var sourcePageValue);
            string? contentValue;
            try
            {
                doc.Document.TryGetValue("content", out var value);
                contentValue = (string)value;
            }
            catch (ArgumentNullException)
            {
                contentValue = null;
            }

            if (sourcePageValue is string sourcePage && contentValue is string content)
            {
                content = content.Replace('\r', ' ').Replace('\n', ' ');
                sb.Add(new SupportingContentRecord(sourcePage, content, "", "", "0"));
            }
        }

        return new ToolContentResponse(sb, [query]);
        
    }

}
