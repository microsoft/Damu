using Api.Models;
using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Api.Functions;

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

    [Function(nameof(DeleteIndex))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        _logger.LogInformation("Deleting index for non-production environment to ensure consistent index definition during development...");

        var result = await _searchIndexClient.DeleteIndexAsync(_functionSettings.SearchIndexName);

        if(result.IsError)
        {
            _logger.LogError("Failed to delete index. {reason}", result.ReasonPhrase);

            return new BadRequestObjectResult("Failed to delete index.");
        }

        _logger.LogInformation("Successfully deleted index {indexName}.", _functionSettings.SearchIndexName);

        return new NoContentResult();
    }
}
