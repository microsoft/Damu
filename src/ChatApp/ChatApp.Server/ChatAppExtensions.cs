﻿using Azure;
using Azure.Identity;
using Azure.Search.Documents;
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
            var searchClient = new SearchClient(
                new Uri(config["AZURE_SEARCH_ENDPOINT"] ?? "https://search"),
                config["AZURE_SEARCH_INDEX"],
                defaultAzureCreds);

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
    }
}
