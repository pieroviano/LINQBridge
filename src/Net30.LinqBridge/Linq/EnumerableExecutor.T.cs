#nullable disable
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq;

/// <summary>Represents an expression tree and provides functionality to execute the expression tree after rewriting it.</summary>
/// <typeparam name="T">The data type of the value that results from executing the expression tree.</typeparam>
public class EnumerableExecutor<T> : EnumerableExecutor
{
    private readonly Expression expression;
    private Func<T> func;

    /// <summary>Initializes a new instance of the <see cref="T:System.Linq.EnumerableExecutor`1" /> class.</summary>
    /// <param name="expression">An expression tree to associate with the new instance.</param>
    public EnumerableExecutor(Expression expression)
    {
        this.expression = expression;
    }

    internal T Execute()
    {
        if (func == null)
        {
            func = Expression.Lambda<Func<T>>(new EnumerableRewriter().Visit(expression),
                (IEnumerable<ParameterExpression>)null).Compile();
        }

        return func();
    }

    internal override object ExecuteBoxed()
    {
        return Execute();
    }
}