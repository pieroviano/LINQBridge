#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Linq.Expressions;

internal class MethodCallExpressionN : MethodCallExpression, IArgumentProvider
{
    private IList<Expression> _arguments;

    public MethodCallExpressionN(MethodInfo method, IList<Expression> args)
        : base(method)
    {
        _arguments = args;
    }

    Expression IArgumentProvider.GetArgument(int index)
    {
        return _arguments[index];
    }

    int IArgumentProvider.ArgumentCount => _arguments.Count;

    internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
    {
        return ReturnReadOnly(ref _arguments);
    }

    internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
    {
        return Call(Method, args ?? _arguments);
    }
}