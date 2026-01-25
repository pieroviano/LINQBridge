#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Linq.Expressions;

internal class BlockN : BlockExpression
{
    private IList<Expression> _expressions;

    internal BlockN(IList<Expression> expressions)
    {
        _expressions = expressions;
    }

    internal override int ExpressionCount => _expressions.Count;

    internal override Expression GetExpression(int index)
    {
        return _expressions[index];
    }

    internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
    {
        return ReturnReadOnly(ref _expressions);
    }

    internal override BlockExpression Rewrite(
        ReadOnlyCollection<ParameterExpression> variables,
        Expression[] args)
    {
        return new BlockN(args);
    }
}