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
    public async Task RunAsync([BlobTrigger("notes/{name}", Connection = "IncomingBlobConnStr")] string content, string name)
    {
        _logger.LogInformation("Processing blob {name}...", name);

        // clean out BOM if present
        var cleanedContent = content.Trim().Replace("\uFEFF", "");

        var sourceNoteRecord = JsonConvert.DeserializeObject<SourceNoteRecord>(cleanedContent);

        if (sourceNoteRecord == null)
        {
            _logger.LogError("Failed to deserialize note record from blob {name}.", name);

            return;
        }

        _logger.LogInformation("Successfully identified note with ID {noteId} in blob {name}.", sourceNoteRecord.NoteId, name);

        await LoadIndexAsync(sourceNoteRecord);

        _logger.LogInformation("Index loading completed.");
    }

    private async Task LoadIndexAsync(SourceNoteRecord document)
    {
        List<SearchDocument> sampleDocuments = [];

        var content = !string.IsNullOrWhiteSpace(document.NoteInHtml) ? document.NoteInHtml : string.Empty;

        var tokenCount = GetTokenCount(content);

        if (tokenCount > _functionSettings.MaxChunkSize)
        {
            _logger.LogDebug("Note {noteId} is too large to index in one chunk. Splitting...", document.NoteId);
            sampleDocuments = await RecursivelySplitNoteContent(document);
            _logger.LogDebug("Note {noteId} chunking produced {count} chunks.", document.NoteId, sampleDocuments.Count);
        }
        else
        {
            _logger.LogDebug("Note {noteId} is small enough to index in one chunk.", document.NoteId);
            document.NoteChunk = content;
            document.NoteChunkOrder = 0;
            sampleDocuments = [await ConvertToSearchDocumentAsync(document)];
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
