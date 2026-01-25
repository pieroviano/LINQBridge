#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace System.Linq.Expressions;

/// <summary>Represents an expression that applies a delegate or lambda expression to a list of argument expressions.</summary>
[DebuggerTypeProxy(typeof(InvocationExpressionProxy))]
public sealed class InvocationExpression : Expression, IArgumentProvider
{
    private IList<Expression> _arguments;

    internal InvocationExpression(Expression lambda, IList<Expression> arguments, Type returnType)
    {
        Expression = lambda;
        _arguments = arguments;
        Type = returnType;
    }

    /// <summary>
    ///     Gets the static type of the expression that this
    ///     <see cref="P:System.Linq.Expressions.InvocationExpression.Expression" /> represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.InvocationExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type { get; }

    /// <summary>
    ///     Returns the node type of this expression. Extension nodes should return
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.
    /// </summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> of the expression.</returns>
    public override ExpressionType NodeType => ExpressionType.Invoke;

    /// <summary>Gets the delegate or lambda expression to be applied.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression" /> that represents the delegate to be applied.</returns>
    public Expression Expression { get; }

    /// <summary>Gets the arguments that the delegate or lambda expression is applied to.</summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects which represent the arguments that the delegate is
    ///     applied to.
    /// </returns>
    public ReadOnlyCollection<Expression> Arguments => ReturnReadOnly(ref _arguments);

    internal LambdaExpression LambdaOperand => Expression.NodeType != ExpressionType.Quote
        ? Expression as LambdaExpression
        : (LambdaExpression)((UnaryExpression)Expression).Operand;

    Expression IArgumentProvider.GetArgument(int index)
    {
        return _arguments[index];
    }

    int IArgumentProvider.ArgumentCount => _arguments.Count;

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="expression">
    ///     The <see cref="P:System.Linq.Expressions.InvocationExpression.Expression" /> property of the
    ///     result.
    /// </param>
    /// <param name="arguments">
    ///     The <see cref="P:System.Linq.Expressions.InvocationExpression.Arguments" /> property of the
    ///     result.
    /// </param>
    public InvocationExpression Update(Expression expression, IEnumerable<Expression> arguments)
    {
        return expression == Expression && arguments == Arguments ? this : Invoke(expression, arguments);
    }

    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitInvocation(this);
    }

    internal InvocationExpression Rewrite(Expression lambda, Expression[] arguments)
    {
        return Invoke(lambda, (IList<Expression>)arguments ?? _arguments);
    }
}