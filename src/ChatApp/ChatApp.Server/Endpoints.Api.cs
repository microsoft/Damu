
namespace ChatApp.Server;

public static partial class Endpoints
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapGet("/frontend_settings", (HttpContext httpContext) => Results.Json(new FrontendSettings()))
            .WithName("GetFrontendSettings")
            .WithOpenApi();

        return app;
    }
}
