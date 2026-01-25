#nullable disable
using System.Diagnostics;

namespace System.Linq.Expressions;

/// <summary>Represents an expression that has a constant value.</summary>
[DebuggerTypeProxy(typeof(ConstantExpressionProxy))]
public class ConstantExpression : Expression
{
    internal ConstantExpression(object value)
    {
        Value = value;
    }

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.ConstantExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type => Value == null ? typeof(object) : Value.GetType();

    /// <summary>
    ///     Returns the node type of this Expression. Extension nodes should return
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.
    /// </summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> of the expression.</returns>
    public sealed override ExpressionType NodeType => ExpressionType.Constant;

    /// <summary>Gets the value of the constant expression.</summary>
    /// <returns>An <see cref="T:System.Object" /> equal to the value of the represented expression.</returns>
    public object Value { get; }

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
        return visitor.VisitConstant(this);
    }

    internal static ConstantExpression Make(object value, Type type)
    {
        return (value == null && type == typeof(object)) || (value != null && value.GetType() == type)
            ? new ConstantExpression(value)
            : new TypedConstantExpression(value, type);
    }
}