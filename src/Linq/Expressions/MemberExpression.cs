using System.Reflection;
using System.Text;

namespace System.Linq.Expressions;

/// <summary>Represents accessing a field or property.</summary>
public sealed class MemberExpression : Expression
{
    private readonly Expression expr;
    private readonly MemberInfo member;

    internal MemberExpression(Expression expression, MemberInfo member, Type type)
        : base(ExpressionType.MemberAccess, type)
    {
        expr = expression;
        this.member = member;
    }

    /// <summary>Gets the containing object of the field or property.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression" /> that represents the containing object of the field or property.</returns>
    public Expression Expression => expr;

    /// <summary>Gets the field or property to be accessed.</summary>
    /// <returns>The <see cref="T:System.Reflection.MemberInfo" /> that represents the field or property to be accessed.</returns>
    public MemberInfo Member => member;

    internal override void BuildString(StringBuilder builder)
    {
        if (builder == null)
            throw Error.ArgumentNull(nameof(builder));
        if (expr != null)
            expr.BuildString(builder);
        else
            builder.Append(member.DeclaringType.Name);
        builder.Append(".");
        builder.Append(member.Name);
    }
}