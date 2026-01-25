#nullable disable
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq;

/// <summary>
///     Represents an <see cref="T:System.Collections.IEnumerable" /> as an
///     <see cref="T:System.Linq.EnumerableQuery" /> data source.
/// </summary>
public abstract class EnumerableQuery
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Linq.EnumerableQuery" /> class.</summary>
    protected EnumerableQuery()
    {
    }

    internal abstract Expression Expression { get; }

    internal abstract IEnumerable Enumerable { get; }

    internal static IQueryable Create(Type elementType, IEnumerable sequence)
    {
        return (IQueryable)Activator.CreateInstance(typeof(EnumerableQuery<>).MakeGenericType(elementType),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[1]
            {
                sequence
            }, null);
    }

    internal static IQueryable Create(Type elementType, Expression expression)
    {
        return (IQueryable)Activator.CreateInstance(typeof(EnumerableQuery<>).MakeGenericType(elementType),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[1]
            {
                expression
            }, null);
    }
}