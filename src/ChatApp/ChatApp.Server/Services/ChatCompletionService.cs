#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
using Azure.Identity;
using ChatApp.Server.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace ChatApp.Server.Services;

public class ChatCompletionService
{
    private readonly Kernel _kernel;
    private readonly PromptExecutionSettings _promptSettings;
    private readonly string _promptDirectory;

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

        _promptDirectory = Path.Combine(Directory.GetCurrentDirectory(),"Plugins");

        builder.Plugins.AddFromPromptDirectory(_promptDirectory);

        //builder.Plugins.AddFromObject(searchService, "SearchNotes");
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
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        string documentContents = string.Empty;
        if (messages.Any(m => m.Role.Equals(AuthorRole.Tool.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            // parse out the document contents
            var toolContent = JsonSerializer.Deserialize<ToolContentResponse>(
                messages.First(m => m.Role.Equals(AuthorRole.Tool.ToString(), StringComparison.OrdinalIgnoreCase)).Content, options);
            documentContents = string.Join("\r", toolContent.Citations.Select(c => $"{c.Title}:{c.Content}:{c.PatientName}:{c.MRN}"));
        }
        else
        {
            documentContents = "no source available.";
        }

        var sysmessage = $$$"""
                ## Notes ##
                {{{documentContents}}}
                ## End Notes ##
                You are an agent helping to analyze the provided medical notes about patient interactions, and supplement with additional data using plugin functions available to you. 
                Respond with a count of notes used to the question and the list of note data in a table formatted like the sample answer.
                Include data in the table asked for by the user, only if you can explicitly find it in the note or data retrieved using plugin functions available. 
                If you do not have data available to meet the user's request, include only what you have, with "not found" listed for missing data. Never make up data.
                If the data set is large, display max 10 indicating it is just a sample. 
                Include source citations, which must be in the format [doc1], [doc2], etc.
                Sample Answer:
                (2) notes found:\n
                Patient Name	|	MRN	    | Citation |\n
                John Johnson 	|	1234567 | [doc1]   |\n
                Peter Peterson	| 	7654321 | [doc2]   |
                """;
        var history = new ChatHistory(sysmessage);

        // filter out 'tool' messages
        messages.Where(m => !m.Role.Equals(AuthorRole.Tool.ToString(), StringComparison.OrdinalIgnoreCase))
            .ToList()
            .ForEach(m => history.AddUserMessage(m.Content));

        var response = await _kernel.GetRequiredService<IChatCompletionService>().GetChatMessageContentAsync(history, _promptSettings, _kernel);
        // add assistant response message to history and return chatcompletion

        // append response messages to messages array
        var responseMessages = messages.ToList();

        response.Items.ToList().ForEach(item => responseMessages.Add(new Message
        {
            Id = Guid.NewGuid().ToString(),
            Role = AuthorRole.Assistant.ToString().ToLower(),
            Content = item.ToString()!,
            Date = DateTime.UtcNow
        }));

        var result = new ChatCompletion
        {
            Id = Guid.NewGuid().ToString(),
            ApimRequestId = Guid.NewGuid().ToString(),
            Model = response.ModelId!,
            Created = DateTime.UtcNow,
            Choices = [new() {
                Messages = [.. responseMessages]
            }]
        };

        return result;
    }

    public async Task<string> GenerateTitleAsync(List<Message> messages)
    {
        // Create a conversation string from the messages
        string conversationText = string.Join(" ", messages.Select(m => m.Role + " " + m.Content));

        // Load prompt yaml
        var promptYaml = File.ReadAllText(Path.Combine(_promptDirectory, "TextPlugin", "SummarizeConversation.yaml"));
        var function = _kernel.CreateFunctionFromPromptYaml(promptYaml);

        // Invoke the function against the conversation text
        var result = await _kernel.InvokeAsync(function, new() { { "history", conversationText } });

        string completion = result.ToString()!;

        return completion;
    }
}

#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
