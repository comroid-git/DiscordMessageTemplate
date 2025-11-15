using Antlr4.Runtime;
using DiscordMessageTemplate.Antlr;
using DiscordMessageTemplate.Compiler;

namespace DiscordMessageTemplate;

public class Program
{
    public static void Main(params string[] args)
    {
        if (args.Length == 0)
        {
            // read using stdio mode
            var buf = new[]{""};
            Console.CancelKeyPress += (_, _) => Evaluate(buf[0]);
            int r;
            while ((r = Console.Read()) != -1)
                buf[0] += (char)r;
        }
        else
        {
            // read from file
            var path = string.Join(" ", args);
            var template = File.ReadAllText(path);
            Evaluate(template);
        }
    }

    public static MessageData Evaluate(string template)
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