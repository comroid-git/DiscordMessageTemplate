using DiscordMessageTemplate.Antlr;

namespace DiscordMessageTemplate.Compiler;

public sealed class Visitor : DiscordMessageTemplateBaseVisitor<ITemplateComponent>
{
    public override ITemplateComponent VisitExprString(DiscordMessageTemplateParser.ExprStringContext context)
    {
        return new ConstantComponent<string>(context.STRLIT().GetText()[1..^1]);
    }

    public override ITemplateComponent VisitExprBool(DiscordMessageTemplateParser.ExprBoolContext context)
    {
        return new ConstantComponent<bool>(context.TRUE() != null);
    }

    public override ITemplateComponent VisitExprNumber(DiscordMessageTemplateParser.ExprNumberContext context)
    {
        return new ConstantComponent<double>(double.Parse(context.GetText()));
    }

    public override ITemplateComponent VisitExprHexColor(DiscordMessageTemplateParser.ExprHexColorContext context)
    {
        var text = context.HEXNUM().GetText();
        var i = text.IndexOfAny(['#', 'x']);
        if (i != -1) throw new Exception("Invalid hex number: " + text);
        text = text[(i + 1)..];
        return new ConstantComponent<int>(int.Parse(text));
    }

    public override ITemplateComponent VisitExprVar(DiscordMessageTemplateParser.ExprVarContext context)
    {
        var varname = context.ID().GetText();
        return new ContextComputedComponent<object>(ctx => ctx.Variables[varname]);
    }

    public override ITemplateComponent VisitExprCallFunc(DiscordMessageTemplateParser.ExprCallFuncContext context)
    {
        var funcName = context.funcname.Text;
        var parameters = context.expression().Select(Visit);
        return new ContextComputedComponent<object?>(ctx =>
        {
            var args = parameters.Select(component => component.Evaluate(ctx)).ToArray();
            return ctx.Functions[funcName].Evaluate(ctx, args);
        });
    }

    public override ITemplateComponent VisitExprUnaryOp(DiscordMessageTemplateParser.ExprUnaryOpContext context)
    {
        var input = Visit(context.expression());
        var op = context.unaryOp();
        return new ContextComputedComponent<object?>(ctx => op.Evaluate(input.Evaluate(ctx)));
    }

    public override ITemplateComponent VisitExprBinaryOp(DiscordMessageTemplateParser.ExprBinaryOpContext context)
    {
        var left = Visit(context.left);
        var right = Visit(context.right);
        var op = context.binaryOp();
        return new ContextComputedComponent<object?>(ctx => op.Evaluate(left.Evaluate(ctx), right.Evaluate(ctx)));
    }
}

public static class OperatorExt
{
    public static object? Evaluate(this DiscordMessageTemplateParser.UnaryOpContext unaryOp, object? source) =>
        unaryOp.RuleIndex switch
        {
            DiscordMessageTemplateParser.RULE_unaryOpNumericalNegate => -(double?)source,
            DiscordMessageTemplateParser.RULE_unaryOpLogicalNegate => !(bool?)source,
            DiscordMessageTemplateParser.RULE_unaryOpBitwiseNegate => ~(long?)source,
            _ => throw new ArgumentOutOfRangeException(nameof(unaryOp), unaryOp, "Invalid unary operator")
        };

    public static object? Evaluate(this DiscordMessageTemplateParser.BinaryOpContext binaryOp, object? left, object? right) =>
        binaryOp.RuleIndex switch
        {
            DiscordMessageTemplateParser.RULE_binaryOpPlus => left is string str ? str + right : (double?)left + (double?)right,
            DiscordMessageTemplateParser.RULE_binaryOpMinus => (double?)left - (double?)right,
            DiscordMessageTemplateParser.RULE_binaryOpMultiply => (double?)left * (double?)right,
            DiscordMessageTemplateParser.RULE_binaryOpDivide => (double?)left / (double?)right,
            DiscordMessageTemplateParser.RULE_binaryOpModulus => (double?)left % (double?)right,
            DiscordMessageTemplateParser.RULE_binaryOpPow => Math.Pow((double)left!, (double)right!),
            DiscordMessageTemplateParser.RULE_binaryOpBitwiseAnd => (long?)left & (long?)right,
            DiscordMessageTemplateParser.RULE_binaryOpBitwiseOr => (long?)left | (long?)right,
            DiscordMessageTemplateParser.RULE_binaryOpLogicalAnd => (bool)(left ?? false) && (bool)(right ?? false),
            DiscordMessageTemplateParser.RULE_binaryOpLogicalOr => (bool)(left ?? false) || (bool)(right ?? false),
            _ => throw new ArgumentOutOfRangeException(nameof(binaryOp), binaryOp, "Invalid binary operator")
        };
}