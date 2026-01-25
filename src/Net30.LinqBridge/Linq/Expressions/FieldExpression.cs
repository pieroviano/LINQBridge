#nullable disable
using System.Reflection;

namespace System.Linq.Expressions;

internal class FieldExpression : MemberExpression
{
    private readonly FieldInfo _field;

    public FieldExpression(Expression expression, FieldInfo member)
        : base(expression)
    {
        _field = member;
    }

    public sealed override Type Type => _field.FieldType;

    internal override MemberInfo GetMember()
    {
        return _field;
    }
}