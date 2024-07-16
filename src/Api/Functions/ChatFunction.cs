using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace API
{
    public class ChatFunction
    {
        private readonly ILogger<ChatFunction> _logger;

        public ChatFunction(ILogger<ChatFunction> logger)
        {
            _logger = logger;
        }

        [Function("ChatFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(requestBody);
            var query = data?["query"];

            _logger.LogInformation("Processing FHIR request");
            var modelId = Environment.GetEnvironmentVariable("AzureOpenAiChatDeployedModel")!;
            var endpoint = Environment.GetEnvironmentVariable("AzureOpenAiEndpoint")!;
            var apiKey = Environment.GetEnvironmentVariable("AzureOpenAiKey")!;
            
            var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);
            //Builder needs to add the HttpClient service to the service collection for the plugin to use
            builder.Services.AddHttpClient();
            builder.Plugins.AddFromType<QueryFhirPlugin>("FHIR");

            Kernel kernel = builder.Build();
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            


            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var history = new ChatHistory(
            """
                You are a FHIR query generation tool.  Based on the user's input, you will generate a FHIR query to retrieve the desired clinical data.
            """
            );

            history.AddUserMessage(query!);

            var result = await chatCompletionService.GetChatMessageContentAsync(
               history,
               executionSettings: openAIPromptExecutionSettings,
               kernel: kernel);

            return new OkObjectResult(result);
        }
    }
}
