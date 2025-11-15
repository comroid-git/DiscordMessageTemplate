using System.Text.Json;
using System.Text.Json.Serialization;
using Antlr4.Runtime;
using CommandLine;
using DiscordMessageTemplate.Antlr;
using DiscordMessageTemplate.Compiler;
using Parser = CommandLine.Parser;

namespace DiscordMessageTemplate;

public class CommandLineOptions
{
    public static CommandLineOptions Current { get; internal set; }

    [Option('n', "nulls", HelpText = "Include null values")]
    public bool PrintNulls { get; set; }

    [Option('p', "pretty", HelpText = "Pretty printing")]
    public bool Prettify { get; set; }

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
        var result = Evaluate(template);
        return JsonSerializer.Serialize(result,
            new JsonSerializerOptions
            {
                WriteIndented = CommandLineOptions.Current.Prettify,
                DefaultIgnoreCondition = CommandLineOptions.Current.PrintNulls ? JsonIgnoreCondition.Never : JsonIgnoreCondition.WhenWritingNull
            });
    }

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