using Api.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Api.Functions;

public class UpsertIndex
{
    private readonly FunctionSettings _functionSettings;
    private readonly ILogger<UpsertIndex> _logger;
    private readonly SearchIndexClient _searchIndexClient;

    public UpsertIndex(FunctionSettings functionSettings, ILogger<UpsertIndex> logger, SearchIndexClient searchIndexClient)
    {
        _functionSettings = functionSettings;
        _logger = logger;
        _searchIndexClient = searchIndexClient;
    }

    [Function("UpsertIndex")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request)
    {
        var aoaiParams = string.IsNullOrWhiteSpace(_functionSettings.AzureOpenAiKey) ? new AzureOpenAIParameters
        {
            ResourceUri = _functionSettings.AzureOpenAiEndpoint,
            DeploymentId = _functionSettings.AzureOpenAiEmbeddingDeployment
        } : new AzureOpenAIParameters
        {
            ApiKey = _functionSettings.AzureOpenAiKey,
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
                            },
                            KeywordsFields =
                            {
                                new SemanticField(IndexFields.NoteChunk),
                                new SemanticField(IndexFields.NoteType),
                                new SemanticField(IndexFields.NoteStatus),
                                new SemanticField(IndexFields.AuthorId),
                                new SemanticField(IndexFields.PatientFirstName),
                                new SemanticField(IndexFields.PatientLastName),
                                new SemanticField(IndexFields.AuthorFirstName),
                                new SemanticField(IndexFields.AuthorLastName),
                                new SemanticField(IndexFields.Department),
                                new SemanticField(IndexFields.Gender),
                                new SemanticField(IndexFields.FilePath),
                                new SemanticField(IndexFields.Title),
                                new SemanticField(IndexFields.Url)
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
                new SearchableField(IndexFields.PatientFirstName) { IsFilterable = true, IsSortable = true },
                new SearchableField(IndexFields.PatientLastName) { IsFilterable = true, IsSortable = true },
                new SearchableField(IndexFields.AuthorFirstName) { IsFilterable = true, IsSortable = true },
                new SearchableField(IndexFields.AuthorLastName) { IsFilterable = true, IsSortable = true },
                new SearchableField(IndexFields.Department) { IsFilterable = true, IsSortable = true, IsFacetable = true  },
                new SearchableField(IndexFields.Gender) { IsFilterable = true, IsSortable = true, IsFacetable = true  },
                new SearchField(IndexFields.BirthDate, SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },

                // for citations
                new SearchableField(IndexFields.FilePath) { IsFilterable = true },
                new SearchableField(IndexFields.Title) { IsFilterable = true },
                new SearchableField(IndexFields.Url) { IsFilterable = true }
            }
        };

        var newIndex = await _searchIndexClient.CreateOrUpdateIndexAsync(index);

        if (newIndex.Value == null)
        {
            _logger.LogError("Failed to create index. {reason}", newIndex?.GetRawResponse()?.ReasonPhrase);

            return new BadRequestObjectResult("Failed to create index.");
        }

        _logger.LogInformation("Successfully created index {indexName}.", newIndex.Value.Name);

        return new OkObjectResult(newIndex.Value);
    }
}
