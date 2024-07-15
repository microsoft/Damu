using Azure.AI.OpenAI;
using Azure.Identity;
using ChatApp.Server.Models;
using OpenAI.Chat;

namespace ChatApp.Server.Services;

public class OpenAIChatCompletionService : IChatCompletionService
{
    private ChatClient _chatClient;

    public OpenAIChatCompletionService(IConfiguration config)
    {
        var openAIClient = new AzureOpenAIClient(new Uri(config["AZURE_OPENAI_ENDPOINT"] ?? "https://openai"), new DefaultAzureCredential());
        _chatClient = openAIClient.GetChatClient(config["AZURE_OPENAI_CHATGPT_DEPLOYMENT"]);
    }

    public async Task<ChatCompletion> CompleteChat(string prompt)
    {
        var completion = await _chatClient.CompleteChatAsync([
            new SystemChatMessage("You are a helpful assistant."),
            new UserChatMessage(prompt)
            ]);
        return completion.Value;
    }
}
