
using ChatApp.Server.Models;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ChatApp.Server;

public static partial class Endpoints
{
    public static WebApplication MapHistoryEndpoints(this WebApplication app)
    {
        app.MapGroup("history");

        app.MapGet("ensure", GetEnsureHistory);

        // Not implemented
        app.MapPost("generate", GenerateHistory);
        app.MapPost("update", UpdateHistory);
        app.MapPost("message_feedback", MessageFeedback);
        app.MapDelete("delete", DeleteHistory);
        app.MapGet("list", ListHistory);
        app.MapPost("read", ReadHistory);
        app.MapPost("rename", RenameHistory);
        app.MapDelete("delete_all", DeleteAllHistory);
        app.MapPost("clear", ClearHistory);

        return app;
    }

    private static IResult GetEnsureHistory(HttpContext httpContext)
    {
        //if not app_settings.chat_history:
        //    return jsonify({ "error": "CosmosDB is not configured"}), 404

        //try:
        //    cosmos_conversation_client = init_cosmosdb_client()
        //    success, err = await cosmos_conversation_client.ensure()
        //    if not cosmos_conversation_client or not success:
        //        if err:
        //            return jsonify({ "error": err}), 422
        //        return jsonify({ "error": "CosmosDB is not configured or not working"}), 500

        //    await cosmos_conversation_client.cosmosdb_client.close()
        //    return jsonify({ "message": "CosmosDB is configured and working"}), 200
        //except Exception as e:
        //    logging.exception("Exception in /history/ensure")
        //    cosmos_exception = str(e)
        //    if "Invalid credentials" in cosmos_exception:
        //    return jsonify({ "error": cosmos_exception}), 401
        //    elif "Invalid CosmosDB database name" in cosmos_exception:
        //    return (
        //        jsonify(
        //                {
        //        "error": f"{cosmos_exception} {app_settings.chat_history.database} for account {app_settings.chat_history.account}"
        //                }
        //            ),
        //            422,
        //        )
        //    elif "Invalid CosmosDB container name" in cosmos_exception:
        //    return (
        //        jsonify(
        //                {
        //        "error": f"{cosmos_exception}: {app_settings.chat_history.conversations_container}"
        //                }
        //            ),
        //            422,
        //        )
        //    else:
        //        return jsonify({ "error": "CosmosDB is not working"}), 500
        string response = @"{ ""error"": ""CosmosDB is not configured""}";
        return Results.NotFound(System.Text.Json.JsonSerializer.Deserialize<object>(response));
    }

    #region NotImplemented
    private static async Task ClearHistory(HttpContext context)
    {
        //## get the user id from the request headers
        //authenticated_user = get_authenticated_user_details(request_headers = request.headers)
        //user_id = authenticated_user["user_principal_id"]

        //## check request for conversation_id
        //request_json = await request.get_json()
        //conversation_id = request_json.get("conversation_id", None)

        //try:
        //    if not conversation_id:
        //        return jsonify({ "error": "conversation_id is required"}), 400

        //    ## make sure cosmos is configured
        //    cosmos_conversation_client = init_cosmosdb_client()
        //    if not cosmos_conversation_client:
        //        raise Exception("CosmosDB is not configured or not working")

        //    ## delete the conversation messages from cosmos
        //    deleted_messages = await cosmos_conversation_client.delete_messages(
        //        conversation_id, user_id
        //    )

        //    return (
        //        jsonify(
        //            {
        //        "message": "Successfully deleted messages in conversation",
        //                "conversation_id": conversation_id,
        //            }
        //        ),
        //        200,
        //    )
        //except Exception as e:
        //    logging.exception("Exception in /history/clear_messages")
        //    return jsonify({ "error": str(e)}), 500
        await Task.Delay(0);
        throw new NotImplementedException();
    }

    private static async Task DeleteAllHistory(HttpContext context)
    {
        //## get the user id from the request headers
        //authenticated_user = get_authenticated_user_details(request_headers = request.headers)
        //user_id = authenticated_user["user_principal_id"]

        //# get conversations for user
        //try:
        //    ## make sure cosmos is configured
        //    cosmos_conversation_client = init_cosmosdb_client()
        //    if not cosmos_conversation_client:
        //        raise Exception("CosmosDB is not configured or not working")

        //    conversations = await cosmos_conversation_client.get_conversations(
        //        user_id, offset = 0, limit = None
        //    )
        //    if not conversations:
        //        return jsonify({ "error": f"No conversations for {user_id} were found"}), 404

        //    # delete each conversation
        //    for conversation in conversations:
        //        ## delete the conversation messages from cosmos first
        //        deleted_messages = await cosmos_conversation_client.delete_messages(
        //            conversation["id"], user_id
        //        )

        //        ## Now delete the conversation
        //        deleted_conversation = await cosmos_conversation_client.delete_conversation(
        //            user_id, conversation["id"]
        //        )
        //    await cosmos_conversation_client.cosmosdb_client.close()
        //    return (
        //        jsonify(
        //            {
        //        "message": f"Successfully deleted conversation and messages for user {user_id}"
        //            }
        //        ),
        //        200,
        //    )

        //except Exception as e:
        await Task.Delay(0);
        throw new NotImplementedException();
    }

    private static async Task RenameHistory(HttpContext context)
    {
        //authenticated_user = get_authenticated_user_details(request_headers = request.headers)
        //user_id = authenticated_user["user_principal_id"]

        //## check request for conversation_id
        //request_json = await request.get_json()
        //conversation_id = request_json.get("conversation_id", None)

        //if not conversation_id:
        //        return jsonify({ "error": "conversation_id is required"}), 400

        //## make sure cosmos is configured
        //cosmos_conversation_client = init_cosmosdb_client()
        //if not cosmos_conversation_client:
        //        raise Exception("CosmosDB is not configured or not working")

        //## get the conversation from cosmos
        //conversation = await cosmos_conversation_client.get_conversation(
        //    user_id, conversation_id
        //)
        //if not conversation:
        //        return (
        //            jsonify(
        //            {
        //        "error": f"Conversation {conversation_id} was not found. It either does not exist or the logged in user does not have access to it."
        //            }
        //        ),
        //        404,
        //    )

        //## update the title
        //title = request_json.get("title", None)
        //if not title:
        //        return jsonify({ "error": "title is required"}), 400
        //conversation["title"] = title
        //updated_conversation = await cosmos_conversation_client.upsert_conversation(
        //    conversation
        //)

        //await cosmos_conversation_client.cosmosdb_client.close()
        //return jsonify(updated_conversation), 200
        await Task.Delay(0);
        throw new NotImplementedException();
    }

    private static async Task ReadHistory(HttpContext context)
    {
        //authenticated_user = get_authenticated_user_details(request_headers = request.headers)
        //user_id = authenticated_user["user_principal_id"]

        //## check request for conversation_id
        //request_json = await request.get_json()
        //conversation_id = request_json.get("conversation_id", None)

        //if not conversation_id:
        //        return jsonify({ "error": "conversation_id is required"}), 400

        //## make sure cosmos is configured
        //cosmos_conversation_client = init_cosmosdb_client()
        //if not cosmos_conversation_client:
        //        raise Exception("CosmosDB is not configured or not working")

        //## get the conversation object and the related messages from cosmos
        //conversation = await cosmos_conversation_client.get_conversation(
        //    user_id, conversation_id
        //)
        //## return the conversation id and the messages in the bot frontend format
        //if not conversation:
        //        return (
        //            jsonify(
        //            {
        //        "error": f"Conversation {conversation_id} was not found. It either does not exist or the logged in user does not have access to it."
        //            }
        //        ),
        //        404,
        //    )

        //# get the messages for the conversation from cosmos
        //conversation_messages = await cosmos_conversation_client.get_messages(
        //    user_id, conversation_id
        //)

        //## format the messages in the bot frontend format
        //messages = [
        //    {
        //        "id": msg["id"],
        //        "role": msg["role"],
        //        "content": msg["content"],
        //        "createdAt": msg["createdAt"],
        //        "feedback": msg.get("feedback"),
        //    }
        //    for msg in conversation_messages
        //]

        //await cosmos_conversation_client.cosmosdb_client.close()
        //return jsonify({ "conversation_id": conversation_id, "messages": messages}), 200
        await Task.Delay(0);
        throw new NotImplementedException();
    }

    private static async Task ListHistory(HttpContext context)
    {
        //offset = request.args.get("offset", 0)
        //authenticated_user = get_authenticated_user_details(request_headers = request.headers)
        //user_id = authenticated_user["user_principal_id"]

        //## make sure cosmos is configured
        //cosmos_conversation_client = init_cosmosdb_client()
        //if not cosmos_conversation_client:
        //        raise Exception("CosmosDB is not configured or not working")

        //## get the conversations from cosmos
        //conversations = await cosmos_conversation_client.get_conversations(
        //    user_id, offset = offset, limit = 25
        //)
        //await cosmos_conversation_client.cosmosdb_client.close()
        //if not isinstance(conversations, list):
        //    return jsonify({ "error": f"No conversations for {user_id} were found"}), 404

        //## return the conversation ids

        //return jsonify(conversations), 200
        await Task.Delay(0);
        throw new NotImplementedException();
    }

    private static async Task DeleteHistory(HttpContext context)
    {
        //## get the user id from the request headers
        //authenticated_user = get_authenticated_user_details(request_headers = request.headers)
        //user_id = authenticated_user["user_principal_id"]

        //## check request for conversation_id
        //request_json = await request.get_json()
        //conversation_id = request_json.get("conversation_id", None)

        //try:
        //    if not conversation_id:
        //        return jsonify({ "error": "conversation_id is required"}), 400

        //    ## make sure cosmos is configured
        //    cosmos_conversation_client = init_cosmosdb_client()
        //    if not cosmos_conversation_client:
        //        raise Exception("CosmosDB is not configured or not working")

        //    ## delete the conversation messages from cosmos first
        //    deleted_messages = await cosmos_conversation_client.delete_messages(
        //        conversation_id, user_id
        //    )

        //    ## Now delete the conversation
        //    deleted_conversation = await cosmos_conversation_client.delete_conversation(
        //        user_id, conversation_id
        //    )

        //    await cosmos_conversation_client.cosmosdb_client.close()

        //    return (
        //        jsonify(
        //            {
        //        "message": "Successfully deleted conversation and messages",
        //                "conversation_id": conversation_id,
        //            }
        //        ),
        //        200,
        //    )
        //except Exception as e:
        //    logging.exception("Exception in /history/delete")
        //    return jsonify({ "error": str(e)}), 500
        await Task.Delay(0);
        throw new NotImplementedException();
    }

    private static async Task MessageFeedback(HttpContext context)
    {
        //authenticated_user = get_authenticated_user_details(request_headers = request.headers)
        //user_id = authenticated_user["user_principal_id"]
        //cosmos_conversation_client = init_cosmosdb_client()

        //## check request for message_id
        //request_json = await request.get_json()
        //message_id = request_json.get("message_id", None)
        //message_feedback = request_json.get("message_feedback", None)
        //try:
        //    if not message_id:
        //        return jsonify({ "error": "message_id is required"}), 400

        //    if not message_feedback:
        //        return jsonify({ "error": "message_feedback is required"}), 400

        //    ## update the message in cosmos
        //    updated_message = await cosmos_conversation_client.update_message_feedback(
        //        user_id, message_id, message_feedback
        //    )
        //    if updated_message:
        //        return (
        //            jsonify(
        //                {
        //        "message": f"Successfully updated message with feedback {message_feedback}",
        //                    "message_id": message_id,
        //                }
        //            ),
        //            200,
        //        )
        //    else:
        //        return (
        //            jsonify(
        //                {
        //        "error": f"Unable to update message {message_id}. It either does not exist or the user does not have access to it."
        //                }
        //            ),
        //            404,
        //        )

        //except Exception as e:
        //    logging.exception("Exception in /history/message_feedback")
        //    return jsonify({ "error": str(e)}), 500
        await Task.Delay(0);
        throw new NotImplementedException();
    }

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

    private static async Task GenerateHistory(HttpContext context)
    {
        var authenticated_user = GetUser(context);
        var user_id = authenticated_user.UserPrincipalId;

        // check request for conversation_id
        using var streamReader = new StreamReader(context.Request.Body);
        var str = await streamReader.ReadToEndAsync();
        // mj: what should request body look like?
        var request_json = JsonSerializer.Deserialize<Dictionary<string, object>>(str);
        var conversation_id = request_json["conversation_id"];

        //    try:
        //        # make sure cosmos is configured
        //        cosmos_conversation_client = init_cosmosdb_client()
        //        if not cosmos_conversation_client:
        //            raise Exception("CosmosDB is not configured or not working")

        //        # check for the conversation_id, if the conversation is not set, we will create a new one
        //        history_metadata = { }
        //        if not conversation_id:
        //            title = await generate_title(request_json["messages"])
        //            conversation_dict = await cosmos_conversation_client.create_conversation(
        //                user_id = user_id, title = title
        //            )
        //            conversation_id = conversation_dict["id"]
        //            history_metadata["title"] = title
        //            history_metadata["date"] = conversation_dict["createdAt"]

        //        ## Format the incoming message object in the "chat/completions" messages format
        //        ## then write it to the conversation history in cosmos
        //        messages = request_json["messages"]
        //        if len(messages) > 0 and messages[-1]["role"] == "user":
        //            createdMessageValue = await cosmos_conversation_client.create_message(
        //                uuid = str(uuid.uuid4()),
        //                conversation_id = conversation_id,
        //                user_id = user_id,
        //                input_message = messages[-1],
        //            )
        //            if createdMessageValue == "Conversation not found":
        //                raise Exception(
        //                    "Conversation not found for the given conversation ID: "
        //                    + conversation_id
        //                    + "."
        //                )
        //        else:
        //            raise Exception("No user message found")

        //        await cosmos_conversation_client.cosmosdb_client.close()

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
