#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Azure.Identity;
using ChatApp.Server.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ChatApp.Server.Services;

public class ChatCompletionService
{
    private readonly Kernel _kernel;
    private readonly PromptExecutionSettings _promptSettings;

    public ChatCompletionService(IConfiguration config, AzureSearchService searchService)
    {
        var defaultAzureCreds = new DefaultAzureCredential();

        _promptSettings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 1024,
            Temperature = 0.5,
            StopSequences = [],
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
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
        builder.Plugins.AddFromObject(searchService, "SearchNotes");
        _kernel = builder.Build();
    }

    public async Task<ChatCompletion> CompleteChat(string prompt)
    {
        var msg = new Message
        {
            Id = "0000",
            Role = AuthorRole.User.ToString(),
            Content = prompt,
            Date = DateTime.UtcNow
        };

        return await CompleteChat([msg]);
    }

    public async Task<ChatCompletion> CompleteChat(Message[] messages)
    {
        var sysmessage = @"You are an agent helping a medical researcher find medical notes that fit criteria and supplement with additional data, 
                using the plugins available to you. Your response should return a count of notes found and a sample list (maximum 10) of patient's names and the corresponding MRNs in a table format.
                If the plugins do not return data based on the question using the provided plugins, respond that you found no information. Do not use general knowledge to respond.
                Sample Answer:
                (2) notes found:
                Patient Name	|	MRN	
                John Johnson 	|	1234567
                Peter Peterson	| 	7654321
                User Question:
                {{query}}";
        var history = new ChatHistory(sysmessage);

        messages.Where(m => m.Role.Equals(AuthorRole.User.ToString(), StringComparison.InvariantCultureIgnoreCase))
            .ToList()
            .ForEach(m => history.AddUserMessage(m.Content));
        
        var response = await _kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(history, _promptSettings, _kernel);
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
                    Role = AuthorRole.Assistant.ToString().ToLower(),
                    Content = item.ToString()!,
                    Date = DateTime.UtcNow
                }).ToList()
            }]
        };

        return result;
    }



    public async Task<ChatCompletion> AlternativeCompleteChat(IEnumerable<Message> messages)
    {
        var history = new ChatHistory(messages.Select(m => m.ToChatMessageContent()).ToList());

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
                    Role = AuthorRole.Assistant.ToString(),
                    Content = item.ToString()!,
                    Date = DateTime.UtcNow
                }).ToList()
            }]
        };

        return result;
    }

    public async Task<string> GenerateTitleAsync(List<Message> messages)
    {
        Console.WriteLine(messages);
        // "Summarize the conversation so far into a 4-word or less title. Do not use any quotation marks or punctuation. Do not include any other commentary or description."
        
        // is there an OOB summary plugin or do we right our own against a history...?
        await Task.Delay(0);
        throw new NotImplementedException();
    }
}

#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
