using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Api;

public class Function1
{
    // todo: move to configuration
    private readonly string SEARCH_ENDPOINT;
    private readonly string SEARCH_KEY;
    private readonly string SEARCH_INDEX_NAME;
    private readonly string SEMANTIC_SEARCH_CONFIG_NAME;
    private readonly int MODEL_DIMENSIONS;
    private readonly string VECTOR_SEARCH_PROFILE_NAME;
    private readonly string VECTOR_SEARCH_HNSW_CONFIG_NAME;

    private readonly string VECTOR_SEARCH_VECTORIZER = "myOpenAIVectorizer";
    private readonly string AZURE_OPENAI_ENDPOINT = "myOpenAIVectorizer";
    private readonly string AZURE_OPENAI_APIKEY = "myOpenAIVectorizer";
    private readonly string AZURE_OPENAI_EMBEDDING_DEPLOYED_MODEL = "myOpenAIVectorizer";

    private readonly ILogger<Function1> _logger;

    public Function1(ILogger<Function1> logger, IConfiguration configuration)
    {
        _logger = logger;
        ArgumentNullException.ThrowIfNull(configuration["MODEL_DIMENSIONS"]);

        SEARCH_KEY = string.IsNullOrWhiteSpace(configuration["SEARCH_KEY"]) ? throw new NullReferenceException("SEARCH_KEY") : configuration["SEARCH_KEY"]!; ;
        SEARCH_INDEX_NAME = string.IsNullOrWhiteSpace(configuration["SEARCH_INDEX_NAME"]) ? throw new NullReferenceException("SEARCH_INDEX_NAME") : configuration["SEARCH_INDEX_NAME"]!;
        SEMANTIC_SEARCH_CONFIG_NAME = string.IsNullOrWhiteSpace(configuration["SEMANTIC_SEARCH_CONFIG_NAME"]) ? throw new NullReferenceException("SEMANTIC_SEARCH_CONFIG_NAME") : configuration["SEMANTIC_SEARCH_CONFIG_NAME"]!;
        SEMANTIC_SEARCH_CONFIG_NAME = string.IsNullOrWhiteSpace(configuration["VECTOR_SEARCH_PROFILE_NAME"]) ? throw new NullReferenceException("VECTOR_SEARCH_PROFILE_NAME") : configuration["VECTOR_SEARCH_PROFILE_NAME"]!;
        VECTOR_SEARCH_HNSW_CONFIG_NAME = string.IsNullOrWhiteSpace(configuration["VECTOR_SEARCH_HNSW_CONFIG_NAME"]) ? throw new NullReferenceException("VECTOR_SEARCH_HNSW_CONFIG_NAME") : configuration["VECTOR_SEARCH_HNSW_CONFIG_NAME"]!;
        VECTOR_SEARCH_HNSW_CONFIG_NAME = configuration["VECTOR_SEARCH_HNSW_CONFIG_NAME"]!;

        if (int.TryParse(configuration["MODEL_DIMENSIONS"], out int modelDimensions))
            MODEL_DIMENSIONS = modelDimensions!;
        else
            throw new ArgumentException($"MODEL_DIMENSIONS {modelDimensions} is not a valid integer value.");
    }

    [Function(nameof(Function1))]
    public async Task Run([BlobTrigger("notes/{name}", Connection = "IncomingBlobConnStr")] Stream stream, string name)
    {
        using var blobStreamReader = new StreamReader(stream);
        var content = await blobStreamReader.ReadToEndAsync();
        _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}");

        Uri searchEndpointUri = new(SEARCH_ENDPOINT);

        SearchClient client = new(
            searchEndpointUri,
            SEARCH_INDEX_NAME,
            new AzureKeyCredential(SEARCH_KEY));

        SearchIndexClient clientIndex = new(
            searchEndpointUri,
            new AzureKeyCredential(SEARCH_KEY));

        await CreateIndexAsync(clientIndex);
    }

    private async Task CreateIndexAsync(SearchIndexClient clientIndex)
    {

        Console.WriteLine("Creating (or updating) search index");
        SearchIndex index = new(SEARCH_INDEX_NAME)
        {
            VectorSearch = new()
            {
                Profiles =
                {
                    new VectorSearchProfile(VECTOR_SEARCH_PROFILE_NAME, VECTOR_SEARCH_HNSW_CONFIG_NAME)
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(VECTOR_SEARCH_HNSW_CONFIG_NAME)
                    // Hnsw vs exhaustive knn algorithms
                },
                Vectorizers =
                    {
                        new AzureOpenAIVectorizer(VECTOR_SEARCH_VECTORIZER)
                        {
                            AzureOpenAIParameters = new AzureOpenAIParameters()
                            {
                                ResourceUri = new Uri(AZURE_OPENAI_ENDPOINT),
                                ApiKey = AZURE_OPENAI_APIKEY, // todo: managed identity
                                DeploymentId = AZURE_OPENAI_EMBEDDING_DEPLOYED_MODEL,
                            }
                        }
                    }
            },
            SemanticSearch = new SemanticSearch()
            {
                Configurations =
                {
                    new SemanticConfiguration(
                        SEMANTIC_SEARCH_CONFIG_NAME,
                        new SemanticPrioritizedFields()
                        {
                            //TitleField = new SemanticField("title"),
                            ContentFields =
                            {
                                new SemanticField("NoteChunk")
                            },
                            //KeywordsFields =
                            //{
                            //    new SemanticField("NoteType")
                            //}
                        })
                },
            },
            Fields =
            {
                // required for index structure
                new SearchableField("IndexRecordId") { IsKey = true },
                new SearchField("NoteId", SearchFieldDataType.Int64) { IsFilterable = true, IsSortable = true }, 

                // index main content
                new SearchableField("NoteChunk") { IsFilterable = true, IsSortable = true },
                new SearchField("NoteChunkOrder", SearchFieldDataType.Int64) { IsFilterable = true, IsSortable = true },
                new SearchField("NoteChunkVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = MODEL_DIMENSIONS,
                    VectorSearchProfileName = VECTOR_SEARCH_PROFILE_NAME
                },

                // good to have fields for contstructing with fhir query results
                new SearchField("CSN", SearchFieldDataType.Int64) { IsFilterable = true, IsSortable = true },
                new SearchField("MRN", SearchFieldDataType.Int64) { IsFilterable = true, IsSortable = true }, 
                
                // nice to have fields for filtering and faceting
                new SearchableField("NoteType") { IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchableField("NoteStatus") { IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchableField("AuthorId") { IsFilterable = true, IsSortable = true, IsFacetable = true },
                new SearchableField("AuthorFirstName") { IsFilterable = true, IsSortable = true },
                new SearchableField("AuthorLastName") { IsFilterable = true, IsSortable = true },
                new SearchableField("Department") { IsFilterable = true, IsSortable = true, IsFacetable = true  },
                new SearchableField("Gender") { IsFilterable = true, IsSortable = true, IsFacetable = true  },
                new SearchField("BirthDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
            }
        };
        var result = await clientIndex.CreateOrUpdateIndexAsync(index);

        Console.WriteLine(result);
    }
}
