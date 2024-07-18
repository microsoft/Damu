using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using ChatApp.Server.Models;
using ChatApp.Server.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace ChatApp.Server;

internal static class ChatAppExtensions
{
    internal static void AddOptions(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AzureAdOptions>(config.GetSection(nameof(AzureAdOptions)));
        services.Configure<AISearchOptions>(config.GetSection(nameof(AISearchOptions)));
        services.Configure<OpenAIOptions>(config.GetSection(nameof(OpenAIOptions)));
        services.Configure<CosmosOptions>(config.GetSection(nameof(CosmosOptions)));
        services.Configure<StorageOptions>(config.GetSection(nameof(StorageOptions)));
        services.Configure<FhirOptions>(config.GetSection(nameof(FhirOptions)));

        services.Configure<FrontendSettings>(config.GetSection(nameof(FrontendSettings)));
    }

    internal static void AddChatAppServices(this IServiceCollection services, IConfiguration config)
    {
        var defaultAzureCreds = new DefaultAzureCredential();

        services.AddSingleton<ChatCompletionService>();

        services.AddSingleton(services =>
        {
            var options = services.GetRequiredService<IOptions<AISearchOptions>>().Value ?? throw new Exception($"{nameof(AISearchOptions)} is rquired in settings.");

            if (string.IsNullOrWhiteSpace(options?.ApiKey))
            {
                var adOptions = services.GetRequiredService<IOptions<AzureAdOptions>>().Value;

                var defaultCreds = string.IsNullOrWhiteSpace(adOptions?.TenantId)
                ? new DefaultAzureCredential()
                    : new DefaultAzureCredential(
                        new DefaultAzureCredentialOptions
                        {
                            TenantId = adOptions.TenantId
                        });

                return new SearchClient(
                        new Uri(options!.Endpoint),
                        options.IndexName,
                        defaultCreds);
            }

            return new SearchClient(
                    new Uri(options.Endpoint),
                    options.IndexName,
                    new AzureKeyCredential(options.ApiKey));
        });

        services.AddSingleton<AzureSearchService>();

        var isChatEnabled = bool.TryParse(config["ENABLE_CHAT_HISTORY"], out var result) && result;

        if (isChatEnabled)
        {
            services.AddSingleton(services =>
            {
                var options = services.GetRequiredService<IOptions<CosmosOptions>>().Value ?? throw new Exception($"{nameof(CosmosOptions)} is rquired in settings.");

                if (string.IsNullOrEmpty(options?.CosmosKey))
                {
                    var adOptions = services.GetRequiredService<IOptions<AzureAdOptions>>().Value;

                    var defaultCreds = string.IsNullOrWhiteSpace(adOptions?.TenantId)
                    ? new DefaultAzureCredential()
                        : new DefaultAzureCredential(
                            new DefaultAzureCredentialOptions
                            {
                                TenantId = adOptions.TenantId
                            });
                    return new CosmosClient(options!.CosmosEndpoint, defaultCreds);
                }

                return new CosmosClient(options.CosmosEndpoint, new AzureKeyCredential(options.CosmosKey));
            });

            services.AddSingleton<CosmosConversationService>();
        }

        services.AddSingleton(services =>
        {
            var options = config.GetSection(nameof(StorageOptions)).Get<StorageOptions>() ?? throw new Exception($"{nameof(StorageOptions)} is rquired in settings."); ;

            var storageEndpoint = options?.BlobStorageEndpoint;

            storageEndpoint = storageEndpoint?.Substring(0, storageEndpoint.LastIndexOf('/'));
            var containerUri = new Uri($"{storageEndpoint}/{options?.BlobStorageContainerName}");

            if (string.IsNullOrEmpty(options?.BlobStorageConnectionString))
            {
                var adOptions = services.GetRequiredService<IOptions<AzureAdOptions>>().Value;

                var defaultCreds = string.IsNullOrWhiteSpace(adOptions?.TenantId)
                ? new DefaultAzureCredential()
                    : new DefaultAzureCredential(
                        new DefaultAzureCredentialOptions
                        {
                            TenantId = adOptions.TenantId
                        });

                return new BlobContainerClient(containerUri, defaultCreds);
            }

            return new BlobContainerClient(options?.BlobStorageConnectionString, options?.BlobStorageContainerName);
        });

        services.AddSingleton<NoteService>();
    }
}
