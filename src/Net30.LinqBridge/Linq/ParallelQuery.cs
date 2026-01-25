#nullable disable
using System.Collections;
using System.Linq.Parallel;

namespace System.Linq;

/// <summary>Represents a parallel sequence.</summary>
public class ParallelQuery : IEnumerable
{
    internal ParallelQuery(QuerySettings specifiedSettings)
    {
        SpecifiedQuerySettings = specifiedSettings;
    }

    internal QuerySettings SpecifiedQuerySettings { get; }

    /// <summary>Returns an enumerator that iterates through the sequence.</summary>
    /// <returns>An enumerator that iterates through the sequence.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumeratorUntyped();
    }

    internal virtual ParallelQuery<TCastTo> Cast<TCastTo>()
    {
        throw new NotSupportedException();
    }

    internal virtual IEnumerator GetEnumeratorUntyped()
    {
        throw new NotSupportedException();
    }

    internal virtual ParallelQuery<TCastTo> OfType<TCastTo>()
    {
        throw new NotSupportedException();
    }
}