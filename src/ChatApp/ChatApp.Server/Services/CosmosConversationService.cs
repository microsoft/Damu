﻿using ChatApp.Server.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System;

namespace ChatApp.Server.Services;

internal class CosmosConversationService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Database _database;
    private readonly Container _container;
    private readonly ILogger _logger;
    private readonly string _databaseId;
    private readonly string _containerId;

    public CosmosConversationService(ILogger<CosmosConversationService> logger, CosmosClient cosmosClient, IOptions<CosmosOptions> cosmosOptions)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        _databaseId = cosmosOptions.Value.CosmosDatabaseId;
        _containerId = cosmosOptions.Value.CosmosContainerId;
        _database = _cosmosClient.GetDatabase(_databaseId);
        _container = _cosmosClient.GetContainer(_databaseId, _containerId);
    }


    internal async Task<(bool, Exception?)> EnsureAsync()
    {
        if (_cosmosClient == null || _database == null || _container == null)
            return (false, new Exception($"CosmosDB database with ID {_databaseId} on account {_cosmosClient?.Endpoint} not initialized correctly."));

        try
        {
            var dbInfo = await _database.ReadAsync();
        }
        catch (Exception readException)
        {
            return (false, new Exception($"CosmosDB database with ID {_databaseId} on account {_cosmosClient?.Endpoint} not found.", readException));
        }

        try
        {
            var containerInfo = await _container.ReadContainerAsync();
        }
        catch (Exception readException)
        {
            return (false, new Exception($"CosmosDB container with ID {_databaseId} on account {_cosmosClient?.Endpoint} not found.", readException));
        }

        return (true, null);  // return True, "CosmosDB client initialized successfully"
    }

    internal async Task<Conversation> CreateConversationAsync(string userId, string title = "")
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid().ToString(),
            Type = "conversation",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UserId = userId,
            Title = title
        };

        //## TODO: add some error handling based on the output of the upsert_item call
        var response = await _container.UpsertItemAsync(conversation);

        if (response != null)
            return response;
        else
            throw new Exception("Failed to create conversation.");
    }

    internal async Task<Conversation?> UpdateConversationAsync(string userId, Conversation conversation)
    {
        var dbConversation = await _container.ReadItemAsync<Conversation>(conversation.Id, new PartitionKey(userId));

        if (conversation == null)
            return null;

        dbConversation.Resource.Title = conversation.Title;
        dbConversation.Resource.UpdatedAt = DateTime.UtcNow;

        var response = await _container.UpsertItemAsync(dbConversation.Resource);

        return response.Resource;
    }

    internal async Task<bool> DeleteConversationAsync(string userId, string conversationId)
    {
        // todo: make sure we delete related messages as well
        var conversation = await _container.ReadItemAsync<Conversation>(conversationId, new PartitionKey(userId));

        if (conversation != null)
        {
            var response = await _container.DeleteItemAsync<Conversation>(conversationId, new PartitionKey(userId));
            return response != null; // todo: in original code, some branches offer the deleted item as a return value while others return a boolean
        }
        else
        {
            return true;
        }
    }

    internal async Task<bool> DeleteConversationsAsync(string userId)
    {
        // todo: is return type of bool worthwile?
        var iterator = _container.GetItemLinqQueryable<Conversation>()
           .Where(m => m.UserId == userId)
           .ToFeedIterator();

        var tasks = new List<Task>();

        while (iterator.HasMoreResults)
        {            
            foreach (var item in await iterator.ReadNextAsync())
            {
                tasks.Add(_container.DeleteItemAsync<Conversation>(item.Id, new PartitionKey(userId)));
            }
        }

        await Task.WhenAll(tasks);

        return true;
    }

    internal async Task DeleteMessagesAsync(string conversationId, string userId)
    {
        var messages = await GetMessagesAsync(userId, conversationId).ToListAsync();

        if (messages == null)
            return;

        var deleteTasks = new List<Task>();
        foreach (var message in messages)
        {
            // is this right?
            deleteTasks.Add(_container.DeleteItemAsync<Message>(message.Id, new PartitionKey(userId)));

            _logger.LogTrace("Deleted message {messageId} from conversation {conversationId}", message.Id, conversationId);
        }
        await Task.WhenAll(deleteTasks);
    }

    internal async IAsyncEnumerable<HistoryMessage> GetMessagesAsync(string userId, string conversationId)
    {
        var iterator = _container.GetItemLinqQueryable<HistoryMessage>()
            .Where(m => m.UserId == userId && m.ConversationId == conversationId)
            .ToFeedIterator();

        while (iterator.HasMoreResults)
        {
            foreach (var session in await iterator.ReadNextAsync())
            {
                yield return session;
            }
        }
    }

    public async IAsyncEnumerable<Conversation>GetConversationsAsync(string userId, int limit, string sortOrder = "DESC", int offset = 0)
    {
        using FeedIterator<Conversation> feed = _container.GetItemLinqQueryable<Conversation>()
            .Where(m => m.UserId == userId && m.Type == "conversation")
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToFeedIterator();
        
        while (feed.HasMoreResults)
        {
            foreach (var session in await feed.ReadNextAsync())
            {
                yield return session;
            }
        }        
    }

    public async Task<Conversation> GetConversationAsync(string userId, string conversationId)
    {
        var conversation = await _container.ReadItemAsync<Conversation>(conversationId, new PartitionKey(userId));

        return conversation;
    }

    public async Task<HistoryMessage?> UpdateMessageFeedbackAsync(string userId, string messageId, string feedback)
    {
        var message = await _container.ReadItemAsync<HistoryMessage>(messageId, new PartitionKey(userId));

        if (message == null)
            return null;

        message.Resource.Feedback = feedback;
        var response = await _container.UpsertItemAsync(message.Resource);

        return response.Resource;
    }

    public async Task<bool> EnableMessageFeedbackAsync(string userId, string messageId)
    {
        //if self.enable_message_feedback:
        //            message['feedback'] = ''


        //        resp = await self.container_client.upsert_item(message)
        //        if resp:
        //            ## update the parent conversations's updatedAt field with the current message's createdAt datetime value
        //            conversation = await self.get_conversation(user_id, conversation_id)
        //            if not conversation:
        //    return "Conversation not found"
        //            conversation['updatedAt'] = message['createdAt']
        //            await self.upsert_conversation(conversation)
        //            return resp
        //        else:
        //            return False

        var message = await _container.ReadItemAsync<HistoryMessage>(messageId, new PartitionKey(userId));

        if (message != null)
        {
            message.Resource.Feedback = string.Empty;
            var response = await _container.UpsertItemAsync(message.Resource);
            return response != null;
        }
        else
        {
            return false;
        }
    }

    public async Task<ItemResponse<HistoryMessage>> CreateMessageAsync(string id, string conversationId, string userId, Message message)
    {
        var historyMessage = new HistoryMessage
        {
            Id = id,
            Type = "message",
            UserId = userId,
            ConversationId = conversationId,
            Role = message.Role,
            Content = message.Content
        };


        //if self.enable_message_feedback:
        //    message['feedback'] = ''

        var resp = await _container.UpsertItemAsync(historyMessage);

        if (resp == null)
            throw new Exception("Failed to create message.");

        var parentConversation = await GetConversationAsync(userId, conversationId);
        parentConversation.UpdatedAt = DateTime.UtcNow;
        await UpdateConversationAsync(userId, parentConversation);

        return resp;
    }

}
