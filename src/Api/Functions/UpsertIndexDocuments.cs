#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Api.Models;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Text;
using Newtonsoft.Json;
using Tiktoken;

namespace Api.Functions;

public class UpsertIndexDocuments
{
    private readonly FunctionSettings _functionSettings;
    private readonly ILogger<UpsertIndexDocuments> _logger;
    private readonly OpenAIClient _openAIClient;
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _searchIndexClient;
    private readonly DocumentIntelligenceClient _docIntelClient;

    // todo: move to orchestration function pattern to handle larger throughput
    // todo: add in upsert for individual notes
    public UpsertIndexDocuments(DocumentIntelligenceClient docIntelClient, FunctionSettings functionSettings, ILogger<UpsertIndexDocuments> logger, OpenAIClient openAiClient, SearchClient searchClient, SearchIndexClient searchIndexClient)
    {
        _docIntelClient = docIntelClient;
        _functionSettings = functionSettings;
        _logger = logger;
        _openAIClient = openAiClient;
        _searchClient = searchClient;
        _searchIndexClient = searchIndexClient;
    }

    [Function(nameof(UpsertIndexDocuments))]
    public async Task RunAsync([BlobTrigger("notes/{blobName}", Connection = "IncomingBlobConnStr")] string blobContent, string blobPath, Uri blobUri)
    {
        _logger.LogInformation("Processing blob {blobName}...", blobPath);

        // clean out BOM if present
        var cleanedContent = blobContent.Trim().Replace("\uFEFF", "");

        var sourceNoteRecord = JsonConvert.DeserializeObject<SourceNoteRecord>(cleanedContent);

        if (sourceNoteRecord == null)
        {
            _logger.LogError("Failed to deserialize note record from blob {blobName}.", blobPath);

            return;
        }

        if (!sourceNoteRecord.NoteId.HasValue)
        {
            _logger.LogError("Note record from blob {blobName} has no ID. Skipping indexing.", blobPath);

            return;
        }

        _logger.LogInformation("Successfully identified note with ID {noteId} in blob {blobName}.", sourceNoteRecord.NoteId, blobPath);

        sourceNoteRecord.FilePath = blobPath;
        sourceNoteRecord.Title = blobPath.Split('/').LastOrDefault();
        sourceNoteRecord.Url = blobUri.ToString();

        List<SearchDocument> sampleDocuments = [];

        if (string.IsNullOrWhiteSpace(sourceNoteRecord.NoteContent))
        {
            _logger.LogWarning("Note {noteId} has no content. Skipping indexing.", sourceNoteRecord.NoteId);

            return;
        }

        var tokenCount = GetTokenCount(sourceNoteRecord.NoteContent);

        if (tokenCount > _functionSettings.MaxChunkSize)
        {
            _logger.LogDebug("Note {noteId} is too large to index in one chunk. Splitting...", sourceNoteRecord.NoteId);
            sampleDocuments = await RecursivelySplitNoteContent(sourceNoteRecord);
            _logger.LogDebug("Note {noteId} chunking produced {count} chunks.", sourceNoteRecord.NoteId, sampleDocuments.Count);
        }
        else
        {
            _logger.LogDebug("Note {noteId} is small enough to index in one chunk.", sourceNoteRecord.NoteId);
            sourceNoteRecord.NoteChunk = sourceNoteRecord.NoteContent;
            sourceNoteRecord.NoteChunkOrder = 0;
            sourceNoteRecord.IndexRecordId = $"{sourceNoteRecord.NoteId}-{sourceNoteRecord.NoteChunkOrder}";
            sampleDocuments = [await ConvertToSearchDocumentAsync(sourceNoteRecord)];
        }


        await DeleteOldChunksAsync(sourceNoteRecord.NoteId.Value, sampleDocuments);


        await LoadIndexAsync(sampleDocuments);
    }

    private async Task DeleteOldChunksAsync(long noteId, List<SearchDocument> documents)
    {
        var lastChunkIndex = documents
            .OrderBy(c => c[IndexFields.NoteChunkOrder])
            .Select(c => int.Parse(c[IndexFields.NoteChunkOrder] as string ?? string.Empty))
            .LastOrDefault();

        var oldChunksToDelete = new List<string>();

        // intellisense not recognizing NoteId cannot be null here...
        await foreach (var indexRecordId in GetExistingIndexRecordsAsync(noteId))
        {
            var oldChunkOrder = int.Parse(indexRecordId.Split('-')[^1]);

            if (oldChunkOrder > lastChunkIndex)
                oldChunksToDelete.Add(indexRecordId);
        }

        Response<IndexDocumentsResult>? deleteDocumentsResult = null;

        if (oldChunksToDelete.Count > 0)
        {
            _logger.LogInformation("Deleting {count} old chunks for note {noteId}...", oldChunksToDelete.Count, noteId);

            _logger.LogTrace("Old chunks to delete: {chunks}", string.Join(',', oldChunksToDelete));

            try
            {
                deleteDocumentsResult = await _searchClient.DeleteDocumentsAsync(oldChunksToDelete);
            }
            catch (AggregateException aggregateException)
            {
                _logger.LogError("Partial failures detected. Some documents failed to delete.");

                foreach (var exception in aggregateException.InnerExceptions)
                {
                    _logger.LogError("{exception}", exception.Message);
                }
            }

            foreach (var deleteResult in deleteDocumentsResult?.Value?.Results ?? [])
            {
                _logger.LogTrace("Deleted document {id} with status {status}.", deleteResult.Key, deleteResult.Status);
            }
        }
    }

    private async Task LoadIndexAsync(List<SearchDocument> documents)
    {
        Response<IndexDocumentsResult>? indexingResult = null;

        try
        {
            var opts = new IndexDocumentsOptions
            {
                ThrowOnAnyError = true
            };

            _logger.LogInformation("Loading {count} index records to index...", documents.Count);

            indexingResult = await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.MergeOrUpload(documents), opts);
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

        _logger.LogInformation("Index loading completed.");
    }

    private async Task<List<SearchDocument>> RecursivelySplitNoteContent(SourceNoteRecord sourceNote)
    {
        if (string.IsNullOrWhiteSpace(sourceNote.NoteContent))
            return [];

        var analyzeRequest = new AnalyzeDocumentContent
        {
            Base64Source = BinaryData.FromString(sourceNote.NoteContent)
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

        // this composite key means that MergeOrUpload will overwrite the previous chunk in any size increase scenarios for source material
        sourceNote.IndexRecordId = $"{sourceNote.NoteId}-{sourceNote.NoteChunkOrder}";

        var dictionary = sourceNote.ToDictionaryForIndexing();

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

    /// <summary>
    /// Get a list of records in the index that have the same noteId as the current note
    /// </summary>
    /// <param name="noteId">Id of the note that returned records are associated with</param>
    /// <returns>List of IndexRecordIds of matched records</returns>
    private async IAsyncEnumerable<string> GetExistingIndexRecordsAsync(long noteId)
    {
        var searchOptions = new SearchOptions
        {
            Filter = $"noteId eq {noteId}",
            IncludeTotalCount = true,
            Select = { IndexFields.IndexRecordId }
        };

        var result = await _searchClient.SearchAsync<string>("*", searchOptions);

        await foreach (var record in result.Value.GetResultsAsync())
        {
            yield return record.Document;
        }
    }
}
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
