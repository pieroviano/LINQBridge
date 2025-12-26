using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace System;

public static class Queriable
{
    /// <summary>Converts a generic <see cref="T:System.Collections.Generic.IEnumerable`1" /> to a generic <see cref="T:System.Linq.IQueryable`1" />.</summary>
    /// <param name="source">A sequence to convert.</param>
    /// <typeparam name="TElement">The type of the elements of <paramref name="source" />.</typeparam>
    /// <returns>An <see cref="T:System.Linq.IQueryable`1" /> that represents the input sequence.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="source" /> is <see langword="null" />.</exception>
    public static IQueryable<TElement> AsQueryable<TElement>(
        this IEnumerable<TElement> source)
    {
        if (source == null)
            throw Error.ArgumentNull(nameof(source));
        return source is IQueryable<TElement> ? (IQueryable<TElement>)source : new EnumerableQuery<TElement>(source);
    }

}