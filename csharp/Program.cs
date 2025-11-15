using System.Text.Json;
using System.Text.Json.Serialization;
using Antlr4.Runtime;
using CommandLine;
using DiscordMessageTemplate.Antlr;
using DiscordMessageTemplate.Compiler;

namespace DiscordMessageTemplate;

public class CommandLineOptions
{
    [Option('n', "nulls", HelpText = "Include null values")]
    public bool PrintNulls { get; set; }

    [Value(0, HelpText = "Input string")]
    public string Input { get; set; }
}

public class Program
{
    public static void Main(params string[] args)
    {
        if (args.Length == 0)
        {
            // read using stdio mode
            var buf = new[] { "" };
            Console.CancelKeyPress += (_, _) => EvalAndPrintTemplate(buf[0]);
            int r;
            while ((r = Console.Read()) != -1)
                buf[0] += (char)r;
        }
        else
        {
            // read from file
            var path = string.Join(" ", args);
            var template = File.ReadAllText(path);
            EvalAndPrintTemplate(template);
        }
    }

    private static void EvalAndPrintTemplate(string template)
    {
        var result = Evaluate(template);
        var json = JsonSerializer.Serialize(result,
            new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
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