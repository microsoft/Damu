using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using IndexOrchestration.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IndexOrchestration.Functions;

public class CreateIndex
{
    private readonly FunctionSettings _functionSettings;
    private readonly ILogger<CreateIndex> _logger;
    private readonly SearchIndexClient _searchIndexClient;

    public CreateIndex(FunctionSettings functionSettings, ILogger<CreateIndex> logger, SearchIndexClient searchIndexClient)
    {
        _functionSettings = functionSettings;
        _logger = logger;
        _searchIndexClient = searchIndexClient;
    }

    [Function("CreateIndex")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("Creating/updating index...");

        var aoaiParams = string.IsNullOrWhiteSpace(_functionSettings.AzureOpenAiKey) ? new AzureOpenAIParameters
        {
            ResourceUri = _functionSettings.AzureOpenAiEndpoint,
            DeploymentId = _functionSettings.AzureOpenAiEmbeddingDeployment
        } : new AzureOpenAIParameters
        {
            ResourceUri = _functionSettings.AzureOpenAiEndpoint,
            DeploymentId = _functionSettings.AzureOpenAiEmbeddingDeployment
        };

        SearchIndex index = new(_functionSettings.SearchIndexName)
        {
            VectorSearch = new()
            {
                Profiles =
                {
                    new VectorSearchProfile(_functionSettings.VectorSearchProfileName, _functionSettings.VectorSearchHnswConfigName)
                    {
                        Vectorizer = _functionSettings.VectorSearchVectorizer
                    }
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(_functionSettings.VectorSearchHnswConfigName)
                },
                Vectorizers =
                    {
                        new AzureOpenAIVectorizer(_functionSettings.VectorSearchVectorizer)
                        {
                            AzureOpenAIParameters = aoaiParams
                        }
                    }
            },
            SemanticSearch = new SemanticSearch()
            {
                Configurations =
                {
                    new SemanticConfiguration(
                        _functionSettings.SemanticSearchConfigName,
                        new SemanticPrioritizedFields()
                        {
                            ContentFields =
                            {
                                new SemanticField(IndexFields.NoteChunk)
                            }
                        })
                }
            },
            Fields =
            {
                // required for index structure
                new SearchableField(IndexFields.IndexRecordId) { IsKey = true },
                new SearchField(IndexFields.NoteId, SearchFieldDataType.Int64) { IsFilterable = true, IsSortable = true }, 

                // index main content
                new SearchableField(IndexFields.NoteChunk) { IsFilterable = true, IsSortable = true },
                new SearchField(IndexFields.NoteChunkOrder, SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SearchField(IndexFields.NoteChunkVector, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _functionSettings.ModelDimensions,
                    VectorSearchProfileName = _functionSettings.VectorSearchProfileName
                },

                // good to have fields for contstructing with fhir query results
                new SearchField(IndexFields.CSN, SearchFieldDataType.Int64) { IsFilterable = true, IsSortable = true },
                new SearchField(IndexFields.MRN, SearchFieldDataType.Int64) { IsFilterable = true, IsSortable = true }, 
                
                // nice to have fields for filtering and faceting
                new SearchableField(IndexFields.NoteType) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchableField(IndexFields.NoteStatus) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchableField(IndexFields.AuthorId) { IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchableField(IndexFields.AuthorFirstName) { IsFilterable = true, IsSortable = true },
                new SearchableField(IndexFields.AuthorLastName) { IsFilterable = true, IsSortable = true },
                new SearchableField(IndexFields.Department) { IsFilterable = true, IsSortable = true, IsFacetable = true  },
                new SearchableField(IndexFields.Gender) { IsFilterable = true, IsSortable = true, IsFacetable = true  },
                new SearchField(IndexFields.BirthDate, SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true }
            }
        };

        var createIndexResult = await _searchIndexClient.CreateOrUpdateIndexAsync(index);

        if (createIndexResult.Value == null)
        {
            _logger.LogError("Failed to create index: {0}", createIndexResult.GetRawResponse().ReasonPhrase);

            return new BadRequestObjectResult("Failed to create index.");
        }
        else
        {
            _logger.LogInformation("Index created successfully.");

            // consider created result
            return new OkObjectResult(createIndexResult.Value);
        }
    }
}
