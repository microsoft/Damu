using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Azure.Storage.Blobs.Models;
using Azure.Search.Documents.Models;
using System.Threading;

namespace Api.Functions;

public class SearchAsync
{

    private readonly FunctionSettings _functionSettings;
    private readonly ILogger<SearchAsync> _logger;
    private readonly OpenAIClient _openAIClient;
    private readonly SearchClient _searchClient;                

    public SearchAsync(ILogger<SearchAsync> logger, FunctionSettings functionSettings, OpenAIClient openAiClient, SearchClient searchClient)
    {
        _logger = logger;                        
        _functionSettings = functionSettings;
        _openAIClient = openAiClient;
        _searchClient = searchClient;
              
    }



    [Function(nameof(SearchAsync))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        string query = string.Empty;
        int k = 3;
        string filter = string.Empty;
        

        var searchOptions = new SearchOptions
        {
            Filter = filter,
            Size = k,
            Select = { "title", "chunk_id", "chunk", },
            IncludeTotalCount = true,
            QueryType = SearchQueryType.Full,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = "my-semantic-config",
                QueryCaption = new QueryCaption(QueryCaptionType.Extractive),
                QueryAnswer = new QueryAnswer(QueryAnswerType.Extractive)
            },
            VectorSearch = new()
            {
                Queries = {
                new VectorizableTextQuery(text: query)
                {
                    KNearestNeighborsCount = k,
                    Fields = { "vector" },
                    Exhaustive = true
                }
            },

            }
        };

        SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions);

        await foreach (SearchResult<SearchDocument> result in response.GetResultsAsync())
        {
            Console.WriteLine($"Title: {result.Document["title"]}");
            Console.WriteLine($"Score: {result.Score}\n");
            Console.WriteLine($"Content: {result.Document["chunk"]}");
            if (result.SemanticSearch?.Captions?.Count > 0)
            {
                QueryCaptionResult firstCaption = result.SemanticSearch.Captions[0];
                Console.WriteLine($"First Caption Highlights: {firstCaption.Highlights}");
                Console.WriteLine($"First Caption Text: {firstCaption.Text}");
            }
        }
      
        return new OkObjectResult("Welcome to Azure Functions!");

    }

}

public class QueryRequest
{
    public string? Query { get; set; }
    //public float[]? Embedding { get; set; }
    //public RequestOverrides? Overrides { get; set; }
}