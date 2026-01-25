#nullable disable
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq;

/// <summary>Represents an expression tree and provides functionality to execute the expression tree after rewriting it.</summary>
public abstract class EnumerableExecutor
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Linq.EnumerableExecutor" /> class.</summary>
    protected EnumerableExecutor()
    {
    }

    internal static EnumerableExecutor Create(Expression expression)
    {
        return (EnumerableExecutor)Activator.CreateInstance(
            typeof(EnumerableExecutor<>).MakeGenericType(expression.Type),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[1]
            {
                expression
            }, null);
    }

    internal abstract object ExecuteBoxed();
}