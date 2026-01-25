#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Linq.Expressions;

internal class ScopeWithType : ScopeN
{
    internal ScopeWithType(
        IList<ParameterExpression> variables,
        IList<Expression> expressions,
        Type type)
        : base(variables, expressions)
    {
        Type = type;
    }

    public sealed override Type Type { get; }

    internal override BlockExpression Rewrite(
        ReadOnlyCollection<ParameterExpression> variables,
        Expression[] args)
    {
        return new ScopeWithType(ReuseOrValidateVariables(variables), args, Type);
    }
}