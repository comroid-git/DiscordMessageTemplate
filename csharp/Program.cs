using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Antlr4.Runtime;
using CommandLine;
using DiscordMessageTemplate.Antlr;
using DiscordMessageTemplate.Compiler;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Parser = CommandLine.Parser;

namespace DiscordMessageTemplate;

public class CommandLineOptions
{
    public static CommandLineOptions Current { get; internal set; }

    [Option('n', "nulls", HelpText = "Include null values")]
    public bool PrintNulls { get; set; }

    [Option('p', "pretty", HelpText = "Pretty printing")]
    public bool Prettify { get; set; }

    [Option('c', "context", HelpText = "Context JSON file to load before eval", Default = "context.json")]
    public string Context { get; set; }

    [Value(0, HelpText = "Input path", Required = true)]
    public string InputFile { get; set; }
}

public class Program
{
    public static void Main(params string[] args)
    {
        Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(result =>
            {
                CommandLineOptions.Current = result;
                var template = File.ReadAllText(result.InputFile);
                EvalAndPrintTemplate(template);
            });
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static string EvalTemplate(string template)
    {
        TryLoadContext();
        var result = Evaluate(template);
        return JsonSerializer.Serialize(result,
            new JsonSerializerOptions
            {
                WriteIndented = CommandLineOptions.Current.Prettify,
                DefaultIgnoreCondition = CommandLineOptions.Current.PrintNulls ? JsonIgnoreCondition.Never : JsonIgnoreCondition.WhenWritingNull
            });
    }

    private static void TryLoadContext()
    {
        var path = CommandLineOptions.Current.Context;
        if (!File.Exists(path)) return;

        var context = JsonSerializer.Deserialize<Dictionary<string, object?>>(File.OpenRead(path));
        if (context is null) return;
        context = context.ToDictionary(e => e.Key, e => UnwrapJson(e.Value));

        foreach (var (key, value) in context)
            RootContext.Instance.Constants[key] = value;
    }

    private static object? UnwrapJson(object? value) => value switch
    {
        JsonObject obj => obj.ToDictionary(e => e.Key, e => UnwrapJson(e.Value)),
        JsonArray arr => arr.Select(UnwrapJson).ToList(),
        JsonElement { ValueKind: JsonValueKind.Object } obj => obj.EnumerateObject().ToDictionary(e => e.Name, e => UnwrapJson(e.Value)),
        JsonElement { ValueKind: JsonValueKind.Array } arr => arr.EnumerateArray().Cast<object>().Select(UnwrapJson).ToList(),
        JsonElement { ValueKind: JsonValueKind.String } str => str.GetString(),
        JsonElement { ValueKind: JsonValueKind.Number } num => num.GetDouble(),
        JsonElement { ValueKind: JsonValueKind.True } => true,
        JsonElement { ValueKind: JsonValueKind.False } => false,
        JsonElement { ValueKind: JsonValueKind.Null } => null,
        _ => value
    };

    private static void EvalAndPrintTemplate(string template)
    {
        var json = EvalTemplate(template);
        Console.WriteLine(json);
    }

    private static MessageData Evaluate(string template)
    {
        var input = new AntlrInputStream(new StringReader(template));
        var lexer = new DiscordMessageTemplateLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new DiscordMessageTemplateParser(tokens);

        var visitor = new Visitor();
        var file = parser.template();
        var templateRoot = visitor.Visit(file);

        templateRoot.Evaluate(RootContext.Instance);
        return RootContext.Instance.Message;
    }
}