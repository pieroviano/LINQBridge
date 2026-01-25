#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class GroupByElementSelectorQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey> :
    GroupByQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey>
{
    private readonly Func<TSource, TElement> m_elementSelector;

    internal GroupByElementSelectorQueryOperatorEnumerator(
        QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source,
        IEqualityComparer<TGroupKey> keyComparer,
        Func<TSource, TElement> elementSelector,
        CancellationToken cancellationToken)
        : base(source, keyComparer, cancellationToken)
    {
        m_elementSelector = elementSelector;
    }

    protected override HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>> BuildHashLookup()
    {
        var hashLookup =
            new HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>>(
                new WrapperEqualityComparer<TGroupKey>(m_keyComparer));
        var currentElement = new Pair<TSource, TGroupKey>();
        var currentKey = default(TOrderKey);
        var num = 0;
        while (m_source.MoveNext(ref currentElement, ref currentKey))
        {
            if ((num++ & 63 /*0x3F*/) == 0)
            {
                CancellationState.ThrowIfCanceled(m_cancellationToken);
            }

            var key = new Wrapper<TGroupKey>(currentElement.Second);
            var listChunk = (ListChunk<TElement>)null;
            if (!hashLookup.TryGetValue(key, ref listChunk))
            {
                listChunk = new ListChunk<TElement>(2);
                hashLookup.Add(key, listChunk);
            }

            listChunk.Add(m_elementSelector(currentElement.First));
        }

        return hashLookup;
    }
}