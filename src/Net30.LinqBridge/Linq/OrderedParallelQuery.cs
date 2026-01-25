#nullable disable
using System.Collections.Generic;
using System.Linq.Parallel;

namespace System.Linq;

/// <summary>Represents a sorted, parallel sequence.</summary>
/// <typeparam name="TSource">The type of elements in the source collection.</typeparam>
public class OrderedParallelQuery<TSource> : ParallelQuery<TSource>
{
    internal OrderedParallelQuery(QueryOperator<TSource> sortOp)
        : base(sortOp.SpecifiedQuerySettings)
    {
        SortOperator = sortOp;
    }

    internal QueryOperator<TSource> SortOperator { get; }

    internal IOrderedEnumerable<TSource> OrderedEnumerable => (IOrderedEnumerable<TSource>)SortOperator;

    /// <summary>Returns an enumerator that iterates through the sequence.</summary>
    /// <returns>An enumerator that iterates through the sequence.</returns>
    public override IEnumerator<TSource> GetEnumerator()
    {
        return SortOperator.GetEnumerator();
    }
}