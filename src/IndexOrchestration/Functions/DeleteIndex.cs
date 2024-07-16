using Azure.Search.Documents.Indexes;
using IndexOrchestration.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IndexOrchestration.Functions;

public class DeleteIndex
{
    private readonly FunctionSettings _functionSettings;
    private readonly ILogger<DeleteIndex> _logger;
    private readonly SearchIndexClient _searchIndexClient;

    public DeleteIndex(FunctionSettings functionSettings, ILogger<DeleteIndex> logger, SearchIndexClient searchIndexClient)
    {
        _functionSettings = functionSettings;
        _logger = logger;
        _searchIndexClient = searchIndexClient;
    }

    [Function("DeleteIndex")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Deleting index named {name}...", _functionSettings.SearchIndexName);

        var result = await _searchIndexClient.DeleteIndexAsync(_functionSettings.SearchIndexName);

        if (!result.IsError)
        {
            _logger.LogInformation("Index {name} deleted successfully.", _functionSettings.SearchIndexName);

            return new NoContentResult();
        }

        _logger.LogError("Failed to delete index {name}. {reason}", _functionSettings.SearchIndexName, result.ReasonPhrase);

        return new BadRequestObjectResult(result.ReasonPhrase);
    }
}
