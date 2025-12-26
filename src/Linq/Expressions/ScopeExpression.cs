using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Linq.Expressions;

internal class ScopeExpression : BlockExpression
{
    private IList<ParameterExpression> _variables;

    internal override int VariableCount => _variables.Count;

    protected IList<ParameterExpression> VariablesList => _variables;

    internal ScopeExpression(IList<ParameterExpression> variables): base(ExpressionType.Block,typeof(object))
    {
        _variables = variables;
    }

    internal override ReadOnlyCollection<ParameterExpression> GetOrMakeVariables()
    {
        return ReturnReadOnly<ParameterExpression>(ref _variables);
    }

    internal override ParameterExpression GetVariable(int index)
    {
        return _variables[index];
    }

    internal IList<ParameterExpression> ReuseOrValidateVariables(ReadOnlyCollection<ParameterExpression> variables)
    {
        if (variables == null || variables == VariablesList)
        {
            return VariablesList;
        }
        ValidateVariables(variables, "variables");
        return variables;
    }
}