
namespace ChatApp.Server;

public static partial class Endpoints
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapGet("/frontend_settings", GetFrontEndSettings)
            .WithName("GetFrontendSettings")
            .WithOpenApi();

        return app;
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
