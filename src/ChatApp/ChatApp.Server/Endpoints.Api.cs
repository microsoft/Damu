
using ChatApp.Server.Models;
using ChatApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Server;

public static partial class Endpoints
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapGet("/frontend_settings", (HttpContext httpContext) => Results.Json(new FrontendSettings()))
            .WithName("GetFrontendSettings")
            .WithOpenApi();

        app.MapPost("/conversation", PostConversation);

        return app;
    }

    private static async Task<IResult> PostConversation(
        [FromServices] ChatCompletionService chat,
        [FromServices] AzureSearchService search,
        [FromBody] ConversationRequest history)
    {
        // do the search here
        //var searchResults = await search.QueryDocumentsAsync("search query");
        // stuff results into the ChatHistory[]
        // TODO: add the phancy plugins and stuff HERE <--
        // call completion??
        // 
        return Results.Ok(await chat.CompleteChat([.. history.Messages]));
    }

}
