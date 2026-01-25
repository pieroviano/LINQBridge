#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal class QueryExecutionOption<TSource> : QueryOperator<TSource>
{
    private readonly QueryOperator<TSource> m_child;
    private readonly OrdinalIndexState m_indexState;

    internal QueryExecutionOption(QueryOperator<TSource> source, QuerySettings settings)
        : base(source.OutputOrdered, settings.Merge(source.SpecifiedQuerySettings))
    {
        m_child = source;
        m_indexState = m_child.OrdinalIndexState;
    }

    internal override OrdinalIndexState OrdinalIndexState => m_indexState;

    internal override bool LimitsParallelism => m_child.LimitsParallelism;

    internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
    {
        return m_child.AsSequentialQuery(token);
    }

    internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
    {
        return m_child.Open(settings, preferStriping);
    }
}