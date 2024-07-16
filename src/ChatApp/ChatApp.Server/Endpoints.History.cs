
using ChatApp.Server.Models;
using ChatApp.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;
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

        // Not implemented
        app.MapPost("/history/generate", GenerateHistory);
        app.MapPost("/history/update", UpdateHistory);
        app.MapGet("/history/list", ListHistory);
        app.MapPost("/history/read", ReadHistory);

        return app;
    }

    private static async Task<IResult> GetEnsureHistoryAsync(HttpContext httpContext, [FromServices] CosmosConversationService conversationService)
    {
        // todo: refactor the UI so that this is can be refactored to make any amount of sense...
        var (cosmosIsConfigured, _) = await conversationService.EnsureAsync();

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

    private static async Task<IResult> ListHistory(
        HttpContext context,
        [FromServices] IChatService chat,
        [FromServices] CosmosConversationService history,
        int offset)
    {
        var user = GetUser(context);

        if (user == null)
            return Results.Unauthorized();

        var convosAsync = history.GetConversationsAsync(user.UserPrincipalId, 25, offset: offset);
        var conversations = await convosAsync.ToListAsync();

        if (conversations == null || conversations.Count != 0)
            return Results.NotFound(new { error = $"No conversations for {user.UserPrincipalId} were found" });

        return Results.Ok(conversations);
    }

    private static async Task<IResult> ReadHistory(
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

    #region NotImplemented

    private static async Task UpdateHistory(HttpContext context)
    {
        //authenticated_user = get_authenticated_user_details(request_headers = request.headers)
        //user_id = authenticated_user["user_principal_id"]

        //## check request for conversation_id
        //request_json = await request.get_json()
        //conversation_id = request_json.get("conversation_id", None)

        //try:
        //    # make sure cosmos is configured
        //    cosmos_conversation_client = init_cosmosdb_client()
        //    if not cosmos_conversation_client:
        //        raise Exception("CosmosDB is not configured or not working")

        //    # check for the conversation_id, if the conversation is not set, we will create a new one
        //    if not conversation_id:
        //        raise Exception("No conversation_id found")

        //    ## Format the incoming message object in the "chat/completions" messages format
        //    ## then write it to the conversation history in cosmos
        //    messages = request_json["messages"]
        //    if len(messages) > 0 and messages[-1]["role"] == "assistant":
        //        if len(messages) > 1 and messages[-2].get("role", None) == "tool":
        //            # write the tool message first
        //            await cosmos_conversation_client.create_message(
        //                uuid = str(uuid.uuid4()),
        //                conversation_id = conversation_id,
        //                user_id = user_id,
        //                input_message = messages[-2],
        //            )
        //        # write the assistant message
        //        await cosmos_conversation_client.create_message(
        //            uuid = messages[-1]["id"],
        //            conversation_id = conversation_id,
        //            user_id = user_id,
        //            input_message = messages[-1],
        //        )
        //    else:
        //        raise Exception("No bot messages found")

        //    # Submit request to Chat Completions for response
        //    await cosmos_conversation_client.cosmosdb_client.close()
        //    response = { "success": True}
        //    return jsonify(response), 200

        //except Exception as e:
        //    logging.exception("Exception in /history/update")
        //    return jsonify({ "error": str(e)}), 500
        await Task.Delay(0);
        throw new NotImplementedException();
    }

    private static async Task<IResult> GenerateHistory(
        HttpContext context, 
        [FromBody] Conversation conversation,
        [FromServices] CosmosConversationService conversationService, 
        [FromServices] ChatCompletionService chatCompletionService)
    {
        var user = GetUser(context);

        if (user == null)
            return Results.Unauthorized();

        if (conversation == null)
            return Results.BadRequest();

        if (string.IsNullOrWhiteSpace(conversation.Id))
        {
            var title = await chatCompletionService.GenerateTitleAsync(conversation.Messages);

            // should we persist user message here too?
            _ = await conversationService.CreateConversationAsync(user.UserPrincipalId, title);
        }

        // Format the incoming message object in the "chat/completions" messages format
        // then write it to the conversation history in cosmos
        if (conversation.Messages.Count == 0 || !conversation.Messages[^1].Role.Equals(AuthorRole.User.ToString(), StringComparison.InvariantCultureIgnoreCase)) // move role format to enum?
            return Results.BadRequest("No user messages found");

        //var result = await chatCompletionService.AlternativeCompleteChat(conversation)

        //var message = new Message(conversation_id, user_id, (Dictionary<string, object>)messages[0]);

        // todo: build out history and send to conversations endpoint

        //        # Submit request to Chat Completions for response
        //        request_body = await request.get_json()
        //        history_metadata["conversation_id"] = conversation_id
        //        request_body["history_metadata"] = history_metadata
        //        return await conversation_internal(request_body, request.headers)

        //    except Exception as e:
        //        logging.exception("Exception in /history/generate")
        //        return jsonify({ "error": str(e)}), 500

        await Task.Delay(0);
        throw new NotImplementedException();
    }
    #endregion

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
