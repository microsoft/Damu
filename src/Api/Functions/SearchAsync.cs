using Api.Models;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Api.Functions;

public partial class SearchAsync
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
        var request = (await req.ReadFromJsonAsync<QueryRequest>()) ?? new();

        if (string.IsNullOrWhiteSpace(request.Query))
            return new BadRequestObjectResult("Query is required");

        var options = CreateQuery(request);

        SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(request.Query, options);

        await foreach (SearchResult<SearchDocument> searchResultDocument in response.GetResultsAsync())
        {
            Console.WriteLine($"Title: {searchResultDocument.Document["title"]}");
            Console.WriteLine($"Score: {searchResultDocument.Score}\n");
            Console.WriteLine($"Content: {searchResultDocument.Document["chunk"]}");
            if (searchResultDocument.SemanticSearch?.Captions?.Count > 0)
            {
                QueryCaptionResult firstCaption = searchResultDocument.SemanticSearch.Captions[0];
                Console.WriteLine($"First Caption Highlights: {firstCaption.Highlights}");
                Console.WriteLine($"First Caption Text: {firstCaption.Text}");
            }
        }

        // streaming response
        // tool message with citations first

        var result = new ApiSearchResult
        {

        };

        return new OkObjectResult(result);
    }

    private SearchOptions? CreateQuery(QueryRequest request)
    {
        return new SearchOptions
        {
            Filter = request.Filter,
            Size = request.KNearestNeighborsCount,
            Select = { "title", "chunk_id", "chunk", },
            IncludeTotalCount = true,
            QueryType = SearchQueryType.Full,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = _functionSettings.SemanticSearchConfigName,
                QueryCaption = new QueryCaption(QueryCaptionType.Extractive),
                QueryAnswer = new QueryAnswer(QueryAnswerType.Extractive)
            },
            VectorSearch = new()
            {
                Queries = {
                new VectorizableTextQuery(text: request.Query)
                    {
                        KNearestNeighborsCount = request.KNearestNeighborsCount,
                        Fields = { "vector" },
                        Exhaustive = true
                    }
                },

            }
        };
    }
}
