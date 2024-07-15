#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Azure.Identity;
using ChatApp.Server.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;

namespace ChatApp.Server.Services;

public class ChatCompletionService : IChatCompletionService
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

        //var deployedModelName = config["AZURE_OPENAI_CHATGPT_DEPLOYMENT"];
        //ArgumentNullException.ThrowIfNullOrWhiteSpace(deployedModelName);
        //var embeddingModelName = config["AZURE_OPENAI_EMBEDDING_DEPLOYMENT"];
        //if (!string.IsNullOrEmpty(embeddingModelName))
        //{
        //    var endpoint = config["AZURE_OPENAI_ENDPOINT"];
        //    ArgumentNullException.ThrowIfNullOrWhiteSpace(endpoint);
        //    builder = builder.AddAzureOpenAITextEmbeddingGeneration(embeddingModelName, endpoint, defaultAzureCreds);
        //    builder = builder.AddAzureOpenAIChatCompletion(deployedModelName, endpoint, defaultAzureCreds);
        //}

        _kernel = builder.Build();
    }

    public async Task<ChatCompletion> CompleteChat(string prompt)
    {
        throw new NotImplementedException();
        //var sysmessage = @"You are a helpful assistant.";
        //var history = new ChatHistory(sysmessage);
        //history.AddUserMessage(prompt);

        //var response = await _kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(history, _promptSettings);
        //var item = response.Items[0];
    }
}

#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
