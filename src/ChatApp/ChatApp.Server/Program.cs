using Azure;
using Azure.Identity;
using ChatApp.Server.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace ChatApp.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<CosmosOptions>(builder.Configuration.GetSection(nameof(CosmosOptions)));

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddTransient(services =>
        {
            var options = services.GetRequiredService<IOptions<CosmosOptions>>().Value;

            return string.IsNullOrWhiteSpace(options.CosmosKey)
            ? new CosmosClient(options.CosmosEndpoint, new DefaultAzureCredential())
            : new CosmosClient(options.CosmosEndpoint, new AzureKeyCredential(options.CosmosKey));
        });
        builder.Services.AddTransient<CosmosConversationService>();

        builder.Services.AddOpenAiServices();

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
