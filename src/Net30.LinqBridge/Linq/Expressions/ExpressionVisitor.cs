#nullable disable
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

/// <summary>Represents a visitor or rewriter for expression trees.</summary>
public abstract class ExpressionVisitor
{
    /// <summary>Initializes a new instance of <see cref="T:System.Linq.Expressions.ExpressionVisitor" />.</summary>
    protected ExpressionVisitor()
    {
    }

    /// <summary>Dispatches the expression to one of the more specialized visit methods in this class.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    public virtual Expression Visit(Expression node)
    {
        return node?.Accept(this);
    }

    /// <summary>Dispatches the list of expressions to one of the more specialized visit methods in this class.</summary>
    /// <returns>
    ///     The modified expression list, if any one of the elements were modified; otherwise, returns the original
    ///     expression list.
    /// </returns>
    /// <param name="nodes">The expressions to visit.</param>
    public ReadOnlyCollection<Expression> Visit(ReadOnlyCollection<Expression> nodes)
    {
        var list = (Expression[])null;
        var index1 = 0;
        for (var count = nodes.Count; index1 < count; ++index1)
        {
            var expression = Visit(nodes[index1]);
            if (list != null)
            {
                list[index1] = expression;
            }
            else if (expression != nodes[index1])
            {
                list = new Expression[count];
                for (var index2 = 0; index2 < index1; ++index2)
                {
                    list[index2] = nodes[index2];
                }

                list[index1] = expression;
            }
        }

        return list == null ? nodes : new TrueReadOnlyCollection<Expression>(list);
    }

    /// <summary>Visits all nodes in the collection using a specified element visitor.</summary>
    /// <returns>The modified node list, if any of the elements were modified; otherwise, returns the original node list.</returns>
    /// <param name="nodes">The nodes to visit.</param>
    /// <param name="elementVisitor">A delegate that visits a single element, optionally replacing it with a new element.</param>
    /// <typeparam name="T">The type of the nodes.</typeparam>
    public static ReadOnlyCollection<T> Visit<T>(
        ReadOnlyCollection<T> nodes,
        Func<T, T> elementVisitor)
    {
        var list = (T[])null;
        var index1 = 0;
        for (var count = nodes.Count; index1 < count; ++index1)
        {
            var obj = elementVisitor(nodes[index1]);
            if (list != null)
            {
                list[index1] = obj;
            }
            else if ((object)obj != (object)nodes[index1])
            {
                list = new T[count];
                for (var index2 = 0; index2 < index1; ++index2)
                {
                    list[index2] = nodes[index2];
                }

                list[index1] = obj;
            }
        }

        return list == null ? nodes : new TrueReadOnlyCollection<T>(list);
    }

    /// <summary>Visits an expression, casting the result back to the original expression type.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    /// <param name="callerName">The name of the calling method; used to report to report a better error message.</param>
    /// <typeparam name="T">The type of the expression.</typeparam>
    /// <exception cref="T:System.InvalidOperationException">The visit method for this node returned a different type.</exception>
    public T VisitAndConvert<T>(T node, string callerName) where T : Expression
    {
        if (node == null)
        {
            return default;
        }

        node = Visit(node) as T;
        return node != null ? node : throw Error.MustRewriteToSameNode(callerName, typeof(T), callerName);
    }

    /// <summary>Visits an expression, casting the result back to the original expression type.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="nodes">The expression to visit.</param>
    /// <param name="callerName">The name of the calling method; used to report to report a better error message.</param>
    /// <typeparam name="T">The type of the expression.</typeparam>
    /// <exception cref="T:System.InvalidOperationException">The visit method for this node returned a different type.</exception>
    public ReadOnlyCollection<T> VisitAndConvert<T>(ReadOnlyCollection<T> nodes, string callerName) where T : Expression
    {
        var list = (T[])null;
        var index1 = 0;
        for (var count = nodes.Count; index1 < count; ++index1)
        {
            if (!(Visit(nodes[index1]) is T obj))
            {
                throw Error.MustRewriteToSameNode(callerName, typeof(T), callerName);
            }

            if (list != null)
            {
                list[index1] = obj;
            }
            else if (obj != nodes[index1])
            {
                list = new T[count];
                for (var index2 = 0; index2 < index1; ++index2)
                {
                    list[index2] = nodes[index2];
                }

                list[index1] = obj;
            }
        }

        return list == null ? nodes : new TrueReadOnlyCollection<T>(list);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.CatchBlock" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    public virtual CatchBlock VisitCatchBlock(CatchBlock node)
    {
        return node.Update(VisitAndConvert(node.Variable, nameof(VisitCatchBlock)), Visit(node.Filter),
            Visit(node.Body));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.SwitchCase" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    public virtual SwitchCase VisitSwitchCase(SwitchCase node)
    {
        return node.Update(Visit(node.TestValues), Visit(node.Body));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.BinaryExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitBinary(BinaryExpression node)
    {
        return ValidateBinary(node,
            node.Update(Visit(node.Left), VisitAndConvert(node.Conversion, nameof(VisitBinary)), Visit(node.Right)));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.BlockExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitBlock(BlockExpression node)
    {
        var expressionCount = node.ExpressionCount;
        var args = (Expression[])null;
        for (var index = 0; index < expressionCount; ++index)
        {
            var expression1 = node.GetExpression(index);
            var expression2 = Visit(expression1);
            if (expression1 != expression2)
            {
                if (args == null)
                {
                    args = new Expression[expressionCount];
                }

                args[index] = expression2;
            }
        }

        var variables = VisitAndConvert(node.Variables, nameof(VisitBlock));
        if (variables == node.Variables && args == null)
        {
            return node;
        }

        for (var index = 0; index < expressionCount; ++index)
        {
            if (args[index] == null)
            {
                args[index] = node.GetExpression(index);
            }
        }

        return node.Rewrite(variables, args);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.ConditionalExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitConditional(ConditionalExpression node)
    {
        return node.Update(Visit(node.Test), Visit(node.IfTrue), Visit(node.IfFalse));
    }

    /// <summary>Visits the <see cref="T:System.Linq.Expressions.ConstantExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitConstant(ConstantExpression node)
    {
        return node;
    }

    /// <summary>Visits the <see cref="T:System.Linq.Expressions.DebugInfoExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitDebugInfo(DebugInfoExpression node)
    {
        return node;
    }

    /// <summary>Visits the <see cref="T:System.Linq.Expressions.DefaultExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitDefault(DefaultExpression node)
    {
        return node;
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.DynamicExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitDynamic(DynamicExpression node)
    {
        var args = VisitArguments(node);
        return args == null ? node : (Expression)node.Rewrite(args);
    }

    /// <summary>Visits the children of the extension expression.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitExtension(Expression node)
    {
        return node.VisitChildren(this);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.GotoExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitGoto(GotoExpression node)
    {
        return node.Update(VisitLabelTarget(node.Target), Visit(node.Value));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.IndexExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitIndex(IndexExpression node)
    {
        var instance = Visit(node.Object);
        var arguments = VisitArguments(node);
        return instance == node.Object && arguments == null ? node : node.Rewrite(instance, arguments);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.InvocationExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitInvocation(InvocationExpression node)
    {
        var lambda = Visit(node.Expression);
        var arguments = VisitArguments(node);
        return lambda == node.Expression && arguments == null ? node : (Expression)node.Rewrite(lambda, arguments);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.LabelExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitLabel(LabelExpression node)
    {
        return node.Update(VisitLabelTarget(node.Target), Visit(node.DefaultValue));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.Expression`1" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    /// <typeparam name="T">The type of the delegate.</typeparam>
    protected internal virtual Expression VisitLambda<T>(Expression<T> node)
    {
        return node.Update(Visit(node.Body), VisitAndConvert(node.Parameters, nameof(VisitLambda)));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.ListInitExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitListInit(ListInitExpression node)
    {
        return node.Update(VisitAndConvert(node.NewExpression, nameof(VisitListInit)),
            Visit(node.Initializers, VisitElementInit));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.LoopExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitLoop(LoopExpression node)
    {
        return node.Update(VisitLabelTarget(node.BreakLabel), VisitLabelTarget(node.ContinueLabel), Visit(node.Body));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.MemberExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitMember(MemberExpression node)
    {
        return node.Update(Visit(node.Expression));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.MemberInitExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitMemberInit(MemberInitExpression node)
    {
        return node.Update(VisitAndConvert(node.NewExpression, nameof(VisitMemberInit)),
            Visit(node.Bindings, VisitMemberBinding));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.MethodCallExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitMethodCall(MethodCallExpression node)
    {
        var instance = Visit(node.Object);
        var args = VisitArguments(node);
        return instance == node.Object && args == null ? node : (Expression)node.Rewrite(instance, args);
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.NewExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitNew(NewExpression node)
    {
        return node.Update(Visit(node.Arguments));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.NewArrayExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitNewArray(NewArrayExpression node)
    {
        return node.Update(Visit(node.Expressions));
    }

    /// <summary>Visits the <see cref="T:System.Linq.Expressions.ParameterExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitParameter(ParameterExpression node)
    {
        return node;
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.RuntimeVariablesExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
    {
        return node.Update(VisitAndConvert(node.Variables, nameof(VisitRuntimeVariables)));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.SwitchExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitSwitch(SwitchExpression node)
    {
        return ValidateSwitch(node,
            node.Update(Visit(node.SwitchValue), Visit(node.Cases, VisitSwitchCase), Visit(node.DefaultBody)));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.TryExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitTry(TryExpression node)
    {
        return node.Update(Visit(node.Body), Visit(node.Handlers, VisitCatchBlock), Visit(node.Finally),
            Visit(node.Fault));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.TypeBinaryExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        return node.Update(Visit(node.Expression));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.UnaryExpression" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected internal virtual Expression VisitUnary(UnaryExpression node)
    {
        return ValidateUnary(node, node.Update(Visit(node.Operand)));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.ElementInit" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected virtual ElementInit VisitElementInit(ElementInit node)
    {
        return node.Update(Visit(node.Arguments));
    }

    /// <summary>Visits the <see cref="T:System.Linq.Expressions.LabelTarget" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected virtual LabelTarget VisitLabelTarget(LabelTarget node)
    {
        return node;
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.MemberAssignment" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        return node.Update(Visit(node.Expression));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.MemberBinding" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected virtual MemberBinding VisitMemberBinding(MemberBinding node)
    {
        switch (node.BindingType)
        {
            case MemberBindingType.Assignment:
                return VisitMemberAssignment((MemberAssignment)node);
            case MemberBindingType.MemberBinding:
                return VisitMemberMemberBinding((MemberMemberBinding)node);
            case MemberBindingType.ListBinding:
                return VisitMemberListBinding((MemberListBinding)node);
            default:
                throw Error.UnhandledBindingType(node.BindingType);
        }
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.MemberListBinding" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding node)
    {
        return node.Update(Visit(node.Initializers, VisitElementInit));
    }

    /// <summary>Visits the children of the <see cref="T:System.Linq.Expressions.MemberMemberBinding" />.</summary>
    /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
    /// <param name="node">The expression to visit.</param>
    protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
    {
        return node.Update(Visit(node.Bindings, VisitMemberBinding));
    }

    internal Expression[] VisitArguments(IArgumentProvider nodes)
    {
        var expressionArray = (Expression[])null;
        var index1 = 0;
        for (var argumentCount = nodes.ArgumentCount; index1 < argumentCount; ++index1)
        {
            var node = nodes.GetArgument(index1);
            var expression = Visit(node);
            if (expressionArray != null)
            {
                expressionArray[index1] = expression;
            }
            else if (expression != node)
            {
                expressionArray = new Expression[argumentCount];
                for (var index2 = 0; index2 < index1; ++index2)
                {
                    expressionArray[index2] = nodes.GetArgument(index2);
                }

                expressionArray[index1] = expression;
            }
        }

        return expressionArray;
    }

    private static BinaryExpression ValidateBinary(BinaryExpression before, BinaryExpression after)
    {
        if (before != after && before.Method == null)
        {
            if (after.Method != null)
            {
                throw Error.MustRewriteWithoutMethod(after.Method, "VisitBinary");
            }

            ValidateChildType(before.Left.Type, after.Left.Type, "VisitBinary");
            ValidateChildType(before.Right.Type, after.Right.Type, "VisitBinary");
        }

        return after;
    }

    private static void ValidateChildType(Type before, Type after, string methodName)
    {
        if (before.IsValueType)
        {
            if (TypeUtils.AreEquivalent(before, after))
            {
                return;
            }
        }
        else if (!after.IsValueType)
        {
            return;
        }

        throw Error.MustRewriteChildToSameType(before, after, methodName);
    }

    private static SwitchExpression ValidateSwitch(SwitchExpression before, SwitchExpression after)
    {
        if (before.Comparison == null && after.Comparison != null)
        {
            throw Error.MustRewriteWithoutMethod(after.Comparison, "VisitSwitch");
        }

        return after;
    }

    private static UnaryExpression ValidateUnary(UnaryExpression before, UnaryExpression after)
    {
        if (before != after && before.Method == null)
        {
            if (after.Method != null)
            {
                throw Error.MustRewriteWithoutMethod(after.Method, "VisitUnary");
            }

            if (before.Operand != null && after.Operand != null)
            {
                ValidateChildType(before.Operand.Type, after.Operand.Type, "VisitUnary");
            }
        }

        return after;
    }
}