using System.Text.Json.Serialization;

namespace ChatApp.Server;

public class FrontendSettings
{
    [JsonPropertyName("auth_enabled")]
    public bool AuthEnabled { get; set; }

    [JsonPropertyName("feedback_enabled")]
    public bool FeedbackEnabled { get; set; }

    [JsonPropertyName("ui")]
    public UiSettings Ui { get; set; }

    [JsonPropertyName("sanitize_answer")]
    public bool SanitizeAnswer { get; set; }
}

public class UiSettings
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("chat_title")]
    public string ChatTitle { get; set; }

    [JsonPropertyName("chat_description")]
    public string ChatDescription { get; set; }

    [JsonPropertyName("show_share_button")]
    public bool ShowShareButton { get; set; }
}
