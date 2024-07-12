using ChatApp.Server.Services;
using Microsoft.SemanticKernel;

namespace ChatApp.Server;

internal static class OpenAiExtensions
{
    internal static void AddOpenAiServices(this IServiceCollection services)
    {
        services.AddSingleton<ChatCompletionService>();
    }
}
