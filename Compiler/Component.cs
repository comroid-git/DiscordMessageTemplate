namespace DiscordMessageTemplate.Compiler;

public interface ITemplateComponent
{
    object? Evaluate(TemplateContext context, params object?[] args);
}

public class ConstantComponent<T> : ITemplateComponent
{
    public ConstantComponent(T value)
    {
        _value = value;
    }

    private readonly T _value;

    public object? Evaluate(TemplateContext context, params object?[] args) => _value;
}

public class ContextComputedComponent<T> : ITemplateComponent
{
    public ContextComputedComponent(Func<TemplateContext, T> source)
    {
        _source = source;
    }

    private readonly Func<TemplateContext, T> _source;

    public object? Evaluate(TemplateContext context, params object?[] args)
    {
        return _source(context);
    }
}

public class ContextEmittingComponent : ITemplateComponent
{
    public ContextEmittingComponent(Action<TemplateContext> emitter)
    {
        _emitter = emitter;
    }

    private readonly Action<TemplateContext> _emitter;

    public object? Evaluate(TemplateContext context, params object?[] args)
    {
        _emitter(context);
        return null;
    }
}

public class FunctionComponent : ITemplateComponent
{
    public FunctionComponent(string[] @params, ITemplateComponent exec)
    {
        _params = @params;
        _exec = exec;
    }

    private readonly string[] _params;
    private readonly ITemplateComponent _exec;

    public object? Evaluate(TemplateContext context, params object?[] args)
    {
        return context.Sub(sub =>
        {
            // set args
            var min = Math.Min(_params.Length, args.Length);
            for (var i = 0; i < min; i++)
                sub.SetVariable(_params[i], args[i], true);

            _exec.Evaluate(sub);
            return sub.ReturnValue;
        });
    }
}