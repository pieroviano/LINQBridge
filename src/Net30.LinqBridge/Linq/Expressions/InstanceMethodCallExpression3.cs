#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Linq.Expressions;

internal class InstanceMethodCallExpression3 : MethodCallExpression, IArgumentProvider
{
    private readonly Expression _arg1;
    private readonly Expression _arg2;
    private readonly Expression _instance;
    private object _arg0;

    public InstanceMethodCallExpression3(
        MethodInfo method,
        Expression instance,
        Expression arg0,
        Expression arg1,
        Expression arg2)
        : base(method)
    {
        _instance = instance;
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

    internal override Expression GetInstance()
    {
        return _instance;
    }

    internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
    {
        return ReturnReadOnly(this, ref _arg0);
    }

    internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
    {
        return args != null
            ? Call(instance, Method, args[0], args[1], args[2])
            : Call(instance, Method, ReturnObject<Expression>(_arg0), _arg1, _arg2);
    }
}