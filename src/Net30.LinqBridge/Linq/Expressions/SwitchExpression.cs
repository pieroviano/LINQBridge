#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions;

/// <summary>
///     Represents a control expression that handles multiple selections by passing control to
///     <see cref="T:System.Linq.Expressions.SwitchCase" />.
/// </summary>
[DebuggerTypeProxy(typeof(SwitchExpressionProxy))]
public sealed class SwitchExpression : Expression
{
    internal SwitchExpression(
        Type type,
        Expression switchValue,
        Expression defaultBody,
        MethodInfo comparison,
        ReadOnlyCollection<SwitchCase> cases)
    {
        Type = type;
        SwitchValue = switchValue;
        DefaultBody = defaultBody;
        Comparison = comparison;
        Cases = cases;
    }

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.SwitchExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type { get; }

    /// <summary>
    ///     Returns the node type of this Expression. Extension nodes should return
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.
    /// </summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> of the expression.</returns>
    public override ExpressionType NodeType => ExpressionType.Switch;

    /// <summary>Gets the test for the switch.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.Expression" /> object representing the test for the switch.</returns>
    public Expression SwitchValue { get; }

    /// <summary>Gets the collection of <see cref="T:System.Linq.Expressions.SwitchCase" /> objects for the switch.</summary>
    /// <returns>The collection of <see cref="T:System.Linq.Expressions.SwitchCase" /> objects.</returns>
    public ReadOnlyCollection<SwitchCase> Cases { get; }

    /// <summary>Gets the test for the switch.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.Expression" /> object representing the test for the switch.</returns>
    public Expression DefaultBody { get; }

    /// <summary>Gets the equality comparison method, if any.</summary>
    /// <returns>The <see cref="T:System.Reflection.MethodInfo" /> object representing the equality comparison method.</returns>
    public MethodInfo Comparison { get; }

    internal bool IsLifted
    {
        get
        {
            if (!SwitchValue.Type.IsNullableType())
            {
                return false;
            }

            return Comparison == null || !TypeUtils.AreEquivalent(SwitchValue.Type,
                Comparison.GetParameters()[0].ParameterType.GetNonRefType());
        }
    }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="switchValue">
    ///     The <see cref="P:System.Linq.Expressions.SwitchExpression.SwitchValue" /> property of the
    ///     result.
    /// </param>
    /// <param name="cases">The <see cref="P:System.Linq.Expressions.SwitchExpression.Cases" /> property of the result.</param>
    /// <param name="defaultBody">
    ///     The <see cref="P:System.Linq.Expressions.SwitchExpression.DefaultBody" /> property of the
    ///     result.
    /// </param>
    public SwitchExpression Update(
        Expression switchValue,
        IEnumerable<SwitchCase> cases,
        Expression defaultBody)
    {
        return switchValue == SwitchValue && cases == Cases && defaultBody == DefaultBody
            ? this
            : Switch(Type, switchValue, defaultBody, Comparison, cases);
    }

    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitSwitch(this);
    }
}