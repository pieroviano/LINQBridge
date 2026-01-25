#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Linq.Expressions;

internal class InstanceMethodCallExpression2 : MethodCallExpression, IArgumentProvider
{
    private readonly Expression _arg1;
    private readonly Expression _instance;
    private object _arg0;

    public InstanceMethodCallExpression2(
        MethodInfo method,
        Expression instance,
        Expression arg0,
        Expression arg1)
        : base(method)
    {
        _instance = instance;
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
            ? Call(instance, Method, args[0], args[1])
            : Call(instance, Method, ReturnObject<Expression>(_arg0), _arg1);
    }
}