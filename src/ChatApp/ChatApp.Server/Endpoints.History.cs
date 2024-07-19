
using ChatApp.Server.Models;
using ChatApp.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Text.Json;

namespace ChatApp.Server;

public static partial class Endpoints
{
    // can we simplify this to just use SK models? https://github.com/microsoft/semantic-kernel/discussions/5815

    public static WebApplication MapHistoryEndpoints(this WebApplication app)
    {
        app.MapGet("/history/ensure", GetEnsureHistoryAsync);
        app.MapPost("/history/clear", ClearHistoryAsync);
        app.MapDelete("/history/delete_all", DeleteAllHistoryAsync);
        app.MapPost("/history/rename", RenameHistoryAsync);
        app.MapDelete("/history/delete", DeleteHistory);
        app.MapPost("/history/message_feedback", MessageFeedbackAsync);
        app.MapPost("/history/update", UpdateHistoryAsync);
        app.MapGet("/history/list", ListHistoryAsync);
        app.MapPost("/history/read", ReadHistoryAsync);
        // Not implemented
        app.MapPost("/history/generate", GenerateHistoryAsync);

        return app;
    }

    private static async Task<IResult> GenerateHistoryAsync(
        HttpContext context,
        [FromBody] ConversationRequest conversation,
        [FromServices] CosmosConversationService history,
        [FromServices] ChatCompletionService chat,
        [FromServices] AzureSearchService search)
    {
        var user = GetUser(context);
        string conversationId = conversation.Id;

        if (user == null)
            return Results.Unauthorized();

        if (conversation == null)
            return Results.BadRequest();

        // --- See if this is an existing conversation, otherwise create a new one ---
        var historyMetadata = new Dictionary<string, string>();
        // if conversationId is empty that means it is a new conversation
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            var title = await chat.GenerateTitleAsync(conversation.Messages);

            // should we persist user message here too?
            var result = await history.CreateConversationAsync(user.UserPrincipalId, title);
            conversationId = result.Id;

            historyMetadata.Add("title", result.Title);
            historyMetadata.Add("date", result.CreatedAt.ToString());
        }
        historyMetadata.Add("conversation_id", conversationId);

        // Format the incoming message object in the "chat/completions" messages format
        // then write it to the conversation history in cosmos
        var userMessage = conversation.Messages.LastOrDefault(m => m.Role.Equals(AuthorRole.User.ToString(), StringComparison.OrdinalIgnoreCase));
        if (userMessage == null)
            return Results.BadRequest("No user messages found");

        _ = await history.CreateMessageAsync(userMessage.Id, conversationId, user.UserPrincipalId, userMessage);

        // --- Do the RAG search ---
        // filter out any existing tool messages (search results)
        conversation.Messages = conversation.Messages.Where(m => !m.Role.Equals(AuthorRole.Tool.ToString(), StringComparison.OrdinalIgnoreCase)).ToList();
        // do the search here
        var searchResults = await search.QueryDocumentsAsync(userMessage.Content);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var toolMsg = new Message
        {
            Id = Guid.NewGuid().ToString(),
            Role = AuthorRole.Tool.ToString().ToLower(),
            Date = DateTime.UtcNow,
            Content = JsonSerializer.Serialize<ToolContentResponse>(searchResults, options)
        };
        // add search results to the conversation
        conversation.Messages.Add(toolMsg);

        // --- Do the chat completion ---

        var completionResult = await chat.CompleteChat([.. conversation.Messages]);
        completionResult.HistoryMetadata = historyMetadata;

        //    except Exception as e:
        //        logging.exception("Exception in /history/generate")
        //        return jsonify({ "error": str(e)}), 500
        return Results.Ok(completionResult);
    }

    private static async Task<IResult> GetEnsureHistoryAsync(HttpContext httpContext, [FromServices] CosmosConversationService history)
    {
        var (cosmosIsConfigured, _) = await history.EnsureAsync();

        return cosmosIsConfigured
            ? Results.Ok(JsonSerializer.Deserialize<object>(@"{ ""converation"": ""CosmosDB is configured and working""}"))
            : Results.NotFound(JsonSerializer.Deserialize<object>(@"{ ""error"": ""CosmosDB is not configured""}"));
    }

    private static async Task<IResult> ClearHistoryAsync(
        HttpContext context,
        Conversation conversation,
        [FromServices] CosmosConversationService conversationService)
    {
        // get the user id from the request headers
        var user = GetUser(context);

        if (user == null)
            return Results.Unauthorized();


        if (string.IsNullOrWhiteSpace(conversation?.Id))
            return Results.BadRequest("conversation_id is required");

        // todo: do conversations and messages need to be deleted separately
        // or can the conversation delete in the service encompass the messages
        // delete the conversation messages from cosmos
        var deleted = await conversationService.DeleteConversationAsync(user.UserPrincipalId, conversation.Id);

        return deleted
            ? Results.Ok(new { message = "Successfully deleted messages in conversation", conversation_id = conversation.Id })
            : Results.NotFound();
    }

    private static async Task<IResult> DeleteAllHistoryAsync(HttpContext context, [FromServices] CosmosConversationService conversationService)
    {
        // get the user id from the request headers
        var user = GetUser(context);

        if (user == null)
            return Results.Unauthorized();

        await conversationService.DeleteConversationsAsync(user.UserPrincipalId);

        return Results.Ok(new
        {
            message = $"Successfully deleted conversation and messages for user {user.UserPrincipalId}"
        });
    }

    private static async Task<IResult> RenameHistoryAsync(
        HttpContext context,
        [FromBody] Conversation conversation,
        [FromServices] CosmosConversationService conversationService)
    {
        var user = GetUser(context);

        if (user == null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(conversation?.Id))
            return Results.BadRequest("conversation_id is required");

        var updatedConversation = await conversationService.UpdateConversationAsync(user.UserPrincipalId, conversation);

        if (updatedConversation == null)
            return Results.NotFound(new { error = $"Conversation {conversation.Id} was not found" });

        return Results.Ok(updatedConversation);
    }

    private static async Task<IResult> DeleteHistory(
        HttpContext context,
        [FromBody] Conversation conversation,
        [FromServices] CosmosConversationService conversationService)
    {
        var user = GetUser(context);

        if (user == null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(conversation?.Id))
            return Results.BadRequest("conversation_id is required");

        _ = await conversationService.DeleteConversationAsync(user.UserPrincipalId, conversation.Id);

        var response = new
        {
            message = "Successfully deleted conversation and messages",
            conversation_id = conversation.Id
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> MessageFeedbackAsync(
        HttpContext context,
        [FromBody] HistoryMessage message,
        [FromServices] CosmosConversationService conversationService)
    {
        var user = GetUser(context);

        if (user == null)
            return Results.Unauthorized();

        var updatedMessage = await conversationService.UpdateMessageFeedbackAsync(user.UserPrincipalId, message.Id, message.Feedback);

        return updatedMessage != null
            ? Results.Ok(updatedMessage)
            : Results.NotFound();
    }

    private static async Task<IResult> ListHistoryAsync(
        HttpContext context,
        [FromServices] ChatCompletionService chat,
        [FromServices] CosmosConversationService history,
        int offset)
    {
        var user = GetUser(context);

        if (user == null)
            return Results.Unauthorized();

        var convosAsync = history.GetConversationsAsync(user.UserPrincipalId, 25, offset: offset);

        var conversations = await convosAsync.ToListAsync();

        if (conversations == null || conversations.Count == 0)
            return Results.NotFound(new { error = $"No conversations for {user.UserPrincipalId} were found" });

        return Results.Ok(conversations);

    }

    private static async Task<IResult> ReadHistoryAsync(
        HttpContext context,
        [FromBody] Conversation conversation,
        [FromServices] CosmosConversationService history)
    {
        var user = GetUser(context);

        if (user == null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(conversation?.Id))
            return Results.BadRequest("conversation_id is required");

        var dbConversation = await history.GetConversationAsync(user.UserPrincipalId, conversation.Id);

        if (dbConversation == null)
        {
            return Results.NotFound(new { ErrorEventArgs = $"Conversation {conversation.Id} was not found. It either does not exist or the logged in user does not have access to it." });
        }

        var messages = await history.GetMessagesAsync(user.UserPrincipalId, dbConversation.Id).ToListAsync();

        var results = messages.Select(m => new Message
        {
            Id = m.Id,
            Role = m.Role,
            Content = m.Content,
            Date = m.CreatedAt
        });

        return Results.Ok(new { conversation_id = dbConversation.Id, messages = results });
    }

    private static async Task<IResult> UpdateHistoryAsync(
        HttpContext context,
        [FromBody] Conversation conversation,
        [FromServices] CosmosConversationService history)
    {
        var user = GetUser(context);

        if (user == null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(conversation?.Id))
            return Results.BadRequest("conversation_id is required");

        if (conversation.Messages.Count > 0 && conversation.Messages[^1].Role.Equals(AuthorRole.Assistant.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            if (conversation.Messages.Count > 1 && conversation.Messages[^2].Role.Equals(AuthorRole.Tool.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                // write the tool message first                
                await history.CreateMessageAsync(Guid.NewGuid().ToString(), conversation.Id, user.UserPrincipalId, conversation.Messages[^2]);
            }

            // write the assistant message
            await history.CreateMessageAsync(Guid.NewGuid().ToString(), conversation.Id, user.UserPrincipalId, conversation.Messages[^1]);
        }
        else
        {
            return Results.BadRequest("No bot messages found");
        }

        return Results.Ok(new { success = true });
    }



    #region Helpers

    private static EasyAuthUser? GetUser(HttpContext context)
    {
        // return a default user if we're in development mode otherwise return null
        if (!context.Request.Headers.TryGetValue("X-Ms-Client-Principal-Id", out var principalId))
            return !string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase)
                ? new() // todo: should we also use a test static json of Easy Auth headers to be injected into HttpContext in development mode?
                : null;

        return new EasyAuthUser
        {
            UserPrincipalId = principalId.FirstOrDefault() ?? string.Empty,
            Username = context.Request.Headers["X-Ms-Client-Principal-Name"].FirstOrDefault() ?? string.Empty,
            AuthProvider = context.Request.Headers["X-Ms-Client-Principal-Idp"].FirstOrDefault() ?? string.Empty,
            AuthToken = context.Request.Headers["X-Ms-Token-Aad-Id-Token"].FirstOrDefault() ?? string.Empty,
            ClientPrincipalB64 = context.Request.Headers["X-Ms-Client-Principal"].FirstOrDefault() ?? string.Empty,
            AadIdToken = context.Request.Headers["X-Ms-Token-Aad-Id-Token"].FirstOrDefault() ?? string.Empty
        };
    }

    #endregion
}
