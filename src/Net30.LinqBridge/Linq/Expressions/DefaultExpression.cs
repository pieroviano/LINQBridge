#nullable disable
using System.Diagnostics;

namespace System.Linq.Expressions;

/// <summary>Represents the default value of a type or an empty expression.</summary>
[DebuggerTypeProxy(typeof(DefaultExpressionProxy))]
public sealed class DefaultExpression : Expression
{
    internal DefaultExpression(Type type)
    {
        Type = type;
    }

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.DefaultExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type { get; }

    /// <summary>
    ///     Returns the node type of this expression. Extension nodes should return
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.
    /// </summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> of the expression.</returns>
    public override ExpressionType NodeType => ExpressionType.Default;

    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitDefault(this);
    }
}