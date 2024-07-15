using ChatApp.Server.Models;

namespace ChatApp.Server.Services;
public interface IChatService
{
    Task<ChatCompletion> CompleteChat(string prompt);
    Task<ChatCompletion> CompleteChat(Message[] messages);
}