﻿public class FunctionSettings
{
    public FunctionSettings()
    {
        var envVars = Environment.GetEnvironmentVariables();

        AzureOpenAiKey = string.IsNullOrWhiteSpace(envVars["AzureOpenAiKey"]?.ToString()) ? throw new NullReferenceException("AzureOpenAiKey") : envVars["AzureOpenAiKey"]?.ToString()!;
        AzureOpenAiEmbeddingDeployement = string.IsNullOrWhiteSpace(envVars["AzureOpenAiEmbeddingDeployement"]?.ToString()) ? throw new NullReferenceException("AzureOpenAiEmbeddingDeployement") : envVars["AzureOpenAiEmbeddingDeployement"]?.ToString()!;
        AzureOpenAiEmbeddingModel = string.IsNullOrWhiteSpace(envVars["AzureOpenAiEmbeddingModel"]?.ToString()) ? throw new NullReferenceException("AzureOpenAiEmbeddingModel") : envVars["AzureOpenAiEmbeddingModel"]?.ToString()!;
        DocIntelApiKey = string.IsNullOrWhiteSpace(envVars["DocIntelApiKey"]?.ToString()) ? throw new NullReferenceException("DocIntelApiKey") : envVars["DocIntelApiKey"]?.ToString()!;
        SearchIndexName = string.IsNullOrWhiteSpace(envVars["SearchIndexName"]?.ToString()) ? throw new NullReferenceException("SearchIndexName") : envVars["SearchIndexName"]?.ToString()!;
        SearchKey = string.IsNullOrWhiteSpace(envVars["SearchKey"]?.ToString()) ? throw new NullReferenceException("SearchKey") : envVars["SearchKey"]?.ToString()!; ;
        SemanticSearchConfigName = string.IsNullOrWhiteSpace(envVars["SemanticSearchConfigName"]?.ToString()) ? throw new NullReferenceException("SemanticSearchConfigName") : envVars["SemanticSearchConfigName"]?.ToString()!;
        VectorSearchHnswConfigName = string.IsNullOrWhiteSpace(envVars["VectorSearchHnswConfigName"]?.ToString()) ? throw new NullReferenceException("VectorSearchHnswConfigName") : envVars["VectorSearchHnswConfigName"]?.ToString()!;
        VectorSearchProfileName = string.IsNullOrWhiteSpace(envVars["VectorSearchProfileName"]?.ToString()) ? throw new NullReferenceException("VectorSearchProfileName") : envVars["VectorSearchProfileName"]?.ToString()!;
        VectorSearchVectorizer = string.IsNullOrWhiteSpace(envVars["VectorSearchVectorizer"]?.ToString()) ? throw new NullReferenceException("VectorSearchVectorizer") : envVars["VectorSearchVectorizer"]?.ToString()!;

        var docIntelEndPoint = string.IsNullOrWhiteSpace(envVars["DocIntelEndPoint"]?.ToString()) ? throw new NullReferenceException("DocIntelEndPoint") : envVars["DocIntelEndPoint"]?.ToString()!; ;

        if (!Uri.TryCreate(docIntelEndPoint, UriKind.Absolute, out DocIntelEndPoint))
            throw new ArgumentException($"DocIntelEndPoint {docIntelEndPoint} is not a valid URI.");

        var openAiEndpoint = string.IsNullOrWhiteSpace(envVars["AzureOpenAiEndpoint"]?.ToString()) ? throw new NullReferenceException("AzureOpenAiEndpoint") : envVars["AzureOpenAiEndpoint"]?.ToString()!; ;

        if (!Uri.TryCreate(openAiEndpoint, UriKind.Absolute, out AzureOpenAiEndpoint))
            throw new ArgumentException($"AzureOpenAiEndpoint {openAiEndpoint} is not a valid URI.");

        var searchEndpoint = string.IsNullOrWhiteSpace(envVars["SearchEndpoint"]?.ToString()) ? throw new NullReferenceException("SearchEndpoint") : envVars["SearchEndpoint"]?.ToString()!; ;

        if (!Uri.TryCreate(searchEndpoint, UriKind.Absolute, out SearchEndpoint))
            throw new ArgumentException($"SearchEndpoint {searchEndpoint} is not a valid URI.");

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

    public readonly Uri AzureOpenAiEndpoint;
    public readonly string AzureOpenAiKey;
    public readonly string AzureOpenAiEmbeddingDeployement;
    public readonly string AzureOpenAiEmbeddingModel;

    public readonly string DocIntelApiKey;
    public readonly Uri DocIntelEndPoint;

    public int MaxChunkSize = 512;
    public decimal ChunkOverlapPercent = .25m;

    public readonly Uri SearchEndpoint;
    public readonly string SearchIndexName;
    public readonly string SearchKey;
    public readonly string SemanticSearchConfigName;
    public readonly int ModelDimensions;
    public readonly string VectorSearchHnswConfigName;
    public readonly string VectorSearchProfileName;
    public readonly string VectorSearchVectorizer;
}