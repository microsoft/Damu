public class FunctionSettings
{
    public FunctionSettings()
    {
        var envVars = Environment.GetEnvironmentVariables();

        SearchKey = envVars["SearchKey"]?.ToString();
        AzureOpenAiKey = envVars["AzureOpenAiKey"]?.ToString();
        DocIntelKey = envVars["DocIntelKey"]?.ToString();

        AzureOpenAiEmbeddingDeployement = string.IsNullOrWhiteSpace(envVars["AzureOpenAiEmbeddingDeployement"]?.ToString()) ? throw new NullReferenceException("AzureOpenAiEmbeddingDeployement") : envVars["AzureOpenAiEmbeddingDeployement"]?.ToString()!;
        AzureOpenAiEmbeddingModel = string.IsNullOrWhiteSpace(envVars["AzureOpenAiEmbeddingModel"]?.ToString()) ? throw new NullReferenceException("AzureOpenAiEmbeddingModel") : envVars["AzureOpenAiEmbeddingModel"]?.ToString()!;

        NoteJsonFileName = string.IsNullOrWhiteSpace(envVars["NoteJsonFileName"]?.ToString()) ? throw new NullReferenceException("NoteJsonFileName") : envVars["NoteJsonFileName"]?.ToString()!;

        var prefix = string.IsNullOrWhiteSpace(envVars["ProjectPrefix"]?.ToString()) ? "damu" : envVars["ProjectPrefix"]?.ToString();

        SearchIndexName = $"{prefix}-index";
        SemanticSearchConfigName = $"{prefix}-semantic-config";
        VectorSearchHnswConfigName = $"{prefix}-hnsw-config";
        VectorSearchProfileName = $"{prefix}-semantic-profile";
        VectorSearchVectorizer = $"{prefix}-search-vectorizer";

#pragma warning disable CS8601 // Possible null reference assignment.
        // VS doesn't understand that the exception means that there will never be a null reference here
        BlobStorageConnStr = string.IsNullOrWhiteSpace(envVars["IncomingBlobConnStr"]?.ToString()) ? throw new NullReferenceException("IncomingBlobConnStr") : envVars["IncomingBlobConnStr"]?.ToString()!;

        var docIntelEndPoint = string.IsNullOrWhiteSpace(envVars["DocIntelEndPoint"]?.ToString()) ? throw new NullReferenceException("DocIntelEndPoint") : envVars["DocIntelEndPoint"]?.ToString()!;

        if (!Uri.TryCreate(docIntelEndPoint, UriKind.Absolute, out DocIntelEndPoint))
            throw new ArgumentException($"DocIntelEndPoint {docIntelEndPoint} is not a valid URI.");

        var openAiEndpoint = string.IsNullOrWhiteSpace(envVars["AzureOpenAiEndpoint"]?.ToString()) ? throw new NullReferenceException("AzureOpenAiEndpoint") : envVars["AzureOpenAiEndpoint"]?.ToString()!;

        if (!Uri.TryCreate(openAiEndpoint, UriKind.Absolute, out AzureOpenAiEndpoint))
            throw new ArgumentException($"AzureOpenAiEndpoint {openAiEndpoint} is not a valid URI.");

        var searchEndpoint = string.IsNullOrWhiteSpace(envVars["SearchEndpoint"]?.ToString()) ? throw new NullReferenceException("SearchEndpoint") : envVars["SearchEndpoint"]?.ToString()!;

        if (!Uri.TryCreate(searchEndpoint, UriKind.Absolute, out SearchEndpoint))
            throw new ArgumentException($"SearchEndpoint {searchEndpoint} is not a valid URI.");
#pragma warning restore CS8601 // Possible null reference assignment.

        ArgumentNullException.ThrowIfNull(envVars["ModelDimensions"]);

        if (int.TryParse(envVars["ModelDimensions"]?.ToString(), out int modelDimensions))
            ModelDimensions = modelDimensions!;
        else
            throw new ArgumentException($"MODEL_DIMENSIONS {modelDimensions} is not a valid integer value.");

        ArgumentNullException.ThrowIfNull(envVars["ModelDimensions"]);

        // https://techcommunity.microsoft.com/t5/ai-azure-ai-services-blog/azure-ai-search-outperforming-vector-search-with-hybrid/ba-p/3929167
        if (decimal.TryParse(envVars["ChunkOverlapPercent"]?.ToString(), out decimal chunkOverlapPercent))
            ChunkOverlapPercent = chunkOverlapPercent!;

        if (int.TryParse(envVars["MaxChunkSize"]?.ToString(), out int maxChunkSize))
            MaxChunkSize = maxChunkSize!;
    }

    public readonly string BlobStorageConnStr;
    public readonly string NoteJsonFileName;

    public readonly Uri AzureOpenAiEndpoint;
    public readonly string AzureOpenAiEmbeddingDeployement;
    public readonly string AzureOpenAiEmbeddingModel;

    public readonly Uri DocIntelEndPoint;

    public int MaxChunkSize = 512;
    public decimal ChunkOverlapPercent = .25m;

    public readonly Uri SearchEndpoint;
    public readonly string SearchIndexName;
    public readonly string SemanticSearchConfigName;
    public readonly int ModelDimensions;
    public readonly string VectorSearchHnswConfigName;
    public readonly string VectorSearchProfileName;
    public readonly string VectorSearchVectorizer;

    public readonly string? SearchKey = null;
    public readonly string? AzureOpenAiKey = null;
    public readonly string? DocIntelKey = null;
}
