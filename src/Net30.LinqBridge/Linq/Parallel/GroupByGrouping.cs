#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class GroupByGrouping<TGroupKey, TElement> :
    IGrouping<TGroupKey, TElement>,
    IEnumerable<TElement>,
    IEnumerable
{
    private KeyValuePair<Wrapper<TGroupKey>, ListChunk<TElement>> m_keyValues;

    internal GroupByGrouping(
        KeyValuePair<Wrapper<TGroupKey>, ListChunk<TElement>> keyValues)
    {
        m_keyValues = keyValues;
    }

    TGroupKey IGrouping<TGroupKey, TElement>.Key => m_keyValues.Key.Value;

    IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator()
    {
        return m_keyValues.Value.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<TElement>)this).GetEnumerator();
    }
}