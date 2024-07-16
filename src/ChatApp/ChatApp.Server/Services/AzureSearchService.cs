using System.ComponentModel;
using Azure.Core;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ChatApp.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace ChatApp.Server.Services;

public class AzureSearchService(SearchClient searchClient)
{
    [KernelFunction("query_documents_async")]
    [Description("Query relevant content from Azure Search")]
    [return: Description("Relevant content text data")]
    public async Task<IActionResult> QueryDocumentsAsync(
        string? query = null,
        float[]? embedding = null,
        CancellationToken cancellationToken = default)
    {
        if (query is null && embedding is null)
        {
            throw new ArgumentException("Either query or embedding must be provided");
        }

        string semanticConfigurationName = "damu-semantic-config";
        bool useSemanticCaptions = false;

        var options = new SearchOptions
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
                SemanticConfigurationName = semanticConfigurationName
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
        };

        SearchResults<SearchDocument> searchResult = await searchClient.SearchAsync<SearchDocument>(query, options, cancellationToken=default);

        // Assemble sources here.
        // Example output for each SearchDocument:
        // {
        //   "@search.score": 11.65396,
        //   "id": "Northwind_Standard_Benefits_Details_pdf-60",
        //   "content": "x-ray, lab, or imaging service, you will likely be responsible for paying a copayment or coinsurance. The exact amount you will be required to pay will depend on the type of service you receive. You can use the Northwind app or website to look up the cost of a particular service before you receive it.\nIn some cases, the Northwind Standard plan may exclude certain diagnostic x-ray, lab, and imaging services. For example, the plan does not cover any services related to cosmetic treatments or procedures. Additionally, the plan does not cover any services for which no diagnosis is provided.\nIt’s important to note that the Northwind Standard plan does not cover any services related to emergency care. This includes diagnostic x-ray, lab, and imaging services that are needed to diagnose an emergency condition. If you have an emergency condition, you will need to seek care at an emergency room or urgent care facility.\nFinally, if you receive diagnostic x-ray, lab, or imaging services from an out-of-network provider, you may be required to pay the full cost of the service. To ensure that you are receiving services from an in-network provider, you can use the Northwind provider search ",
        //   "category": null,
        //   "sourcepage": "Northwind_Standard_Benefits_Details-24.pdf",
        //   "sourcefile": "Northwind_Standard_Benefits_Details.pdf"
        // }
        //_logger.LogInformation("SearchAsync found {count} relevant documents.", response.TotalCount);
        //_logger.LogInformation("Search results reduced to {nearestNeighbors} by KNearestNeighborsCount parameter.", options.VectorSearch.Queries.FirstOrDefault()?.KNearestNeighborsCount);

        var searchResults = new List<SearchResult<SearchDocument>>();

        await foreach (SearchResult<SearchDocument> searchResultDocument in searchResult.GetResultsAsync())
        {
            //_logger.LogTrace(
            //    "Search results include the chunk in order {noteChunkOrder} of note with NodeId {noteId} with a score of {score}.",
            //    searchResultDocument.Document[IndexFields.NoteChunkOrder],
            //    searchResultDocument.Document[IndexFields.NoteId],
            //    searchResultDocument.Score);

            searchResults.Add(searchResultDocument);
        }

        return new OkObjectResult(searchResults);
    }

}
