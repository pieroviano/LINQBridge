#nullable disable
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

internal class DynamicExpression3 : DynamicExpression, IArgumentProvider
{
    private readonly Expression _arg1;
    private readonly Expression _arg2;
    private object _arg0;

    internal DynamicExpression3(
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0,
        Expression arg1,
        Expression arg2)
        : base(delegateType, binder)
    {
        _arg0 = arg0;
        _arg1 = arg1;
        _arg2 = arg2;
    }

    Expression IArgumentProvider.GetArgument(int index)
    {
        switch (index)
        {
            case 0:
                return ReturnObject<Expression>(_arg0);
            case 1:
                return _arg1;
            case 2:
                return _arg2;
            default:
                throw new InvalidOperationException();
        }
    }

    int IArgumentProvider.ArgumentCount => 3;

    internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
    {
        return ReturnReadOnly(this, ref _arg0);
    }

    internal override DynamicExpression Rewrite(Expression[] args)
    {
        return Expression.MakeDynamic(DelegateType, Binder, args[0], args[1], args[2]);
    }
}