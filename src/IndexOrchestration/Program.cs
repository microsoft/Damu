using Api.Models;
using API;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<FunctionSettings>();
        services.AddTransient(services =>
        {
            var settings = services.GetRequiredService<FunctionSettings>();

            return string.IsNullOrWhiteSpace(settings.DocIntelKey)
            ? new DocumentIntelligenceClient(settings.DocIntelEndPoint, new DefaultAzureCredential())
            : new DocumentIntelligenceClient(settings.DocIntelEndPoint, new AzureKeyCredential(settings.DocIntelKey));
        });
        services.AddTransient(services =>
        {
            var settings = services.GetRequiredService<FunctionSettings>();

            return string.IsNullOrWhiteSpace(settings.AzureOpenAiKey)
            ? new OpenAIClient(settings.AzureOpenAiEndpoint, new DefaultAzureCredential())
            : new OpenAIClient(settings.AzureOpenAiEndpoint, new AzureKeyCredential(settings.AzureOpenAiKey));
        });
        services.AddTransient(services =>
        {
            var settings = services.GetRequiredService<FunctionSettings>();

            return string.IsNullOrWhiteSpace(settings.SearchKey)
            ? new SearchClient(settings.SearchEndpoint, settings.SearchIndexName, new DefaultAzureCredential())
            : new SearchClient(settings.SearchEndpoint, settings.SearchIndexName, new AzureKeyCredential(settings.SearchKey));
        });
        services.AddTransient(services =>
        {
            var settings = services.GetRequiredService<FunctionSettings>();

            return string.IsNullOrWhiteSpace(settings.SearchKey)
            ? new SearchIndexClient(settings.SearchEndpoint, new DefaultAzureCredential())
            : new SearchIndexClient(settings.SearchEndpoint, new AzureKeyCredential(settings.SearchKey));
        });
    })
    .Build();

await host.RunAsync();
