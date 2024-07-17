
using ChatApp.Server.Models;
using ChatApp.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace ChatApp.Server;

public static partial class Endpoints
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapGet("/frontend_settings", (HttpContext httpContext) => Results.Json(new FrontendSettings()))
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
        // do the search here
        var searchResults = await search.QueryDocumentsAsync(history.Messages[^1].Content);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var toolMsg = new Message
        {
            Id = Guid.NewGuid().ToString(),
            Role = AuthorRole.Tool.ToString().ToLower(),
            Date = DateTime.UtcNow,
            Content = JsonSerializer.Serialize<ToolContentResponse>(searchResults, options)
        };
        // add search results to the conversation
        history.Messages.Add(toolMsg);
        
        // return result from LLM
        return Results.Ok(await chat.CompleteChat([.. history.Messages]));
    }

    private static async Task<IResult> PostSearch(
        [FromServices] AzureSearchService search,
        [FromQuery] string query)
    {
        var searchResults = await search.QueryDocumentsAsync(query);
        return Results.Ok(searchResults);
    }

}
