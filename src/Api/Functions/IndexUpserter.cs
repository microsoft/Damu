#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Api.Models;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Text;
using Newtonsoft.Json;
using Tiktoken;

namespace Api.Functions;

public class IndexUpserter
{
    private readonly FunctionSettings _functionSettings;
    private readonly ILogger<IndexUpserter> _logger;
    private readonly OpenAIClient _openAIClient;
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _searchIndexClient;
    private readonly DocumentIntelligenceClient _docIntelClient;

    // todo: move to orchestration function pattern to handle larger throughput
    // todo: add in upsert for individual notes
    public IndexUpserter(DocumentIntelligenceClient docIntelClient, FunctionSettings functionSettings, ILogger<IndexUpserter> logger, OpenAIClient openAiClient, SearchClient searchClient, SearchIndexClient searchIndexClient)
    {
        _docIntelClient = docIntelClient;
        _functionSettings = functionSettings;
        _logger = logger;
        _openAIClient = openAiClient;
        _searchClient = searchClient;
        _searchIndexClient = searchIndexClient;
    }

    [Function(nameof(IndexUpserter))]
    public async Task RunAsync([BlobTrigger("notes/{name}", Connection = "IncomingBlobConnStr")] string content)
    {
        // clean out BOM if present
        var cleanedContent = content.Trim().Replace("\uFEFF", "");

        if (!string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Deleting index for non-production environment to ensure consistent index definition during development...");

            await _searchIndexClient.DeleteIndexAsync(_functionSettings.SearchIndexName);
        }

        _logger.LogInformation("Creating/updating index...");

        await CreateOrUpdateIndexAsync();

        var jsonReader = new JsonTextReader(new StringReader(cleanedContent))
        {
            SupportMultipleContent = true
        };

        var jsonSerializer = new JsonSerializer();

        List<SourceNoteRecord> inputDocuments = [];

        while (jsonReader.Read())
        {
            SourceNoteRecord foo = jsonSerializer.Deserialize<SourceNoteRecord>(jsonReader) ?? new();
            inputDocuments.Add(foo);
        }

        _logger.LogInformation("Parsed out {count} note records to analyze for loading.", inputDocuments.Count);

        await LoadIndexAsync(inputDocuments);

        _logger.LogInformation("Index loading completed.");
    }

    private async Task<Response<SearchIndex>?> CreateOrUpdateIndexAsync()
    {
        var aoaiParams = string.IsNullOrWhiteSpace(_functionSettings.AzureOpenAiKey) ? 
            new AzureOpenAIParameters
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

        return await _searchIndexClient.CreateOrUpdateIndexAsync(index);
    }

    private async Task LoadIndexAsync(List<SourceNoteRecord> inputDocuments)
    {
        List<SearchDocument> sampleDocuments = [];

        foreach (var document in inputDocuments)
        {
            List<SearchDocument> newSearchDocs = [];

            var content = !string.IsNullOrWhiteSpace(document.NoteInHtml) ? document.NoteInHtml : string.Empty;

            var tokenCount = GetTokenCount(content);

            if (tokenCount > _functionSettings.MaxChunkSize)
            {
                _logger.LogDebug("Note {noteId} is too large to index in one chunk. Splitting...", document.NoteId);
                newSearchDocs = await RecursivelySplitNoteContent(document);
                _logger.LogDebug("Note {noteId} chunking produced {count} chunks.", document.NoteId, newSearchDocs.Count);
            }
            else
            {
                _logger.LogDebug("Note {noteId} is small enough to index in one chunk.", document.NoteId);
                document.NoteChunk = content;
                document.NoteChunkOrder = 0;
                newSearchDocs.Add(await ConvertToSearchDocumentAsync(document));
            }

            sampleDocuments.AddRange(newSearchDocs);
        }

        Response<IndexDocumentsResult>? indexingResult = null;

        try
        {
            var opts = new IndexDocumentsOptions
            {
                ThrowOnAnyError = true
            };

            _logger.LogInformation("Loading {count} index records to index...", sampleDocuments.Count);

            indexingResult = await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(sampleDocuments), opts);
        }
        catch (AggregateException aggregateException)
        {
            _logger.LogError("Partial failures detected. Some documents failed to index.");
            foreach (var exception in aggregateException.InnerExceptions)
            {
                _logger.LogError("{exception}", exception.Message);
            }

            _logger.LogInformation("Encountered {count} exceptions trying to load the index.", aggregateException.InnerExceptions.Count);

            if (indexingResult?.Value.Results.Count > 0)
            {
                _logger.LogInformation("Successfully indexed {count} documents.", indexingResult.Value.Results.Count);
            }
        }
    }

    private async Task<List<SearchDocument>> RecursivelySplitNoteContent(SourceNoteRecord sourceNote)
    {
        if (string.IsNullOrWhiteSpace(sourceNote.NoteInHtml))
            return [];

        var analyzeRequest = new AnalyzeDocumentContent
        {
            Base64Source = BinaryData.FromString(sourceNote.NoteInHtml)
        };

        var analysis = await _docIntelClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", analyzeRequest, outputContentFormat: ContentFormat.Markdown);
        var chunkerOverlapTokens = Convert.ToInt32(_functionSettings.ChunkOverlapPercent * _functionSettings.MaxChunkSize);
        var lines = TextChunker.SplitMarkDownLines(analysis.Value.Content, _functionSettings.MaxChunkSize, tokenCounter: GetTokenCount);
        var paragraphs = TextChunker.SplitMarkdownParagraphs(lines, _functionSettings.MaxChunkSize, chunkerOverlapTokens, tokenCounter: GetTokenCount);

        var results = new List<SearchDocument>();

        foreach (var (p, i) in paragraphs.Select((p, i) => (p, i)))
        {
            var searchDoc = await ConvertToSearchDocumentAsync(new SourceNoteRecord(sourceNote), p, i);

            results.Add(searchDoc);
        }

        return results;
    }

    private async Task<SearchDocument> ConvertToSearchDocumentAsync(SourceNoteRecord sourceNote, string? chunk = null, int? chunkOrder = null)
    {
        if (!string.IsNullOrWhiteSpace(chunk))
            sourceNote.NoteChunk = chunk;

        if (chunkOrder != null)
            sourceNote.NoteChunkOrder = chunkOrder;

        var dictionary = sourceNote.ToDictionary();

        if (!string.IsNullOrWhiteSpace(sourceNote.NoteChunk))
        {
            _logger.LogDebug("Generating embeddings for note {noteId} chunk {noteChunkOrder}", sourceNote.NoteId, sourceNote.NoteChunkOrder);

            dictionary[IndexFields.NoteChunkVector] = await GenerateEmbeddingAsync(sourceNote.NoteChunk);
        }

        var document = new SearchDocument(dictionary);

        return document;
    }
    private async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
    {
        _logger.LogTrace("Generating embedding for text: {text}", text);

        var response = await _openAIClient.GetEmbeddingsAsync(new EmbeddingsOptions(_functionSettings.AzureOpenAiEmbeddingDeployment, [text]));

        return response.Value.Data[0].Embedding;
    }

    private int GetTokenCount(string text)
    {
        var encoder = ModelToEncoder.For(_functionSettings.AzureOpenAiEmbeddingModel);

        return encoder.CountTokens(text);
    }
}
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
