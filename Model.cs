using System.Text.Json.Serialization;
using DiscordMessageTemplate.Compiler;

namespace DiscordMessageTemplate;

public sealed class TemplateContext
{
    public Dictionary<string, object> Variables { get; } = new();
    public Dictionary<string, ITemplateComponent> Functions { get; } = new();
    public MessageData Message { get; } = new();
}

public sealed class MessageData
{
    [JsonPropertyName("content")] public string Content { get; set; } = "";
    [JsonPropertyName("attachments")] public List<MessageAttachment> Attachments { get; set; }
    [JsonPropertyName("embeds")] public List<MessageEmbed> Embeds { get; set; }
}

public sealed class MessageAttachment
{
    [JsonPropertyName("url")] public string Url { get; set; }
}

public sealed class MessageEmbed
{
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("type")] public string Type { get; } = "rich";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("timestamp")] public DateTime? Timestamp { get; set; }
    [JsonPropertyName("color")] public int? Color { get; set; }
    [JsonPropertyName("author")] public MessageEmbedAuthor? Author { get; set; }
    [JsonPropertyName("image")] public MessageEmbedImage? Image { get; set; }
    [JsonPropertyName("footer")] public MessageEmbedFooter? Footer { get; set; }
    [JsonPropertyName("fields")] public List<MessageEmbedField>? Fields { get; set; }
}

public sealed class MessageEmbedAuthor
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("icon_url")] public string? IconUrl { get; set; }
}

public sealed class MessageEmbedImage
{
    [JsonPropertyName("url")] public string Url { get; set; }
}

public sealed class MessageEmbedFooter
{
    [JsonPropertyName("text")] public string Text { get; set; }
    [JsonPropertyName("icon_url")] public string? IconUrl { get; set; }
}

public sealed class MessageEmbedField
{
    [JsonPropertyName("name")] public string Title { get; set; }
    [JsonPropertyName("value")] public string Text { get; set; }
    [JsonPropertyName("inline")] public bool? Inline { get; set; } = false;
}