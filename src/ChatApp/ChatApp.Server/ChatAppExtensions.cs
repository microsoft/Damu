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
    internal static void AddChatAppServices(this IServiceCollection services, IConfiguration config)
    {
        var defaultAzureCreds = new DefaultAzureCredential();

        services.AddSingleton<ChatCompletionService>();

        services.AddSingleton(services =>
        {
            var options = services.GetRequiredService<IOptions<AISearchOptions>>().Value;

            return string.IsNullOrEmpty(options.ApiKey) ?
                new SearchClient(
                    new Uri(options.Endpoint),
                    options.IndexName,
                    new DefaultAzureCredential()) :
                new SearchClient(
                    new Uri(options.Endpoint),
                    options.IndexName,
                    new AzureKeyCredential(options.ApiKey));
        });

        services.AddTransient<AzureSearchService>();

        var isChatEnabled = bool.TryParse(config["ENABLE_CHAT_HISTORY"], out var result) && result;

        if (isChatEnabled)
        {
            services.AddTransient(services =>
            {
                var options = services.GetRequiredService<IOptions<CosmosOptions>>().Value;

                return string.IsNullOrWhiteSpace(options.CosmosKey)
                ? new CosmosClient(options.CosmosEndpoint, new DefaultAzureCredential())
                : new CosmosClient(options.CosmosEndpoint, new AzureKeyCredential(options.CosmosKey));
            });
            services.AddTransient<CosmosConversationService>();
        }

        services.AddTransient(sp =>
        {
            var storageOptions = config.GetSection(nameof(StorageOptions)).Get<StorageOptions>();

            var storageEndpoint = storageOptions?.BlobStorageEndpoint;

            storageEndpoint = storageEndpoint?.Substring(0, storageEndpoint.LastIndexOf('/'));
            var containerUri = new Uri($"{storageEndpoint}/{storageOptions?.BlobStorageContainerName}");

            return string.IsNullOrWhiteSpace(storageOptions?.BlobStorageConnectionString)
                ? new BlobContainerClient(containerUri, new DefaultAzureCredential())
                : new BlobContainerClient(storageOptions?.BlobStorageConnectionString, storageOptions?.BlobStorageContainerName);
        });

        services.AddTransient<NoteService>();
    }
}
