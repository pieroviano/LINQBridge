#nullable disable
using System.Reflection;

namespace System.Linq.Expressions;

/// <summary>Represents assignment operation for a field or property of an object.</summary>
public sealed class MemberAssignment : MemberBinding
{
    internal MemberAssignment(MemberInfo member, Expression expression)
#pragma warning disable CS0618 // Type or member is obsolete
        : base(MemberBindingType.Assignment, member)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        Expression = expression;
    }

    /// <summary>Gets the expression to assign to the field or property.</summary>
    /// <returns>
    ///     The <see cref="T:System.Linq.Expressions.Expression" /> that represents the value to assign to the field or
    ///     property.
    /// </returns>
    public Expression Expression { get; }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="expression">
    ///     The <see cref="P:System.Linq.Expressions.MemberAssignment.Expression" /> property of the
    ///     result.
    /// </param>
    public MemberAssignment Update(Expression expression)
    {
        return expression == Expression ? this : Expression.Bind(Member, expression);
    }
}