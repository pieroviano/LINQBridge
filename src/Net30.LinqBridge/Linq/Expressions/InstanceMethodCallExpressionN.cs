#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Linq.Expressions;

internal class InstanceMethodCallExpressionN : MethodCallExpression, IArgumentProvider
{
    private readonly Expression _instance;
    private IList<Expression> _arguments;

    public InstanceMethodCallExpressionN(
        MethodInfo method,
        Expression instance,
        IList<Expression> args)
        : base(method)
    {
        _instance = instance;
        _arguments = args;
    }

    Expression IArgumentProvider.GetArgument(int index)
    {
        return _arguments[index];
    }

    int IArgumentProvider.ArgumentCount => _arguments.Count;

    internal override Expression GetInstance()
    {
        return _instance;
    }

    internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
    {
        return ReturnReadOnly(ref _arguments);
    }

    internal override MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
    {
        return Call(instance, Method, args ?? _arguments);
    }
}