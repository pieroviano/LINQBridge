#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Linq.Expressions;

internal class MethodCallExpression5 : MethodCallExpression, IArgumentProvider
{
    private readonly Expression _arg1;
    private readonly Expression _arg2;
    private readonly Expression _arg3;
    private readonly Expression _arg4;
    private object _arg0;

    public MethodCallExpression5(
        MethodInfo method,
        Expression arg0,
        Expression arg1,
        Expression arg2,
        Expression arg3,
        Expression arg4)
        : base(method)
    {
        _arg0 = arg0;
        _arg1 = arg1;
        _arg2 = arg2;
        _arg3 = arg3;
        _arg4 = arg4;
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
            case 3:
                return _arg3;
            case 4:
                return _arg4;
            default:
                throw new InvalidOperationException();
        }
    }

    int IArgumentProvider.ArgumentCount => 5;

    internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
    {
        return ReturnReadOnly(this, ref _arg0);
    }

    internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
    {
        return args != null
            ? Call(Method, args[0], args[1], args[2], args[3], args[4])
            : Call(Method, ReturnObject<Expression>(_arg0), _arg1, _arg2, _arg3, _arg4);
    }
}