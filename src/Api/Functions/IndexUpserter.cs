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
    public async Task RunAsync([BlobTrigger("notes/{name}", Connection = "IncomingBlobConnStr")] Stream stream, string name)
    {
        using var blobStreamReader = new StreamReader(stream);

        var content = await blobStreamReader.ReadToEndAsync();

        _logger.LogInformation("Creating/updating index...");

        await CreateOrUpdateIndexAsync();

        var jsonReader = new JsonTextReader(new StringReader(content))
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

        _logger.LogInformation($"Parsed out {inputDocuments.Count} note records to analyze for loading.");

        await LoadIndexAsync(inputDocuments);
    }

    private async Task<Response<SearchIndex>?> CreateOrUpdateIndexAsync()
    {
        SearchIndex index = new(_functionSettings.SearchIndexName)
        {
            VectorSearch = new()
            {
                Profiles =
                {
                    new VectorSearchProfile(_functionSettings.VectorSearchProfileName, _functionSettings.VectorSearchHnswConfigName)
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(_functionSettings.VectorSearchHnswConfigName)
                },
                Vectorizers =
                    {
                        new AzureOpenAIVectorizer(_functionSettings.VectorSearchVectorizer)
                        {
                            AzureOpenAIParameters = new AzureOpenAIParameters()
                            {
                                ResourceUri = _functionSettings.AzureOpenAiEndpoint,
                                DeploymentId = _functionSettings.AzureOpenAiEmbeddingDeployement,
                            }
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
                                new SemanticField("NoteChunk")
                            }
                        })
                }
            },
            Fields =
            {
                // required for index structure
                new SearchableField("IndexRecordId") { IsKey = true },
                new SearchField("NoteId", SearchFieldDataType.Int64) { IsFilterable = true, IsSortable = true }, 

                // index main content
                new SearchableField("NoteChunk") { IsFilterable = true, IsSortable = true },
                new SearchField("NoteChunkOrder", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SearchField("NoteChunkVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _functionSettings.ModelDimensions,
                    VectorSearchProfileName = _functionSettings.VectorSearchProfileName
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
                new SearchField("BirthDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true }
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
                newSearchDocs = await RecursivelySplitNoteContent(document);
            }
            else
            {
                document.NoteChunk = content;
                document.NoteChunkOrder = 0;
                newSearchDocs.Add(await ConvertToSearchDocumentAsync(document));
            }

            sampleDocuments.AddRange(newSearchDocs);
        }

        try
        {
            var opts = new IndexDocumentsOptions
            {
                ThrowOnAnyError = true
            };

            _logger.LogInformation($"Loading {sampleDocuments.Count} index records to index...");

            await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(sampleDocuments), opts);
        }
        catch (AggregateException aggregateException)
        {
            _logger.LogError("Partial failures detected. Some documents failed to index.");
            foreach (var exception in aggregateException.InnerExceptions)
            {
                _logger.LogError(exception.Message);
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

        // with markdown
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
            dictionary["NoteChunkVector"] = await GenerateEmbeddingAsync(sourceNote.NoteChunk);

        var document = new SearchDocument(dictionary);

        return document;
    }
    private async Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text)
    {
        var response = await _openAIClient.GetEmbeddingsAsync(_functionSettings.AzureOpenAiEmbeddingDeployement, new EmbeddingsOptions(text));

        return response.Value.Data[0].Embedding;
    }

    private int GetTokenCount(string text)
    {
        var encoder = ModelToEncoder.For(_functionSettings.AzureOpenAiEmbeddingModel);

        return encoder.CountTokens(text);
    }
}
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
