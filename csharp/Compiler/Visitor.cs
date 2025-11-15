using Antlr4.Runtime;
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
        if (i is not 0 and not 1) throw new Exception("Invalid hex number: " + text);
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
            return ctx.Functions.ContainsKey(funcName)
                ? ctx.Functions[funcName].Evaluate(ctx, args)
                : throw new RuntimeException($"function '{funcName}' is undefined", context.funcname);
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

    public override ITemplateComponent VisitComponentText(DiscordMessageTemplateParser.ComponentTextContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx => ctx.Message.Content = str.Evaluate(ctx)?.ToString());
    }

    public override ITemplateComponent VisitComponentAttachment(DiscordMessageTemplateParser.ComponentAttachmentContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var url = str.Evaluate(ctx)?.ToString();
            if (url != null)
                ctx.Message.Attachments.Add(new MessageAttachment { Url = url });
        });
    }

    public override ITemplateComponent VisitComponentEmbed(DiscordMessageTemplateParser.ComponentEmbedContext context)
    {
        var block = context.embedComponent().Select(Visit).ToArray();
        return new ContextEmittingComponent(ctx =>
        {
            foreach (var component in block)
                component.Evaluate(ctx);
        });
    }

    public override ITemplateComponent VisitEmbedTitle(DiscordMessageTemplateParser.EmbedTitleContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var text = str.Evaluate(ctx)?.ToString();
            if (text != null)
                ctx.Message.Embed.Title = text;
        });
    }

    public override ITemplateComponent VisitEmbedUrl(DiscordMessageTemplateParser.EmbedUrlContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var url = str.Evaluate(ctx)?.ToString();
            if (url != null)
                ctx.Message.Embed.Url = url;
        });
    }

    public override ITemplateComponent VisitEmbedDescription(DiscordMessageTemplateParser.EmbedDescriptionContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var text = str.Evaluate(ctx)?.ToString();
            if (text != null)
                ctx.Message.Embed.Description = text;
        });
    }

    public override ITemplateComponent VisitEmbedTimestamp(DiscordMessageTemplateParser.EmbedTimestampContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var time = (DateTime?)str.Evaluate(ctx);
            if (null != time)
                ctx.Message.Embed.Timestamp = time;
        });
    }

    public override ITemplateComponent VisitEmbedColor(DiscordMessageTemplateParser.EmbedColorContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var color = (int?)str.Evaluate(ctx);
            if (null != color)
                ctx.Message.Embed.Color = color;
        });
    }

    public override ITemplateComponent VisitEmbedImage(DiscordMessageTemplateParser.EmbedImageContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var url = str.Evaluate(ctx)?.ToString();
            if (url != null)
                ctx.Message.Embed.Image = new MessageEmbedImage { Url = url };
        });
    }

    public override ITemplateComponent VisitEmbedAuthor(DiscordMessageTemplateParser.EmbedAuthorContext context)
    {
        var author = Visit(context.embedAuthorComponent());
        return new ContextEmittingComponent(ctx =>
        {
            // if not append/mutate
            if (context.mutate().GetToken(DiscordMessageTemplateLexer.APPEND, 0) == null)
                // clear author
                ctx.Message.Embed.Author = null;
            // apply author
            author.Evaluate(ctx);
        });
    }

    public override ITemplateComponent VisitEmbedFooter(DiscordMessageTemplateParser.EmbedFooterContext context)
    {
        var footer = Visit(context.embedFooterComponent());
        return new ContextEmittingComponent(ctx =>
        {
            // if not append/mutate
            if (context.mutate().GetToken(DiscordMessageTemplateLexer.APPEND, 0) == null)
                // clear footer
                ctx.Message.Embed.Footer = null;
            // apply footer
            footer.Evaluate(ctx);
        });
    }

    public override ITemplateComponent VisitEmbedFields(DiscordMessageTemplateParser.EmbedFieldsContext context)
    {
        var field = Visit(context.embedFieldsComponent());
        return new ContextEmittingComponent(ctx =>
        {
            // if not append/mutate
            if (context.mutate().GetToken(DiscordMessageTemplateLexer.APPEND, 0) == null)
                // clear fields
                ctx.Message.Embed.Fields.Clear();
            // apply field
            field.Evaluate(ctx);
        });
    }

    public override ITemplateComponent VisitEmbedFieldList(DiscordMessageTemplateParser.EmbedFieldListContext context) =>
        new ContextComputedComponent<MessageEmbedField[]>(ctx =>
            context.embedFieldComponentPart()
                .Select(Visit)
                .Select(comp => comp.Evaluate(ctx))
                .Cast<MessageEmbedField>()
                .ToArray());

    public override ITemplateComponent VisitEmbedAuthorFlow(DiscordMessageTemplateParser.EmbedAuthorFlowContext context)
    {
        var name = Visit(context.name) ?? throw new ParseException("missing embed.author.name", context.Start);
        var url = context.url != null ? Visit(context.url) : null;
        var icon = context.icon != null ? Visit(context.icon) : null;
        return new ContextEmittingComponent(ctx =>
        {
            ctx.Message.Embed.Author = new MessageEmbedAuthor
            {
                Name = name.Evaluate(ctx)?.ToString() ?? throw new ArgumentNullException("embed.author.name"),
                Url = url?.Evaluate(ctx)?.ToString(),
                IconUrl = icon?.Evaluate(ctx)?.ToString()
            };
        });
    }

    public override ITemplateComponent VisitEmbedAuthorObj(DiscordMessageTemplateParser.EmbedAuthorObjContext context)
    {
        return new ContextEmittingComponent(ctx =>
        {
            ctx.Message.Embed.Author = new MessageEmbedAuthor();
            foreach (var authorProperty in context.embedAuthorComponentField().Select(Visit))
                authorProperty.Evaluate(ctx);
        });
    }

    public override ITemplateComponent VisitEmbedAuthorComponentName(DiscordMessageTemplateParser.EmbedAuthorComponentNameContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var name = str.Evaluate(ctx)?.ToString();
            if (name != null)
                ctx.Message.Embed.Author.Name = name;
        });
    }

    public override ITemplateComponent VisitEmbedAuthorComponentUrl(DiscordMessageTemplateParser.EmbedAuthorComponentUrlContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var url = str.Evaluate(ctx)?.ToString();
            if (url != null)
                ctx.Message.Embed.Author.Url = url;
        });
    }

    public override ITemplateComponent VisitEmbedAuthorComponentIcon(DiscordMessageTemplateParser.EmbedAuthorComponentIconContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var iconUrl = str.Evaluate(ctx)?.ToString();
            if (iconUrl != null)
                ctx.Message.Embed.Author.IconUrl = iconUrl;
        });
    }

    public override ITemplateComponent VisitEmbedFooterFlow(DiscordMessageTemplateParser.EmbedFooterFlowContext context)
    {
        var text = Visit(context.text ?? throw new ParseException("missing embed.footer.text", context.Start));
        var icon = context.icon != null ? Visit(context.icon) : null;
        return new ContextEmittingComponent(ctx =>
        {
            ctx.Message.Embed.Footer = new MessageEmbedFooter
            {
                Text = text.Evaluate(ctx)?.ToString() ?? throw new ArgumentNullException("embed.footer.text"),
                IconUrl = icon?.Evaluate(ctx)?.ToString()
            };
        });
    }

    public override ITemplateComponent VisitEmbedFooterObj(DiscordMessageTemplateParser.EmbedFooterObjContext context)
    {
        return new ContextEmittingComponent(ctx =>
        {
            ctx.Message.Embed.Footer = new MessageEmbedFooter();
            foreach (var footerProperty in context.embedFooterComponentField().Select(Visit))
                footerProperty.Evaluate(ctx);
        });
    }

    public override ITemplateComponent VisitEmbedFooterComponentText(DiscordMessageTemplateParser.EmbedFooterComponentTextContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var text = str.Evaluate(ctx)?.ToString();
            if (text != null)
                ctx.Message.Embed.Footer.Text = text;
        });
    }

    public override ITemplateComponent VisitEmbedFooterComponentIcon(DiscordMessageTemplateParser.EmbedFooterComponentIconContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var iconUrl = str.Evaluate(ctx)?.ToString();
            if (iconUrl != null)
                ctx.Message.Embed.Footer.IconUrl = iconUrl;
        });
    }

    public override ITemplateComponent VisitEmbedFieldFlow(DiscordMessageTemplateParser.EmbedFieldFlowContext context)
    {
        var title = Visit(context.title ?? throw new ParseException("missing embed.field.title", context.Start));
        var text = Visit(context.text ?? throw new ParseException("missing embed.field.text", context.COMMA(0).Symbol));
        return new ContextEmittingComponent(ctx =>
        {
            ctx.Message.Embed.Fields.Add(new MessageEmbedField
            {
                Title = title.Evaluate(ctx)?.ToString() ?? throw new ArgumentNullException("embed.field.title"),
                Text = text.Evaluate(ctx)?.ToString() ?? throw new ArgumentNullException("embed.field.text"),
                Inline = context.INLINE() != null
            });
        });
    }

    public override ITemplateComponent VisitEmbedFieldObj(DiscordMessageTemplateParser.EmbedFieldObjContext context)
    {
        return new ContextEmittingComponent(ctx =>
        {
            ctx.Message.Embed.Fields.Add(new MessageEmbedField());
            foreach (var fieldProperty in context.embedFieldComponentField().Select(Visit))
                fieldProperty.Evaluate(ctx);
        });
    }

    public override ITemplateComponent VisitEmbedFieldComponentTitle(DiscordMessageTemplateParser.EmbedFieldComponentTitleContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var title = str.Evaluate(ctx)?.ToString();
            if (title != null)
                ctx.Message.Embed.LastField.Title = title;
        });
    }

    public override ITemplateComponent VisitEmbedFieldComponentText(DiscordMessageTemplateParser.EmbedFieldComponentTextContext context)
    {
        var str = Visit(context.expression());
        return new ContextEmittingComponent(ctx =>
        {
            var text = str.Evaluate(ctx)?.ToString();
            if (text != null)
                ctx.Message.Embed.LastField.Text = text;
        });
    }

    public override ITemplateComponent VisitEmbedFieldComponentInline(DiscordMessageTemplateParser.EmbedFieldComponentInlineContext context) =>
        new ContextEmittingComponent(ctx => ctx.Message.Embed.LastField.Inline = context.TRUE() != null);

    public override ITemplateComponent VisitStmtBlockEmpty(DiscordMessageTemplateParser.StmtBlockEmptyContext context) =>
        new ContextEmittingComponent(_ => { });

    public override ITemplateComponent VisitStmtBlock(DiscordMessageTemplateParser.StmtBlockContext context)
    {
        var statements = context.statement().Select(Visit).ToArray();
        return new ContextEmittingComponent(ctx =>
        {
            foreach (var statement in statements)
                statement.Evaluate(ctx);
        });
    }

    public override ITemplateComponent VisitStmtSingular(DiscordMessageTemplateParser.StmtSingularContext context)
    {
        var statement = Visit(context.statement());
        return new ContextEmittingComponent(ctx => statement.Evaluate(ctx));
    }

    public override ITemplateComponent VisitStmtAssign(DiscordMessageTemplateParser.StmtAssignContext context)
    {
        var value = Visit(context.expression());
        return new ContextEmittingComponent(ctx => ctx.SetVariable(context.varname.Text, value.Evaluate(ctx)));
    }

    public override ITemplateComponent VisitStmtIf(DiscordMessageTemplateParser.StmtIfContext context)
    {
        var check = Visit(context.expression());
        var execIf = Visit(context.@if);
        var execElse = Visit(context.@else);
        return new ContextEmittingComponent(ctx => ctx.Sub(sub =>
        {
            if ((bool?)check.Evaluate(sub) == true)
                execIf.Evaluate(sub);
            else execElse.Evaluate(sub);
        }));
    }

    public override ITemplateComponent VisitStmtForI(DiscordMessageTemplateParser.StmtForIContext context)
    {
        var init = context.init != null ? Visit(context.init) : null;
        var check = context.check != null ? Visit(context.check) : null;
        var accumulate = context.accumulate != null ? Visit(context.accumulate) : null;
        var exec = Visit(context.statementBlock());
        return new ContextEmittingComponent(ctx => ctx.Sub(sub =>
        {
            init?.Evaluate(sub);
            while ((bool?)check?.Evaluate(sub) ?? false)
            {
                exec.Evaluate(sub);
                accumulate?.Evaluate(sub);
            }
        }));
    }

    public override ITemplateComponent VisitStmtForEach(DiscordMessageTemplateParser.StmtForEachContext context)
    {
        var iterable = context.iterable != null ? Visit(context.iterable) : null;
        return new ContextEmittingComponent(ctx => ctx.Sub(sub =>
        {
            var varname = context.varname.Text;
            var iter = (IEnumerable<object?>)(iterable?.Evaluate(sub) ?? throw new ArgumentNullException("iterator"));
            var exec = Visit(context.statementBlock());
            foreach (var each in iter)
            {
                sub.SetVariable(varname, each);
                exec.Evaluate(sub);
            }
        }));
    }

    public override ITemplateComponent VisitStmtWhile(DiscordMessageTemplateParser.StmtWhileContext context)
    {
        var check = Visit(context.check);
        var exec = Visit(context.statementBlock());
        return new ContextEmittingComponent(ctx => ctx.Sub(sub =>
        {
            while ((bool?)check?.Evaluate(sub) ?? false)
                exec.Evaluate(sub);
        }));
    }

    public override ITemplateComponent VisitStmtDoWhile(DiscordMessageTemplateParser.StmtDoWhileContext context)
    {
        var exec = Visit(context.statementBlock());
        var check = Visit(context.check);
        return new ContextEmittingComponent(ctx => ctx.Sub(sub =>
        {
            do exec.Evaluate(sub);
            while ((bool?)check.Evaluate(sub) ?? false);
        }));
    }

    public override ITemplateComponent VisitStmtReturn(DiscordMessageTemplateParser.StmtReturnContext context)
    {
        var expr = Visit(context.expression());
        return new ContextEmittingComponent(ctx => ctx.ReturnValue = expr.Evaluate(ctx));
    }

    public override ITemplateComponent VisitStmtDeclFunc(DiscordMessageTemplateParser.StmtDeclFuncContext context)
    {
        var name = context.name.Text;
        if (RootContext.Instance.SystemFunctions.ContainsKey(name))
            throw new RuntimeException($"cannot rebind system function '{name}'", context.name);
        var exec = Visit(context.statementBlock());
        var parameters = context.ID()[1..].Select(tn => tn.GetText()).ToArray();
        return new ContextEmittingComponent(ctx => ctx.SetFunction<FunctionComponent>(name, new FunctionComponent(parameters, exec)));
    }

    public override ITemplateComponent VisitTemplateConst(DiscordMessageTemplateParser.TemplateConstContext context)
    {
        var expr = Visit(context.expression());
        RootContext.Instance.Constants[context.name.Text] = expr.Evaluate(RootContext.Instance);
        return new CompiledCode(context);
    }

    public override ITemplateComponent VisitTemplateStatement(DiscordMessageTemplateParser.TemplateStatementContext context)
    {
        var statements = context.statement().Select(Visit).Where(x => x != null).ToArray();
        return new TemplateDocument(statements);
    }

    public override ITemplateComponent VisitTemplateText(DiscordMessageTemplateParser.TemplateTextContext context) =>
        new ConstantComponent<string>(context.STRLIT().GetText());
}

public static class OperatorExt
{
    public static object? Evaluate(this DiscordMessageTemplateParser.UnaryOpContext unaryOp, object? source) =>
        ((RuleContext)unaryOp.children[0]).RuleIndex switch
        {
            DiscordMessageTemplateParser.RULE_unaryOpNumericalNegate => -(double?)source,
            DiscordMessageTemplateParser.RULE_unaryOpLogicalNegate => !(bool?)source,
            DiscordMessageTemplateParser.RULE_unaryOpBitwiseNegate => ~(long?)source,
            _ => throw new ArgumentOutOfRangeException(nameof(unaryOp), unaryOp, "Invalid unary operator")
        };

    public static object? Evaluate(this DiscordMessageTemplateParser.BinaryOpContext binaryOp, object? left, object? right) =>
        ((RuleContext)binaryOp.children[0]).RuleIndex switch
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
            DiscordMessageTemplateParser.RULE_binaryOpLessThan => (double?)left < (double?)right,
            DiscordMessageTemplateParser.RULE_binaryOpGreaterThan => (double?)left > (double?)right,
            _ => throw new ArgumentOutOfRangeException(nameof(binaryOp), binaryOp, "Invalid binary operator")
        };
}