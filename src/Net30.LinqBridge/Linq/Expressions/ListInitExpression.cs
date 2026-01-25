#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace System.Linq.Expressions;

/// <summary>Represents a constructor call that has a collection initializer.</summary>
[DebuggerTypeProxy(typeof(ListInitExpressionProxy))]
public sealed class ListInitExpression : Expression
{
    internal ListInitExpression(
        NewExpression newExpression,
        ReadOnlyCollection<ElementInit> initializers)
    {
        NewExpression = newExpression;
        Initializers = initializers;
    }

    /// <summary>Returns the node type of this <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> that represents this expression.</returns>
    public override ExpressionType NodeType => ExpressionType.ListInit;

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.ListInitExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type => NewExpression.Type;

    /// <summary>Gets a value that indicates whether the expression tree node can be reduced.</summary>
    /// <returns>True if the node can be reduced, otherwise false.</returns>
    public override bool CanReduce => true;

    /// <summary>Gets the expression that contains a call to the constructor of a collection type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> that represents the call to the constructor of a
    ///     collection type.
    /// </returns>
    public NewExpression NewExpression { get; }

    /// <summary>Gets the element initializers that are used to initialize a collection.</summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of
    ///     <see cref="T:System.Linq.Expressions.ElementInit" /> objects which represent the elements that are used to
    ///     initialize the collection.
    /// </returns>
    public ReadOnlyCollection<ElementInit> Initializers { get; }

    /// <summary>Reduces the binary expression node to a simpler expression.</summary>
    /// <returns>The reduced expression.</returns>
    public override Expression Reduce()
    {
        return MemberInitExpression.ReduceListInit(NewExpression, Initializers, true);
    }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="newExpression">
    ///     The <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property of
    ///     the result.
    /// </param>
    /// <param name="initializers">
    ///     The <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> property of the
    ///     result.
    /// </param>
    public ListInitExpression Update(
        NewExpression newExpression,
        IEnumerable<ElementInit> initializers)
    {
        return newExpression == NewExpression && initializers == Initializers
            ? this
            : ListInit(newExpression, initializers);
    }

    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitListInit(this);
    }
}