#nullable disable
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Linq.Expressions;

internal class NewValueTypeExpression : NewExpression
{
    internal NewValueTypeExpression(
        Type type,
        ReadOnlyCollection<Expression> arguments,
        ReadOnlyCollection<MemberInfo> members)
        : base(null, arguments, members)
    {
        Type = type;
    }

    public sealed override Type Type { get; }
}