
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
        [FromServices] AzureSearchService search)
    {
        // do the search here
        //var searchResults = await search.QueryDocumentsAsync("search query");
        // stuff results into the ChatHistory[]
        // TODO: add the phancy plugins and stuff HERE <--
        // call completion??
        // 
        return Results.Ok(await chat.CompleteChat("It works!"));
    }

    private static IResult GetFrontEndSettings(HttpContext httpContext)
    {
        var settings = new FrontendSettings
        {
            AuthEnabled = false,
            FeedbackEnabled = false,
            Ui = new UiSettings
            {
                Title = "Damu",
                ChatTitle = "Start chatting",
                ChatDescription = "This chatbot is configured to answer your questions",
                ShowShareButton = true
            },
            SanitizeAnswer = false
        };

        return Results.Json(settings);
    }

}
