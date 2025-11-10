namespace DiscordMessageTemplate;

public class Program
{
    public static void Main(params string[] args)
    {
        if (args.Length == 0)
        {
            // read using stdio mode
            var buf = "";
            Console.CancelKeyPress += (s, e) => Evaluate(buf);
            int r;
            while ((r = Console.Read()) != -1)
                buf += (char)r;
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
    }
}