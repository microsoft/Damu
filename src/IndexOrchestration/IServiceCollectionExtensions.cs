using Api.Models;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IndexOrchestration;

internal static class IServiceCollectionExtensions
{
    internal static void AddIndexOrchestrationServices(this IServiceCollection services, IConfiguration config)
    {
        var tenantId = config["TenantId"];
        var defaultCreds = string.IsNullOrWhiteSpace(tenantId)
        ? new DefaultAzureCredential()
            : new DefaultAzureCredential(
                new DefaultAzureCredentialOptions
                {
                    TenantId = tenantId
                });

        services.AddTransient(services =>
        {
            var settings = services.GetRequiredService<FunctionSettings>();

            return string.IsNullOrWhiteSpace(settings.DocIntelKey)
            ? new DocumentIntelligenceClient(settings.DocIntelEndPoint, defaultCreds)
            : new DocumentIntelligenceClient(settings.DocIntelEndPoint, new AzureKeyCredential(settings.DocIntelKey));
        });
        services.AddTransient(services =>
        {
            var settings = services.GetRequiredService<FunctionSettings>();

            return string.IsNullOrWhiteSpace(settings.AzureOpenAiKey)
            ? new OpenAIClient(settings.AzureOpenAiEndpoint, defaultCreds)
            : new OpenAIClient(settings.AzureOpenAiEndpoint, new AzureKeyCredential(settings.AzureOpenAiKey));
        });
        services.AddTransient(services =>
        {
            var settings = services.GetRequiredService<FunctionSettings>();

            return string.IsNullOrWhiteSpace(settings.SearchKey)
            ? new SearchClient(settings.SearchEndpoint, settings.SearchIndexName, defaultCreds)
            : new SearchClient(settings.SearchEndpoint, settings.SearchIndexName, new AzureKeyCredential(settings.SearchKey));
        });
        services.AddTransient(services =>
        {
            var settings = services.GetRequiredService<FunctionSettings>();

            return string.IsNullOrWhiteSpace(settings.SearchKey)
            ? new SearchIndexClient(settings.SearchEndpoint, defaultCreds)
            : new SearchIndexClient(settings.SearchEndpoint, new AzureKeyCredential(settings.SearchKey));
        });
    }
}
