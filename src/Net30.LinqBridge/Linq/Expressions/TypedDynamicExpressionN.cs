#nullable disable
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

internal class TypedDynamicExpressionN : DynamicExpressionN
{
    internal TypedDynamicExpressionN(
        Type returnType,
        Type delegateType,
        CallSiteBinder binder,
        IList<Expression> arguments)
        : base(delegateType, binder, arguments)
    {
        Type = returnType;
    }

    public sealed override Type Type { get; }
}