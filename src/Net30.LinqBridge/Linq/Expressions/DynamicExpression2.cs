#nullable disable
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

internal class DynamicExpression2 : DynamicExpression, IArgumentProvider
{
    private readonly Expression _arg1;
    private object _arg0;

    internal DynamicExpression2(
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0,
        Expression arg1)
        : base(delegateType, binder)
    {
        _arg0 = arg0;
        _arg1 = arg1;
    }

    Expression IArgumentProvider.GetArgument(int index)
    {
        if (index == 0)
        {
            return ReturnObject<Expression>(_arg0);
        }

        if (index == 1)
        {
            return _arg1;
        }

        throw new InvalidOperationException();
    }

    int IArgumentProvider.ArgumentCount => 2;

    internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
    {
        return ReturnReadOnly(this, ref _arg0);
    }

    internal override DynamicExpression Rewrite(Expression[] args)
    {
        return Expression.MakeDynamic(DelegateType, Binder, args[0], args[1]);
    }
}