#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Linq.Expressions;

internal class MethodCallExpression4 : MethodCallExpression, IArgumentProvider
{
    private readonly Expression _arg1;
    private readonly Expression _arg2;
    private readonly Expression _arg3;
    private object _arg0;

    public MethodCallExpression4(
        MethodInfo method,
        Expression arg0,
        Expression arg1,
        Expression arg2,
        Expression arg3)
        : base(method)
    {
        _arg0 = arg0;
        _arg1 = arg1;
        _arg2 = arg2;
        _arg3 = arg3;
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
            default:
                throw new InvalidOperationException();
        }
    }

    int IArgumentProvider.ArgumentCount => 4;

    internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
    {
        return ReturnReadOnly(this, ref _arg0);
    }

    internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
    {
        return args != null
            ? Call(Method, args[0], args[1], args[2], args[3])
            : Call(Method, ReturnObject<Expression>(_arg0), _arg1, _arg2, _arg3);
    }
}