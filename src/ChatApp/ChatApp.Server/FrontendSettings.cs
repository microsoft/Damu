using System.Text.Json.Serialization;

namespace ChatApp.Server;

public class FrontendSettings
{
    [JsonPropertyName("auth_enabled")]
    public bool AuthEnabled { get; set; } = false;

    [JsonPropertyName("feedback_enabled")]
    public bool FeedbackEnabled { get; set; } = false;

    [JsonPropertyName("ui")]
    public UiSettings Ui { get; set; } = new();

    [JsonPropertyName("sanitize_answer")]
    public bool SanitizeAnswer { get; set; } = false;
}

public class UiSettings
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Damu";

    [JsonPropertyName("chat_title")]
    public string ChatTitle { get; set; } = "Start chatting";

    [JsonPropertyName("chat_description")]
    public string ChatDescription { get; set; } = "This chatbot is configured to answer your questions";

    [JsonPropertyName("show_share_button")]
    public bool ShowShareButton { get; set; } = true;
}
