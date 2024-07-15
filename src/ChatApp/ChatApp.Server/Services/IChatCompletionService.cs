using OpenAI.Chat;

namespace ChatApp.Server.Services
{
    public interface IChatCompletionService
    {
        Task<ChatCompletion> CompleteChat(string prompt);
    }
}