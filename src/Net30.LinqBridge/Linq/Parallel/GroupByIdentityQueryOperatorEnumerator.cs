#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class GroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TOrderKey> :
    GroupByQueryOperatorEnumerator<TSource, TGroupKey, TSource, TOrderKey>
{
    internal GroupByIdentityQueryOperatorEnumerator(
        QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source,
        IEqualityComparer<TGroupKey> keyComparer,
        CancellationToken cancellationToken)
        : base(source, keyComparer, cancellationToken)
    {
    }

    protected override HashLookup<Wrapper<TGroupKey>, ListChunk<TSource>> BuildHashLookup()
    {
        var hashLookup =
            new HashLookup<Wrapper<TGroupKey>, ListChunk<TSource>>(
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
            var listChunk = (ListChunk<TSource>)null;
            if (!hashLookup.TryGetValue(key, ref listChunk))
            {
                listChunk = new ListChunk<TSource>(2);
                hashLookup.Add(key, listChunk);
            }

            listChunk.Add(currentElement.First);
        }

        return hashLookup;
    }
}