using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Antlr4.Runtime;
using DiscordMessageTemplate.Compiler;

namespace DiscordMessageTemplate;

public abstract class TemplateContext(TemplateContext? parent, MessageData? message)
{
    private readonly TemplateContext? _parent = parent;
    private readonly Dictionary<string, object?> _variables = new();
    private readonly Dictionary<string, FunctionComponent> _functions = new();

    public virtual IReadOnlyDictionary<string, object?> Variables => _variables
        .Concat(_parent?.Variables ?? new Dictionary<string, object?>())
        .ToDictionary(e => e.Key, e => e.Value);

    public virtual IReadOnlyDictionary<string, FunctionComponent> Functions => _functions
        .Concat(_parent?.Functions ?? new Dictionary<string, FunctionComponent>())
        .ToDictionary(e => e.Key, e => e.Value);

    public MessageData Message { get; } = message ?? new MessageData();
    public object? ReturnValue { get; set; }

    public RootContext Root
    {
        get
        {
            var it = this;
            while (it._parent != null)
                it = it._parent;
            return (RootContext)it;
        }
    }

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
        var sub = new SubContext(this);
        return action(sub);
    }
}

internal sealed class SubContext : TemplateContext
{
    internal SubContext(TemplateContext parent) : base(parent, parent.Message)
    {
    }
}

public sealed class RootContext : TemplateContext
{
    public static readonly RootContext Instance = new();

    public override IReadOnlyDictionary<string, object?> Variables => Constants.Concat(base.Variables)
        .ToDictionary(e => e.Key, e => e.Value);
    public override IReadOnlyDictionary<string, FunctionComponent> Functions => SystemFunctions.Concat(base.Functions)
        .ToDictionary(e => e.Key, e => e.Value);

    private RootContext() : base(null, new MessageData())
    {
    }

    public readonly Dictionary<string, object?> Constants = new();

    public readonly ReadOnlyDictionary<string, FunctionComponent> SystemFunctions = new(new Dictionary<string, FunctionComponent>
    {
        { "now", new FunctionComponent([], new ContextComputedComponent<DateTime>(_ => DateTime.Now)) }
    });
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

public sealed class ParseException(string message, IToken srcPos) : Exception(message + '@' + srcPos.ToSrcPos());

public sealed class RuntimeException(string message, IToken srcPos) : Exception(message + '@' + srcPos.ToSrcPos());