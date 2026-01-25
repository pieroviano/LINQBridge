#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler;

internal sealed class HoistedLocals
{
    internal readonly ReadOnlyDictionary<Expression, int> Indexes;
    internal readonly HoistedLocals Parent;
    internal readonly ParameterExpression SelfVariable;
    internal readonly ReadOnlyCollection<ParameterExpression> Variables;

    internal HoistedLocals(HoistedLocals parent, ReadOnlyCollection<ParameterExpression> vars)
    {
        if (parent != null)
        {
            vars = new TrueReadOnlyCollection<ParameterExpression>(vars.AddFirst(parent.SelfVariable));
        }

        var dictionary = new Dictionary<Expression, int>(vars.Count);
        for (var index = 0; index < vars.Count; ++index)
        {
            dictionary.Add(vars[index], index);
        }

        SelfVariable = Expression.Variable(typeof(object[]), null);
        Parent = parent;
        Variables = vars;
        Indexes = new ReadOnlyDictionary<Expression, int>(dictionary);
    }

    internal ParameterExpression ParentVariable => Parent == null ? null : Parent.SelfVariable;

    internal static object[] GetParent(object[] locals)
    {
        return ((StrongBox<object[]>)locals[0]).Value;
    }
}