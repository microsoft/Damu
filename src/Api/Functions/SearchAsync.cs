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

public class QueryRequest
{
    public string? Query { get; set; }
    //public float[]? Embedding { get; set; }
    //public RequestOverrides? Overrides { get; set; }
}

/** pseudo code from Python
* @bp.route("/history/generate", methods=["POST"])
async def add_conversation():
    authenticated_user = get_authenticated_user_details(request_headers=request.headers)
    user_id = authenticated_user["user_principal_id"]

    ## check request for conversation_id
    request_json = await request.get_json()
    conversation_id = request_json.get("conversation_id", None)

    try:
        # make sure cosmos is configured
        cosmos_conversation_client = init_cosmosdb_client()
        if not cosmos_conversation_client:
            raise Exception("CosmosDB is not configured or not working")

        # check for the conversation_id, if the conversation is not set, we will create a new one
        history_metadata = {}
        if not conversation_id:
            title = await generate_title(request_json["messages"])
            conversation_dict = await cosmos_conversation_client.create_conversation(
                user_id=user_id, title=title
            )
            conversation_id = conversation_dict["id"]
            history_metadata["title"] = title
            history_metadata["date"] = conversation_dict["createdAt"]

        ## Format the incoming message object in the "chat/completions" messages format
        ## then write it to the conversation history in cosmos
        messages = request_json["messages"]
        if len(messages) > 0 and messages[-1]["role"] == "user":
            createdMessageValue = await cosmos_conversation_client.create_message(
                uuid=str(uuid.uuid4()),
                conversation_id=conversation_id,
                user_id=user_id,
                input_message=messages[-1],
            )
            if createdMessageValue == "Conversation not found":
                raise Exception(
                    "Conversation not found for the given conversation ID: "
                    + conversation_id
                    + "."
                )
        else:
            raise Exception("No user message found")

        await cosmos_conversation_client.cosmosdb_client.close()

        # Submit request to Chat Completions for response
        request_body = await request.get_json()
        history_metadata["conversation_id"] = conversation_id
        request_body["history_metadata"] = history_metadata
        return await conversation_internal(request_body, request.headers)

    except Exception as e:
        logging.exception("Exception in /history/generate")
        return jsonify({"error": str(e)}), 500
*/
