#nullable disable
using System.Collections.ObjectModel;

namespace System.Linq.Expressions;

internal sealed class NewArrayBoundsExpression : NewArrayExpression
{
    internal NewArrayBoundsExpression(Type type, ReadOnlyCollection<Expression> expressions)
        : base(type, expressions)
    {
    }

    public override ExpressionType NodeType => ExpressionType.NewArrayBounds;
}