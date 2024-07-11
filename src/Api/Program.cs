using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Core;
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

            return new DocumentIntelligenceClient(
                settings.DocIntelEndPoint,
                new DefaultAzureCredential());
        });
        services.AddTransient(services =>
        {
            var settings = services.GetRequiredService<FunctionSettings>();

            return new OpenAIClient(settings.AzureOpenAiEndpoint, new DefaultAzureCredential()); //AzureKeyCredential settings.AzureOpenAiKey
        });
        services.AddTransient(services =>
        {
            var settings = services.GetRequiredService<FunctionSettings>();

            return new SearchClient(
                settings.SearchEndpoint,
                settings.SearchIndexName,
                new DefaultAzureCredential()); // todo: move to managed identity
        });
        services.AddTransient(services =>
        {
            var settings = services.GetRequiredService<FunctionSettings>();

            return new SearchIndexClient(
                settings.SearchEndpoint,
                 new DefaultAzureCredential());
        });
    })
    .Build();

await host.RunAsync();
