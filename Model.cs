using System.Text.Json.Serialization;

namespace DiscordMessageTemplate;

public sealed class MessageData
{
    [JsonPropertyName("content")] private string Content { get; set; } = "";
    [JsonPropertyName("attachments")] private List<MessageAttachment> Attachments { get; set; }
    [JsonPropertyName("embeds")] private List<MessageEmbed> Embeds { get; set; }
}

public sealed class MessageAttachment
{
    [JsonPropertyName("url")] private string Url { get; set; }
}

public sealed class MessageEmbed
{
    [JsonPropertyName("title")] private string? Title { get; set; }
    [JsonPropertyName("type")] private string Type { get; } = "rich";
    [JsonPropertyName("description")] private string? Description { get; set; }
    [JsonPropertyName("url")] private string? Url { get; set; }
    [JsonPropertyName("timestamp")] private DateTime? Timestamp { get; set; }
    [JsonPropertyName("color")] private int? Color { get; set; }
    [JsonPropertyName("author")] private MessageEmbedAuthor? Author { get; set; }
    [JsonPropertyName("image")] private MessageEmbedImage? Image { get; set; }
    [JsonPropertyName("footer")] private MessageEmbedFooter? Footer { get; set; }
    [JsonPropertyName("fields")] private List<MessageEmbedField>? Fields { get; set; }
}

public sealed class MessageEmbedAuthor
{
    [JsonPropertyName("name")] private string Name { get; set; }
    [JsonPropertyName("icon_url")] private string? IconUrl { get; set; }
}

public sealed class MessageEmbedImage
{
    [JsonPropertyName("url")] private string Url { get; set; }
}

public sealed class MessageEmbedFooter
{
    [JsonPropertyName("text")] private string Text { get; set; }
    [JsonPropertyName("icon_url")] private string? IconUrl { get; set; }
}

public sealed class MessageEmbedField
{
    [JsonPropertyName("name")] private string Title { get; set; }
    [JsonPropertyName("value")] private string Text { get; set; }
    [JsonPropertyName("inline")] private bool? Inline { get; set; } = false;
}