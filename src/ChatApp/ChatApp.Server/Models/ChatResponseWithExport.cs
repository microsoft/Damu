using ChatApp.Server.Models;

namespace ChatApp.Server.Services;

public class ChatResponseWithExport
{
    public ChatResponseWithExport() { }
    public ChatResponseWithExport(ChatCompletion completion, List<MinimalSearchResult> export)
    {
        Completion = completion;
        Export = export;
    }

    public ChatCompletion Completion { get; set; } = new();
    public List<MinimalSearchResult> Export { get; set; } = [];
}
