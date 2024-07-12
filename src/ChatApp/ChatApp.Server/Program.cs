using Azure.Identity;
using ChatApp.Server.Services;
using Microsoft.Azure.Cosmos;

namespace ChatApp.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        //builder.Services.AddTransient(services => new CosmosClient("", new DefaultAzureCredential()));
        //builder.Services.AddTransient(services =>
        //{

        //    //ILogger<CosmosConversationService> logger, CosmosClient cosmosClient, string databaseId, string containerId
        //    return new CosmosConversationService(builder.Configuration["Cosmos:Endpoint"], builder.Configuration["Cosmos:Key"]);
        //});

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
