
namespace ChatApp.Server;

public static partial class Endpoints
{
    public static WebApplication MapHistoryEndpoints(this WebApplication app)
    {
        app.MapGroup("history");

        app.MapGet("ensure", GetEnsureHistory);

        // Not implemented
        app.MapPost("generate", GenerateHistory);
        app.MapPost("update", UpdateHistory);
        app.MapPost("message_feedback", MessageFeedback);
        app.MapDelete("delete", DeleteHistory);
        app.MapGet("list", ListHistory);
        app.MapPost("read", ReadHistory);
        app.MapPost("rename", RenameHistory);
        app.MapDelete("delete_all", DeleteAllHistory);
        app.MapPost("clear", ClearHistory);

        return app;
    }

    private static IResult GetEnsureHistory(HttpContext httpContext)
    {
        string response = @"{ ""error"": ""CosmosDB is not configured""}";
        return Results.NotFound(System.Text.Json.JsonSerializer.Deserialize<object>(response));
    }

    #region NotImplemented
    private static async Task ClearHistory(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task DeleteAllHistory(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task RenameHistory(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task ReadHistory(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task ListHistory(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task DeleteHistory(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task MessageFeedback(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task UpdateHistory(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task GenerateHistory(HttpContext context)
    {
        throw new NotImplementedException();
    }
    #endregion

}
