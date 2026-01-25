#nullable disable
using System.Collections.ObjectModel;

namespace System.Linq.Expressions;

internal sealed class NewArrayInitExpression : NewArrayExpression
{
    internal NewArrayInitExpression(Type type, ReadOnlyCollection<Expression> expressions)
        : base(type, expressions)
    {
    }

    public override ExpressionType NodeType => ExpressionType.NewArrayInit;
}