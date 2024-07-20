using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ChatApp.Server.Models;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Reflection;
using Microsoft.Azure.Cosmos.Core;
using System.Collections.Frozen;

namespace ChatApp.Server.Services;

public class AzureSearchService(SearchClient searchClient, IOptions<AISearchOptions> options)
{
    private string[] _indexContentFields = [
        "CSN",
        "NoteType",
        "NoteStatus",
        "AuthorId",
        "AuthorFirstName",
        "AuthorLastName",
        "Department",
        "Gender",
        "BirthDate",
        "NoteId",
        "PatientFirstName",
        "PatientLastName",
        "MRN"
        ];

    public async Task<ToolContentResponse> QueryDocumentsAsync(
        string? query = null,
        float[]? embedding = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options?.Value?.SemanticConfigurationName);

        if (query is null && embedding is null)
            throw new ArgumentException("Either query or embedding must be provided");

        var response = await searchClient.SearchAsync<SearchDocument>(query, new SearchOptions
        {
            Filter = "",
            Size = 5,
            Select = {
               "*"
            },
            IncludeTotalCount = true,
            QueryType = SearchQueryType.Full,
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = options?.Value.SemanticConfigurationName,
                QueryCaption = new(QueryCaptionType.None)
            },
            VectorSearch = new()
            {
                Queries = {
                new VectorizableTextQuery(text: query)
                {
                        KNearestNeighborsCount = 5,
                        Fields = { "NoteChunkVector"},
                        Exhaustive = true
                    }
                },
            }
        });

        if (response.Value == null)
            throw new Exception(response.GetRawResponse().ReasonPhrase);

        var searchResult = response.Value;

        var sb = new List<SupportingContentRecord>();
        await foreach (var doc in searchResult.GetResultsAsync())
        {
            // "FilePath": "notes/1000054825.json",
            // "Title": "1000054825.json",
            // "Url": "notes/1000054825.json"
            doc.Document.TryGetValue("NoteChunkOrder", out var chunkOrderValue);

            doc.Document.TryGetValue("Title", out var titleValue);
            doc.Document.TryGetValue("FilePath", out var filePathValue);
            doc.Document.TryGetValue("Url", out var urlValue);

            // parse the search results into SupportingContentRecord
            string? contentValue;
            try
            {
                doc.Document.TryGetValue("NoteChunk", out var value);
                contentValue = (string)value;
            }
            catch (ArgumentNullException)
            {
                contentValue = null;
            }

            // put all other sortable and retrievable fields into 'additionalcontent'
            var indexProps = doc.Document.ToDictionary().Where(d => _indexContentFields.Contains(d.Key)).ToDictionary();

            if (titleValue is string title &&
                filePathValue is string filePath &&
                urlValue is string url &&
                chunkOrderValue is int chunkOrder &&
                contentValue is string content)
            {
                content = content.Replace('\r', ' ').Replace('\n', ' ');
                sb.Add(new SupportingContentRecord(title, content, url, filePath, chunkOrder.ToString(), JsonSerializer.Serialize(indexProps)));
            }
        }

        return new ToolContentResponse(sb, [query]);
    }

    public async Task<List<MinimalSearchResult>> ReturnAllResultsAsync(string query, double relevancyThreshold)
    {
        var top = 1000;
        // alternative is to retain these values as a nonkey index and instead using a plain number for index then using that to help page results..
        long largestNoteId = 0;
        long largestChunkOrder = 0;

        long? totalResultCount = null;
        List<MinimalSearchResult> allResults = [];

        var initialResults = await SearchPageAsync(query, top, largestNoteId, largestChunkOrder);

        totalResultCount = initialResults.Value.TotalCount;

        await foreach (var doc in initialResults.Value.GetResultsAsync())
        {
            if (!doc.Score.HasValue || doc.Score > relevancyThreshold)
                return [];

            allResults.Add(new MinimalSearchResult(doc.Score.Value, doc.Document));
        }

        var lastRecord = allResults
            .OrderByDescending(r => r.NoteId)
            .ThenByDescending(r => r.NoteChunkOrder)
            .LastOrDefault();

        if (lastRecord == null)
            return [];

        largestNoteId = lastRecord!.NoteId!.Value; // could probably make NoteId non-nullable
        largestChunkOrder = lastRecord.NoteChunkOrder;

        // while our last document retrieved is above the relevancy threshold
        // and we haven't retrieved all the results
        // search again, append the results sorted to our list
        // and update the largest note id / chunk orders to assist with pagination
        while (allResults.Count > 0 && allResults.Last().Score > relevancyThreshold && totalResultCount > allResults.Count)
        {
            var nextPageResults = await SearchPageAsync(query, top, largestNoteId, largestChunkOrder);
            // if (doc.Score.HasValue && doc.Score > relevancyThreshold)


            await foreach (var doc in initialResults.Value.GetResultsAsync())
            {
                if (!doc.Score.HasValue || doc.Score > relevancyThreshold)
                    return allResults;

                allResults.Add(new MinimalSearchResult(doc.Score.Value, doc.Document));
            }

            lastRecord = allResults
                .OrderByDescending(r => r.NoteId)
                .ThenByDescending(r => r.NoteChunkOrder)
                .LastOrDefault();

            if (lastRecord == null)
                return allResults;

            // almost certainly a bug here somewhere
            largestNoteId = lastRecord!.NoteId!.Value; // could probably make NoteId non-nullable
            largestChunkOrder = lastRecord.NoteChunkOrder;
        }

        // trim results down by relevancy score
        return allResults.Where(r => r.Score > relevancyThreshold).ToList();

        //    // https://learn.microsoft.com/en-us/azure/search/index-similarity-and-scoring#number-of-ranked-results-in-a-full-text-query-response
        //    // https://learn.microsoft.com/en-us/azure/search/search-pagination-page-layout#paging-results
        //    await foreach (var doc in response.Value.GetResultsAsync())
        //    {
        //        // doc.Score = The relevance score of the document compared to other documents returned by the query.
        //        // https://learn.microsoft.com/en-us/azure/search/hybrid-search-ranking
        //        // used Reciprocal Rank Fusion (RRF) to combine the results from the two queries (viz. vector and simple)
        //        // scoring only applied to fields marked searchable (most on this index)
        //        // Upper limit is bounded by the number of queries being fused, with each query contributing a maximum of approximately 1
        //        // to the RRF score. For example, merging three queries would produce higher RRF scores than if only two search results are merged.
        //        // semantic ranking doesn't participate in RRF... It's score is always reported seaparately in the query
        //        // ** customer should play with including semantic reranking in the RRF or not and based on that modify this relevancy threshold **
        //        // https://learn.microsoft.com/en-us/azure/search/hybrid-search-ranking#diagram-of-a-search-scoring-workflow
        //        if (doc.Score.HasValue && doc.Score > relevancyThreshold) // check in on this a bit more...
        //            allResults.Add(new MinimalSearchResult(doc.Score.Value, doc.Document));
        //
        //        // is there someway we could take advantage of this feature?
        //        // https://learn.microsoft.com/en-us/azure/search/index-similarity-and-scoring#featuresmode-parameter-preview
        //    }
        //
        //    // https://learn.microsoft.com/en-us/azure/search/search-pagination-page-layout#counting-matches
        //    //response.Value.TotalCount
        //
        //    return allResults;
    }


    private async Task<Response<SearchResults<SearchDocument>>> SearchPageAsync(string query, int top, long largestNoteId, long largestChunkOrder)
    {
        var opts = new SearchOptions
        {
            // https://learn.microsoft.com/en-us/azure/search/search-filters
            // todo: join user filters here
            Filter = $"NoteId ge {largestNoteId} and NoteChunkOrder ge {largestChunkOrder}",
            OrderBy = { "NoteId asc", "NoteChunkOrder asc" },
            Size = top,
            // keep only most important values
            Select = {
                string.Join(',',
                [
                    nameof(MinimalSearchResult.IndexRecordId),
                    nameof(MinimalSearchResult.NoteId),
                    nameof(MinimalSearchResult.CSN),
                    nameof(MinimalSearchResult.MRN),
                    nameof(MinimalSearchResult.NoteType),
                    nameof(MinimalSearchResult.NoteStatus),
                    nameof(MinimalSearchResult.AuthorId),
                    nameof(MinimalSearchResult.PatientFirstName),
                    nameof(MinimalSearchResult.PatientLastName),
                    nameof(MinimalSearchResult.AuthorFirstName),
                    nameof(MinimalSearchResult.AuthorLastName),
                    nameof(MinimalSearchResult.Department),
                    nameof(MinimalSearchResult.Gender),
                    nameof(MinimalSearchResult.BirthDate)
                ])
            },
            IncludeTotalCount = true,
            // https://learn.microsoft.com/en-us/azure/search/search-pagination-page-layout#tips-for-unexpected-results
            SearchMode = SearchMode.All,
            // currently using full text lucene search
            // other options are simple (a la old google search style syntax) and Semantic 
            QueryType = SearchQueryType.Full,
            // Semantic ranker is a premium feature, billed by usage
            // https://learn.microsoft.com/en-us/azure/search/semantic-search-overview
            // 1. adds secondary ranking over an initial result set that was scored using BM25 or RRF
            // 2. extracts and returns captions and answers in the response, which you can render on a search page to improve the user's search experience.
            // For each document in the search result, the summarization model accepts up to 2,000 tokens, where a token is approximately 10 characters.
            // Inputs are assembled from the "title", "keyword", and "content" fields listed in the semantic configuration
            // Note that these constraints are separate from the constraints on the input text for the query vectorization model.
            SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = options?.Value.SemanticConfigurationName,
                QueryCaption = new(QueryCaptionType.None)
            },
            VectorSearch = new()
            {
                Queries = {
                    // https://learn.microsoft.com/en-us/azure/search/hybrid-search-ranking#weighted-scores
                    new VectorizableTextQuery(text: query)
                    {
                        KNearestNeighborsCount = 5,
                        Fields = { "NoteChunkVector"},
                        Exhaustive = true
                    }
                },
            }
        };

        return await searchClient.SearchAsync<SearchDocument>(query, opts);
    }


    public async Task<List<SupportingContentRecord>> RetrieveDocumentsForChatAsync(IEnumerable<string> indexRecordIds)
    {
        var response = await searchClient.SearchAsync<SearchDocument>("*", new SearchOptions
        {
            Filter = $"search.in({nameof(Note.IndexRecordId)}, '{string.Join(',', indexRecordIds)})'",
            Size = indexRecordIds.Count(),
            Select = {
               "*"
            },
            QueryType = SearchQueryType.Simple
        });

        if (response.Value == null)
            throw new Exception(response.GetRawResponse().ReasonPhrase);

        var searchResult = response.Value;

        var sb = new List<SupportingContentRecord>();
        await foreach (var doc in searchResult.GetResultsAsync())
        {
            doc.Document.TryGetValue("NoteChunkOrder", out var chunkOrderValue);
            doc.Document.TryGetValue("Title", out var titleValue);
            doc.Document.TryGetValue("FilePath", out var filePathValue);
            doc.Document.TryGetValue("Url", out var urlValue);

            // parse the search results into SupportingContentRecord
            string? contentValue;

            try
            {
                doc.Document.TryGetValue("NoteChunk", out var value);
                contentValue = (string)value;
            }
            catch (ArgumentNullException)
            {
                contentValue = null;
            }

            // put all other sortable and retrievable fields into 'additionalcontent'
            var indexProps = doc.Document.ToDictionary().Where(d => _indexContentFields.Contains(d.Key)).ToDictionary();

            if (titleValue is string title &&
                filePathValue is string filePath &&
                urlValue is string url &&
                chunkOrderValue is int chunkOrder &&
                contentValue is string content)
            {
                content = content.Replace('\r', ' ').Replace('\n', ' ');
                sb.Add(new SupportingContentRecord(title, content, url, filePath, chunkOrder.ToString(), JsonSerializer.Serialize(indexProps)));
            }
        }

        return sb;
    }
}
