using System;
using System.Collections.ObjectModel;

namespace System.Linq.Expressions;

internal sealed class Block2 : BlockExpression
{
    private object _arg0;

    private readonly Expression _arg1;

    internal override int ExpressionCount => 2;

    internal Block2(Expression arg0, Expression arg1): base()
    {

        _arg0 = arg0;
        _arg1 = arg1;
    }

    internal override Expression GetExpression(int index)
    {
        if (index == 0)
        {
            return ReturnObject<Expression>(_arg0);
        }
        if (index != 1)
        {
            throw new InvalidOperationException();
        }
        return _arg1;
    }

    internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
    {
        return ReturnReadOnlyExpressions(this, ref _arg0);
    }

    internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
    {
        return new Block2(args[0], args[1]);
    }
}