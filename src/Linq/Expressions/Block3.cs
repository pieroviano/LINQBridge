using System;
using System.Collections.ObjectModel;

namespace System.Linq.Expressions;

internal sealed class Block3 : BlockExpression
{
    private object _arg0;

    private readonly Expression _arg1;

    private readonly Expression _arg2;

    internal override int ExpressionCount => 3;

    internal Block3(Expression arg0, Expression arg1, Expression arg2)
    {
        _arg0 = arg0;
        _arg1 = arg1;
        _arg2 = arg2;
    }

    internal override Expression GetExpression(int index)
    {
        switch (index)
        {
            case 0:
            {
                return ReturnObject<Expression>(_arg0);
            }
            case 1:
            {
                return _arg1;
            }
            case 2:
            {
                return _arg2;
            }
        }
        throw new InvalidOperationException();
    }

    internal override ReadOnlyCollection<Expression> GetOrMakeExpressions()
    {
        return ReturnReadOnlyExpressions(this, ref _arg0);
    }

    internal override BlockExpression Rewrite(ReadOnlyCollection<ParameterExpression> variables, Expression[] args)
    {
        return new Block3(args[0], args[1], args[2]);
    }
}