using API;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
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

        services.AddHttpClient(); // Registers IHttpClientFactory and allows you to use HttpClient
        services.AddSingleton<QueryFhirPlugin>();
    })
    .Build();

await host.RunAsync();
