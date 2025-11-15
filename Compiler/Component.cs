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
