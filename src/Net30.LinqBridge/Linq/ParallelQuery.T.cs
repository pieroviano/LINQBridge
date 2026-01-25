#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Linq.Parallel;

namespace System.Linq;

/// <summary>Represents a parallel sequence.</summary>
/// <typeparam name="TSource">The type of element in the source sequence.</typeparam>
public class ParallelQuery<TSource> : ParallelQuery, IEnumerable<TSource>, IEnumerable
{
    internal ParallelQuery(QuerySettings settings)
        : base(settings)
    {
    }

    /// <summary>Returns an enumerator that iterates through the sequence.</summary>
    /// <returns>An enumerator that iterates through the sequence.</returns>
    public virtual IEnumerator<TSource> GetEnumerator()
    {
        throw new NotSupportedException();
    }

    internal sealed override ParallelQuery<TCastTo> Cast<TCastTo>()
    {
        return this.Select(elem => (TCastTo)(object)elem);
    }

    internal override IEnumerator GetEnumeratorUntyped()
    {
        return GetEnumerator();
    }

    internal sealed override ParallelQuery<TCastTo> OfType<TCastTo>()
    {
        return this.Where(elem => (object)elem is TCastTo).Select(elem => (TCastTo)(object)elem);
    }
}