#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class OrderingQueryOperator<TSource> : QueryOperator<TSource>
{
    private readonly QueryOperator<TSource> m_child;
    private bool m_orderOn;

    public OrderingQueryOperator(QueryOperator<TSource> child, bool orderOn)
        : base(orderOn, child.SpecifiedQuerySettings)
    {
        m_child = child;
        OrdinalIndexState = m_child.OrdinalIndexState;
        m_orderOn = orderOn;
    }

    internal override bool LimitsParallelism => m_child.LimitsParallelism;

    internal override OrdinalIndexState OrdinalIndexState { get; }

    internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
    {
        return m_child.AsSequentialQuery(token);
    }

    internal override IEnumerator<TSource> GetEnumerator(
        ParallelMergeOptions? mergeOptions,
        bool suppressOrderPreservation)
    {
        return m_child is ScanQueryOperator<TSource> child
            ? child.Data.GetEnumerator()
            : base.GetEnumerator(mergeOptions, suppressOrderPreservation);
    }

    internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
    {
        return m_child.Open(settings, preferStriping);
    }
}