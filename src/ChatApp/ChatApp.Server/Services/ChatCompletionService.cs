#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Azure.Identity;
using ChatApp.Server.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ChatApp.Server.Services;

public class ChatCompletionService : IChatService
{
    private Kernel _kernel;
    private PromptExecutionSettings _promptSettings;

    public ChatCompletionService(IConfiguration config)
    {
        var defaultAzureCreds = new DefaultAzureCredential();

        _promptSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1024,
            Temperature = 0.5,
            StopSequences = [],
        };

        var builder = Kernel.CreateBuilder();

        var deployedModelName = config["AZURE_OPENAI_CHATGPT_DEPLOYMENT"];
        ArgumentNullException.ThrowIfNullOrWhiteSpace(deployedModelName);
        var embeddingModelName = config["AZURE_OPENAI_EMBEDDING_DEPLOYMENT"];
        if (!string.IsNullOrEmpty(embeddingModelName))
        {
            var endpoint = config["AZURE_OPENAI_ENDPOINT"];
            ArgumentNullException.ThrowIfNullOrWhiteSpace(endpoint);
            builder = builder.AddAzureOpenAITextEmbeddingGeneration(embeddingModelName, endpoint, defaultAzureCreds);
            builder = builder.AddAzureOpenAIChatCompletion(deployedModelName, endpoint, defaultAzureCreds);
        }

        _kernel = builder.Build();
    }

    public async Task<ChatCompletion> CompleteChat(string prompt)
    {
        var msg = new Message
        {
            Id = "0000",
            Role = "user",
            Content = prompt,
            Date = DateTime.UtcNow
        };

        return await CompleteChat([msg]);
    }

    public async Task<ChatCompletion> CompleteChat(Message[] messages)
    {
        var sysmessage = @"You are a helpful assistant.";
        var history = new ChatHistory(sysmessage);
        foreach (var message in messages)
        {
            history.AddUserMessage(message.Content);
        }

        var response = await _kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(history, _promptSettings);
        var result = new ChatCompletion
        {
            Id = Guid.NewGuid().ToString(),
            ApimRequestId = Guid.NewGuid().ToString(),
            Model = response.ModelId!,
            Created = DateTime.UtcNow,
            Choices = [new() {
                Messages = response.Items.Select(item => new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    Role = "assistant",
                    Content = item.ToString()!,
                    Date = DateTime.UtcNow
                }).ToList()
            }]
        };
        return result;
    }
}

#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
