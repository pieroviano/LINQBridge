#nullable disable
using System.Diagnostics;

namespace System.Linq.Expressions;

/// <summary>
///     Represents an unconditional jump. This includes return statements, break and continue statements, and other
///     jumps.
/// </summary>
[DebuggerTypeProxy(typeof(GotoExpressionProxy))]
public sealed class GotoExpression : Expression
{
    internal GotoExpression(
        GotoExpressionKind kind,
        LabelTarget target,
        Expression value,
        Type type)
    {
        Kind = kind;
        Value = value;
        Target = target;
        Type = type;
    }

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.GotoExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type { get; }

    /// <summary>Returns the node type of this <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> that represents this expression.</returns>
    public override ExpressionType NodeType => ExpressionType.Goto;

    /// <summary>The value passed to the target, or null if the target is of type System.Void.</summary>
    /// <returns>
    ///     The <see cref="T:System.Linq.Expressions.Expression" /> object representing the value passed to the target or
    ///     null.
    /// </returns>
    public Expression Value { get; }

    /// <summary>The target label where this node jumps to.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.LabelTarget" /> object representing the target label for this node.</returns>
    public LabelTarget Target { get; }

    /// <summary>The kind of the "go to" expression. Serves information purposes only.</summary>
    /// <returns>
    ///     The <see cref="T:System.Linq.Expressions.GotoExpressionKind" /> object representing the kind of the "go to"
    ///     expression.
    /// </returns>
    public GotoExpressionKind Kind { get; }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="target">The <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property of the result. </param>
    /// <param name="value">The <see cref="P:System.Linq.Expressions.GotoExpression.Value" /> property of the result. </param>
    public GotoExpression Update(LabelTarget target, Expression value)
    {
        return target == Target && value == Value ? this : MakeGoto(Kind, target, value, Type);
    }

    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitGoto(this);
    }
}