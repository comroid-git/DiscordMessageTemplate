using System.Text.Json.Serialization;
using DiscordMessageTemplate.Compiler;

namespace DiscordMessageTemplate;

public sealed class TemplateContext(TemplateContext? parent = null)
{
    private readonly TemplateContext? _parent = parent;
    private readonly Dictionary<string, object?> _variables = new();
    private readonly Dictionary<string, FunctionComponent> _functions = new();

    public IReadOnlyDictionary<string, object?> Variables => _variables
        .Concat(_parent?.Variables ?? [])
        .ToDictionary(e => e.Key, e => e.Value);

    public IReadOnlyDictionary<string, FunctionComponent> Functions => _functions
        .Concat(_parent?.Functions ?? [])
        .ToDictionary(e => e.Key, e => e.Value);

    public MessageData Message { get; } = new();
    public object? ReturnValue { get; set; }

    public T? SetVariable<T>(string key, T value, bool local = false)
    {
        TemplateContext? parent;
        if (!local)
        {
            // find whether any parent has this key already
            parent = _parent;
            while (parent?.Variables.ContainsKey(key) ?? false)
            {
                if (parent._parent == null)
                    break;
                parent = parent._parent;
            }

            if (parent == null || !parent.Variables.ContainsKey(key))
                parent = this;
        }
        else parent = this;

        return (T?)(parent._variables[key] = value);
    }

    public T SetFunction<T>(string key, FunctionComponent function) where T : FunctionComponent => (T)(_functions[key] = function);

    public void Sub(Action<TemplateContext> action) => Sub<object?>(ctx =>
    {
        action(ctx);
        return null;
    });

    public T Sub<T>(Func<TemplateContext, T> action)
    {
        var sub = new TemplateContext(this);
        return action(sub);
    }
}

public sealed class MessageData
{
    [JsonPropertyName("content")] public string? Content { get; set; }
    [JsonPropertyName("attachments")] public List<MessageAttachment> Attachments { get; } = [];
    [JsonPropertyName("embeds")] public List<MessageEmbed> Embeds { get; } = [];

    [JsonIgnore]
    public MessageEmbed Embed
    {
        get
        {
            if (Embeds.Count != 0)
                return Embeds[0];
            var embed = new MessageEmbed();
            Embeds.Add(embed);
            return embed;
        }
    }
}

public sealed class MessageAttachment
{
    [JsonPropertyName("url")] public string Url { get; init; }
}

public sealed class MessageEmbed
{
    private MessageEmbedAuthor? _author;
    private MessageEmbedImage? _image;
    private MessageEmbedFooter? _footer;
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("type")] public string Type { get; } = "rich";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
    [JsonPropertyName("timestamp")] public DateTime? Timestamp { get; set; }
    [JsonPropertyName("color")] public int? Color { get; set; }

    [JsonPropertyName("author")]
    public MessageEmbedAuthor Author
    {
        get => _author ??= new MessageEmbedAuthor();
        set => _author = value;
    }

    [JsonPropertyName("image")]
    public MessageEmbedImage Image
    {
        get => _image ??= new MessageEmbedImage();
        set => _image = value;
    }

    [JsonPropertyName("footer")]
    public MessageEmbedFooter Footer
    {
        get => _footer ??= new MessageEmbedFooter();
        set => _footer = value;
    }

    [JsonPropertyName("fields")] public List<MessageEmbedField> Fields { get; } = [];

    [JsonIgnore]
    public MessageEmbedField LastField
    {
        get
        {
            if (Fields.Count == 0)
                Fields.Add(new MessageEmbedField());
            return Fields[^1];
        }
    }
}

public sealed class MessageEmbedAuthor
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("url")] public string? Url { get; set; }
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