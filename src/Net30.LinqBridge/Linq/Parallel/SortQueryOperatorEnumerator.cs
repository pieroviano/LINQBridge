#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class SortQueryOperatorEnumerator<TInputOutput, TKey, TSortKey> :
    QueryOperatorEnumerator<TInputOutput, TSortKey>
{
    private readonly Func<TInputOutput, TSortKey> m_keySelector;
    private readonly QueryOperatorEnumerator<TInputOutput, TKey> m_source;

    internal SortQueryOperatorEnumerator(
        QueryOperatorEnumerator<TInputOutput, TKey> source,
        Func<TInputOutput, TSortKey> keySelector,
        IComparer<TSortKey> keyComparer)
    {
        m_source = source;
        m_keySelector = keySelector;
        KeyComparer = keyComparer;
    }

    public IComparer<TSortKey> KeyComparer { get; }

    protected override void Dispose(bool disposing)
    {
        m_source.Dispose();
    }

    internal override bool MoveNext(ref TInputOutput currentElement, ref TSortKey currentKey)
    {
        var currentKey1 = default(TKey);
        if (!m_source.MoveNext(ref currentElement, ref currentKey1))
        {
            return false;
        }

        currentKey = m_keySelector(currentElement);
        return true;
    }
}