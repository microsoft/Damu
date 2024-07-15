using ChatApp.Server.Models;
using Microsoft.Azure.Cosmos;

namespace ChatApp.Server.Services;

internal class CosmosConversationService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Database _database;
    private readonly Container _container;
    private readonly ILogger _logger;
    private readonly string _databaseId;
    private readonly string _containerId;

    public CosmosConversationService(ILogger<CosmosConversationService> logger, CosmosClient cosmosClient, string databaseId, string containerId)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        _databaseId = databaseId;
        _containerId = containerId;
        _database = _cosmosClient.GetDatabase(databaseId);
        _container = _cosmosClient.GetContainer(databaseId, containerId);
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

    internal async Task<bool> DeleteConversationAsync(string userId, string conversationId)
    {
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

    internal async Task DeleteMessagesAsync(string conversationId, string userId)
    {
        var messages = await GetMessagesAsync(userId, conversationId);

        if (messages == null)
            return;

        foreach (var message in messages)
        {
            // is this right?
            var response = await _container.DeleteItemAsync<Message>(message.Id, new PartitionKey(userId));

            _logger.LogTrace("Deleted message {messageId} from conversation {conversationId}", message.Id, conversationId);
        }
    }

    internal async Task<IList<Message>> GetMessagesAsync(string userId, string conversationId)
    {
        await Task.Delay(0);

        // todo: check this... feels like it could use some kind of async call?
        var messages = _container.GetItemLinqQueryable<Message>()
            .Where(m => m.ConversationId == conversationId && m.UserId == userId)
            .OrderBy(m => m.CreatedAt)
            .ToList();

        return messages;
    }

    public async Task<IList<Conversation>> GetConversationsAsync(string userId, int limit, string sortOrder = "DESC", int offset = 0)
    {
        await Task.Delay(0);

        // todo: check this... feels like it could use some kind of async call?
        var conversations = _container.GetItemLinqQueryable<Conversation>()
            .Where(c => c.UserId == userId && c.Type == "conversation")
            .OrderBy(c => c.UpdatedAt)
            .ToList();

        return conversations;
        //var query = new QueryDefinition("SELECT * FROM c where c.userId = @userId and c.type='conversation' order by c.updatedAt " + sortOrder);
        //query.WithParameter("@userId", userId);


        //var conversations = new List<Conversation>();

        //await foreach (var item in _container.GetItemQueryIterator<Conversation>(query))
        //{
        //    conversations.Add(item);
        //}

        //return conversations;
    }

    public async Task<Conversation> GetConversationAsync(string userId, string conversationId)
    {
        var conversation = await _container.ReadItemAsync<Conversation>(conversationId, new PartitionKey(userId));

        return conversation;
    }

    public async Task<bool> UpdateMessageFeedback(string userId, string messageId, string feedback)
    {
        var message = await _container.ReadItemAsync<Message>(messageId, new PartitionKey(userId));

        if (message != null)
        {
            message.Resource.Feedback = feedback;
            var response = await _container.UpsertItemAsync(message.Resource);
            return response != null;
        }
        else
        {
            return false;
        }
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

        var message = await _container.ReadItemAsync<Message>(messageId, new PartitionKey(userId));

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
}
