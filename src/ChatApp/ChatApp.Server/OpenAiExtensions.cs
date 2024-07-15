using Azure.Identity;
using Azure.Search.Documents;
using ChatApp.Server.Services;

namespace ChatApp.Server;

internal static class OpenAiExtensions
{
    internal static void AddOpenAiServices(this IServiceCollection services)
    {
        var defaultAzureCreds = new DefaultAzureCredential();

        services.AddSingleton<IChatCompletionService, OpenAIChatCompletionService>();

        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var searchClient = new SearchClient(
                new Uri(config["AZURE_SEARCH_ENDPOINT"] ?? "https://search"),
                config["AZURE_SEARCH_INDEX"],
                defaultAzureCreds);

            return new AzureSearchService(searchClient);
        });
    }
}
