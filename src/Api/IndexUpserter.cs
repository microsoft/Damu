#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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
using System.Reflection.Metadata;
using Tiktoken;

namespace Api;

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
        try
        {
            using var blobStreamReader = new StreamReader(stream);

            var content = await blobStreamReader.ReadToEndAsync();

            _logger.LogInformation($"C# Blob trigger function Processed blob\n Name: {name} \n Data: {content}");

            // testing 
            await DeleteIndexAsync();

            await CreateIndexAsync();

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

            await LoadIndexAsync(inputDocuments);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private async Task CreateIndexAsync()
    {
        Console.WriteLine("Creating (or updating) search index");

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
                                ApiKey = _functionSettings.AzureOpenAiKey,
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
                                // title
                                new SemanticField("NoteChunk")
                            }
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
                new SearchField("NoteChunkOrder", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SearchField("NoteChunkVector", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _functionSettings.ModelDimensions,
                    VectorSearchProfileName = _functionSettings.VectorSearchProfileName
                },

                // todo: potentially scrape <h1> tag as title field

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

        var result = await _searchIndexClient.CreateOrUpdateIndexAsync(index);

        Console.WriteLine(result);
    }

    private async Task LoadIndexAsync(List<SourceNoteRecord> inputDocuments)
    {
        List<SearchDocument> sampleDocuments = [];

        foreach (var document in inputDocuments)
        {
            Console.WriteLine(document);

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
            await _searchClient.IndexDocumentsAsync(IndexDocumentsBatch.Upload(sampleDocuments));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    private async Task<SearchDocument> ConvertToSearchDocumentAsync(SourceNoteRecord sourceNote)
    {
        var dictionary = sourceNote.ToDictionary();

        var content = !string.IsNullOrWhiteSpace(sourceNote.NoteInHtml) ? sourceNote.NoteInHtml : string.Empty;

        dictionary["NoteChunkVector"] = await GenerateEmbeddingAsync(content);

        var document = new SearchDocument(dictionary);

        return document;
    }

    private async Task<List<SearchDocument>> RecursivelySplitNoteContent(SourceNoteRecord sourceNote)
    {
        if (string.IsNullOrWhiteSpace(sourceNote.NoteInHtml))
            return [];

        var analyzeRequest = new AnalyzeDocumentContent
        {
            Base64Source = BinaryData.FromString(sourceNote.NoteInHtml)
        };

        // with plain text
        //var analysis = await _docIntelClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", analyzeRequest, outputContentFormat: ContentFormat.Text);
        //var chunkerOverlapTokens = Convert.ToInt32(_functionSettings.ChunkOverlapPercent * _functionSettings.MaxChunkSize);
        //var lines = TextChunker.SplitPlainTextLines(analysis.Value.Content, _functionSettings.MaxChunkSize);
        //var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, _functionSettings.MaxChunkSize, chunkerOverlapTokens);

        // with markdown
        var analysis = await _docIntelClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", analyzeRequest, outputContentFormat: ContentFormat.Markdown);
        var chunkerOverlapTokens = Convert.ToInt32(_functionSettings.ChunkOverlapPercent * _functionSettings.MaxChunkSize);
        var lines = TextChunker.SplitMarkDownLines(analysis.Value.Content, _functionSettings.MaxChunkSize);
        var paragraphs = TextChunker.SplitMarkdownParagraphs(lines, _functionSettings.MaxChunkSize, chunkerOverlapTokens);

        var results = new List<SearchDocument>();

        for (var i = 0; i < paragraphs.Count; i++)
        {
            var p = paragraphs[i];
            var split = new SourceNoteRecord(sourceNote);

            split.NoteChunk = p;
            split.NoteChunkOrder = i;

            var searchDoc = await ConvertToSearchDocumentAsync(split);
            results.Add(searchDoc);
        }

        return results;
    }

    private int GetTokenCount(string text)
    {
        var encoder = ModelToEncoder.For(_functionSettings.AzureOpenAiEmbeddingModel);

        return encoder.CountTokens(text);
    }

    private async Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text)
    {
        var response = await _openAIClient.GetEmbeddingsAsync(_functionSettings.AzureOpenAiEmbeddingDeployement, new EmbeddingsOptions(text));

        return response.Value.Data[0].Embedding;
    }

    // for testing
    private async Task DeleteIndexAsync()
    {
        Console.WriteLine("Deleting search index");

        await _searchIndexClient.DeleteIndexAsync(_functionSettings.SearchIndexName);
    }
}
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
