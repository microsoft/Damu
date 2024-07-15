using Api.Models;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
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

        var options = new SearchOptions
        {
            Filter = request.Filter,
            Size = request.KNearestNeighborsCount,
            Select = {
                IndexFields.NoteId,
                IndexFields.NoteChunk,
                IndexFields.NoteChunkOrder,
                IndexFields.CSN,
                IndexFields.MRN,
                IndexFields.NoteType,
                IndexFields.NoteStatus,
                IndexFields.AuthorId,
                IndexFields.AuthorFirstName,
                IndexFields.AuthorLastName,
                IndexFields.Department,
                IndexFields.Gender,
                IndexFields.BirthDate
            }, // "*"
            IncludeTotalCount = true,
            QueryType = SearchQueryType.Full,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = _functionSettings.SemanticSearchConfigName
            },
            VectorSearch = new()
            {
                Queries = {
                new VectorizableTextQuery(text: request.Query)
                    {
                        KNearestNeighborsCount = request.KNearestNeighborsCount,
                        Fields = { IndexFields.NoteChunkVector },
                        Exhaustive = true
                    }
                },
            }
        };

        _logger.LogInformation($"Initiating search for the following query \n{request.Query}.");

        SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(request.Query, options);

        _logger.LogInformation($"SearchAsync found {response.TotalCount} relevant documents.");
        _logger.LogInformation($"Search results reduced to {options.VectorSearch.Queries.FirstOrDefault()?.KNearestNeighborsCount} by KNearestNeighborsCount parameter.");

        var searchResults = new List<SearchResult<SearchDocument>>();

        await foreach (SearchResult<SearchDocument> searchResultDocument in response.GetResultsAsync())
        {
            _logger.LogTrace($"Search results include the chunk in order {searchResultDocument.Document[IndexFields.NoteChunkOrder]} of note " +
                $"with NodeId {searchResultDocument.Document[IndexFields.NoteId]} with a score of {searchResultDocument.Score}.");

            searchResults.Add(searchResultDocument);
        }

        return new OkObjectResult(searchResults);
    }
}
