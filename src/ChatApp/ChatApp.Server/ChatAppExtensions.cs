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

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var searchClient = string.IsNullOrEmpty(config["AZURE_SEARCH_KEY"]) ?
                new SearchClient(
                    new Uri(config["AZURE_SEARCH_ENDPOINT"] ?? "https://search"),
                    config["AZURE_SEARCH_INDEX"],
                    defaultAzureCreds) :
                new SearchClient(
                    new Uri(config["AZURE_SEARCH_ENDPOINT"] ?? "https://search"),
                    config["AZURE_SEARCH_INDEX"],
                    new AzureKeyCredential(config["AZURE_SEARCH_KEY"]!));

            return new AzureSearchService(searchClient);
        });

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
