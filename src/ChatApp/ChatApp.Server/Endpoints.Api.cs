
using ChatApp.Server.Models;
using ChatApp.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Linq;
using System.Text.Json;

namespace ChatApp.Server;

public static partial class Endpoints
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapGet("/frontend_settings", (HttpContext httpContext, [FromServices] IOptions<FrontendSettings> options) => options.Value)
            .WithName("GetFrontendSettings")
            .WithOpenApi();

        app.MapPost("/conversation", PostConversation);
        app.MapPost("/search", PostSearch);

        app.MapGet("notes/{id}", async (long id, [FromServices] NoteService noteService) =>
        {
            var note = await noteService.GetNoteAsync(id);

            return note is null ? Results.NotFound() : Results.Ok(note);
        });

        return app;
    }

    private static async Task<IResult> PostConversation(
        [FromServices] ChatCompletionService chat,
        [FromServices] AzureSearchService search,
        [FromBody] ConversationRequest history)
    {
        // filter out any existing tool messages (search results)
        history.Messages = history.Messages.Where(m => !m.Role.Equals(AuthorRole.Tool.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();

        var userQuery = history.Messages[^1].Content;
        // get user intent using llm and then send to search.. is this worthwhile with a hybrid search?

        // test values... these should definitely be experimented with
        var take = 5;
        double relevancyThreshold = 0.5;

        // use for export, retrieve all results within a certain relevancy threshold
        var minimalSearchResults = await search.ReturnAllResultsAsync(userQuery, relevancyThreshold);

        // restrict expansive search result to most relevant by take (you can play with take value based on estimated token counts)
        var indexRecordIds = minimalSearchResults.OrderByDescending(msr => msr.Score).Take(take).Select(r => r.IndexRecordId);

        var supportingContentRecords = await search.RetrieveDocumentsForChatAsync(indexRecordIds);

        var toolContentResponse = new ToolContentResponse(supportingContentRecords, [userQuery]);

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var toolMsg = new Message
        {
            Id = Guid.NewGuid().ToString(),
            Role = AuthorRole.Tool.ToString().ToLower(),
            Date = DateTime.UtcNow,
            Content = JsonSerializer.Serialize(toolContentResponse, options)
        };

        // add search results to the conversation
        history.Messages.Add(toolMsg);

        var completion = await chat.CompleteChat([.. history.Messages]);

        var response = new ChatResponseWithExport(completion, minimalSearchResults);

        return Results.Ok(response);
    }

    private static async Task<IResult> PostSearch(
        [FromServices] AzureSearchService search,
        [FromQuery] string query)
    {
        var searchResults = await search.QueryDocumentsAsync(query);
        return Results.Ok(searchResults);
    }
}
