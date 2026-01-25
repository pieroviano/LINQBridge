#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal abstract class GroupByQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey> :
    QueryOperatorEnumerator<IGrouping<TGroupKey, TElement>, TOrderKey>
{
    protected readonly CancellationToken m_cancellationToken;
    protected readonly IEqualityComparer<TGroupKey> m_keyComparer;
    protected readonly QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> m_source;
    private Mutables m_mutables;

    protected GroupByQueryOperatorEnumerator(
        QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source,
        IEqualityComparer<TGroupKey> keyComparer,
        CancellationToken cancellationToken)
    {
        m_source = source;
        m_keyComparer = keyComparer;
        m_cancellationToken = cancellationToken;
    }

    protected abstract HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>> BuildHashLookup();

    protected override void Dispose(bool disposing)
    {
        m_source.Dispose();
    }

    internal override bool MoveNext(
        ref IGrouping<TGroupKey, TElement> currentElement,
        ref TOrderKey currentKey)
    {
        var mutables = m_mutables;
        if (mutables == null)
        {
            mutables = m_mutables = new Mutables();
            mutables.m_hashLookup = BuildHashLookup();
            mutables.m_hashLookupIndex = -1;
        }

        if (++mutables.m_hashLookupIndex >= mutables.m_hashLookup.Count)
        {
            return false;
        }

        currentElement = new GroupByGrouping<TGroupKey, TElement>(mutables.m_hashLookup[mutables.m_hashLookupIndex]);
        return true;
    }

    private class Mutables
    {
        internal HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>> m_hashLookup;
        internal int m_hashLookupIndex;
    }
}