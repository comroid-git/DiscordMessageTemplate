using Antlr4.Runtime;

namespace DiscordMessageTemplate.Compiler;

public struct SourcefilePosition
{
    public string SourcefilePath;
    public int SourcefileLine;
    public int SourcefileCursor;

    public override string ToString() => SourcefilePath == null ? "<internal>" : $"file {SourcefilePath} in line {SourcefileLine}:{SourcefileCursor}";
}

public static class ComponentModel
{
    public static SourcefilePosition ToSrcPos(this IToken? token, string? clsName = null) => token == null
        ? new SourcefilePosition()
        : new SourcefilePosition { SourcefileLine = token.Line, SourcefileCursor = token.Column, SourcefilePath = clsName ?? "<unknown>" };
}

public interface ITemplateComponent
{
    object? Evaluate(TemplateContext context, params object?[] args);
}

public class TemplateDocument(params ITemplateComponent[] statements) : ITemplateComponent
{
    public object? Evaluate(TemplateContext context, params object?[] args)
    {
        foreach (var statement in statements)
            statement.Evaluate(context);
        return null;
    }
}

public class CompiledCode(ParserRuleContext context) : ITemplateComponent
{
    public object? Evaluate(TemplateContext context, params object?[] args) => null;
}

public class ConstantComponent<T>(T value) : ITemplateComponent
{
    public object? Evaluate(TemplateContext context, params object?[] args) => value;
}

public class ContextComputedComponent<T>(Func<TemplateContext, T> source) : ITemplateComponent
{
    public object? Evaluate(TemplateContext context, params object?[] args)
    {
        return source(context);
    }
}

public class ArgumentComputedComponent<T>(Func<TemplateContext, object?[], T> source) : ITemplateComponent
{
    public object? Evaluate(TemplateContext context, params object?[] args) => source(context, args);
}

public class ContextEmittingComponent(Action<TemplateContext> emitter) : ITemplateComponent
{
    public object? Evaluate(TemplateContext context, params object?[] args)
    {
        emitter(context);
        return null;
    }
}

public class FunctionComponent(string[] @params, ITemplateComponent exec) : ITemplateComponent
{
    public IToken? SrcPos { get; init; }

    public object? Evaluate(TemplateContext context, params object?[] args)
    {
        return context.Sub(sub =>
        {
            // set args
            if (args.Length < @params.Length)
                throw new RuntimeException($"not enough arguments; expected {@params.Length}, got [{string.Join(",", args)}]", SrcPos);
            for (var i = 0; i < @params.Length; i++)
                sub.SetVariable(@params[i], args[i], true);

            exec.Evaluate(sub, args);
            return sub.ReturnValue;
        });
    }
}