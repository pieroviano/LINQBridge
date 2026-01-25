#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Linq.Expressions;

internal class MethodCallExpression2 : MethodCallExpression, IArgumentProvider
{
    private readonly Expression _arg1;
    private object _arg0;

    public MethodCallExpression2(MethodInfo method, Expression arg0, Expression arg1)
        : base(method)
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

    internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
    {
        return args != null ? Call(Method, args[0], args[1]) : Call(Method, ReturnObject<Expression>(_arg0), _arg1);
    }
}