#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

internal class DynamicExpressionN : DynamicExpression, IArgumentProvider
{
    private IList<Expression> _arguments;

    internal DynamicExpressionN(
        Type delegateType,
        CallSiteBinder binder,
        IList<Expression> arguments)
        : base(delegateType, binder)
    {
        _arguments = arguments;
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

    internal override DynamicExpression Rewrite(Expression[] args)
    {
        return Expression.MakeDynamic(DelegateType, Binder, args);
    }
}