
namespace ChatApp.Server.Models;

public class FrontendSettings
{
    [ConfigurationKeyName("auth_enabled")]
    public bool AuthEnabled { get; set; } = false;

    [ConfigurationKeyName("feedback_enabled")]
    public bool FeedbackEnabled { get; set; } = false;

    [ConfigurationKeyName("ui")]
    public UiSettings Ui { get; set; } = new();

    [ConfigurationKeyName("sanitize_answer")]
    public bool SanitizeAnswer { get; set; } = false;

    [ConfigurationKeyName("history_enabled")]
    public bool HistoryEnabled { get; set; } = false;
}

public class UiSettings
{
    [ConfigurationKeyName("title")]
    public string Title { get; set; } = "Damu";

    [ConfigurationKeyName("chat_title")]
    public string ChatTitle { get; set; } = "Start chatting";

    [ConfigurationKeyName("chat_description")]
    public string ChatDescription { get; set; } = "This chatbot is configured to answer your questions";

    [ConfigurationKeyName("show_share_button")]
    public bool ShowShareButton { get; set; } = true;
}
