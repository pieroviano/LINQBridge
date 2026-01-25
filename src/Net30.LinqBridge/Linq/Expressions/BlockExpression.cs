#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Linq.Expressions;

/// <summary>Represents a block that contains a sequence of expressions where variables can be defined.</summary>
[DebuggerTypeProxy(typeof(BlockExpressionProxy))]
public class BlockExpression : Expression
{
    internal BlockExpression()
    {
    }

    /// <summary>Gets the expressions in this block.</summary>
    /// <returns>The read-only collection containing all the expressions in this block.</returns>
    public ReadOnlyCollection<Expression> Expressions => GetOrMakeExpressions();

    /// <summary>Gets the variables defined in this block.</summary>
    /// <returns>The read-only collection containing all the variables defined in this block.</returns>
    public ReadOnlyCollection<ParameterExpression> Variables => GetOrMakeVariables();

    /// <summary>Gets the last expression in this block.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.Expression" /> object representing the last expression in this block.</returns>
    public Expression Result => GetExpression(ExpressionCount - 1);

    /// <summary>
    ///     Returns the node type of this expression. Extension nodes should return
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.
    /// </summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> of the expression.</returns>
    public sealed override ExpressionType NodeType => ExpressionType.Block;

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>The <see cref="P:Type" /> that represents the static type of the expression.</returns>
    public override Type Type => GetExpression(ExpressionCount - 1).Type;

    internal virtual int ExpressionCount => throw ContractUtils.Unreachable;

    internal virtual int VariableCount => 0;

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
    /// <param name="variables">The <see cref="P:System.Linq.Expressions.BlockExpression.Variables" /> property of the result. </param>
    /// <param name="expressions">
    ///     The <see cref="P:System.Linq.Expressions.BlockExpression.Expressions" /> property of the
    ///     result.
    /// </param>
    public BlockExpression Update(
        IEnumerable<ParameterExpression> variables,
        IEnumerable<Expression> expressions)
    {
        return variables == Variables && expressions == Expressions ? this : Block(Type, variables, expressions);
    }

    /// <summary>
    ///     Dispatches to the specific visit method for this node type. For example,
    ///     <see cref="T:System.Linq.Expressions.MethodCallExpression" /> calls the
    ///     <see
    ///         cref="M:System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />
    ///     .
    /// </summary>
    /// <returns>The result of visiting this node.</returns>
    /// <param name="visitor">The visitor to visit this node with.</param>
    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitBlock(this);
    }

    internal virtual Expression GetExpression(int index)
    {
        throw ContractUtils.Unreachable;
    }

    internal virtual ReadOnlyCollection<Expression> GetOrMakeExpressions()
    {
        throw ContractUtils.Unreachable;
    }

    internal virtual ReadOnlyCollection<ParameterExpression> GetOrMakeVariables()
    {
        return EmptyReadOnlyCollection<ParameterExpression>.Instance;
    }

    internal virtual ParameterExpression GetVariable(int index)
    {
        throw ContractUtils.Unreachable;
    }

    internal static ReadOnlyCollection<Expression> ReturnReadOnlyExpressions(
        BlockExpression provider,
        ref object collection)
    {
        if (collection is Expression comparand)
        {
            Interlocked.CompareExchange(ref collection,
                new ReadOnlyCollection<Expression>(new BlockExpressionList(provider, comparand)), comparand);
        }

        return (ReadOnlyCollection<Expression>)collection;
    }

    internal virtual BlockExpression Rewrite(
        ReadOnlyCollection<ParameterExpression> variables,
        Expression[] args)
    {
        throw ContractUtils.Unreachable;
    }
}