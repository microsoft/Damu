using ChatApp.Server.Models;

namespace ChatApp.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<AISearchOptions>(builder.Configuration.GetSection(nameof(AISearchOptions)));
        builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection(nameof(OpenAIOptions)));
        builder.Services.Configure<CosmosOptions>(builder.Configuration.GetSection(nameof(CosmosOptions)));
        builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(nameof(StorageOptions)));
        builder.Services.Configure<FhirOptions>(builder.Configuration.GetSection(nameof(FhirOptions)));

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // for FHIR plugin
        builder.Services.AddHttpClient();

        // Register all of our things from ChatAppExtensions
        builder.Services.AddChatAppServices(builder.Configuration);

        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapApiEndpoints();
        app.MapHistoryEndpoints();

        app.MapFallbackToFile("/index.html");

        app.Run();
    }
}
