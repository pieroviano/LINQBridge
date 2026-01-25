#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class OrderedGroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TOrderKey> :
    OrderedGroupByQueryOperatorEnumerator<TSource, TGroupKey, TSource, TOrderKey>
{
    internal OrderedGroupByIdentityQueryOperatorEnumerator(
        QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source,
        Func<TSource, TGroupKey> keySelector,
        IEqualityComparer<TGroupKey> keyComparer,
        IComparer<TOrderKey> orderComparer,
        CancellationToken cancellationToken)
        : base(source, keySelector, keyComparer, orderComparer, cancellationToken)
    {
    }

    protected override HashLookup<Wrapper<TGroupKey>, GroupKeyData> BuildHashLookup()
    {
        var hashLookup =
            new HashLookup<Wrapper<TGroupKey>, GroupKeyData>(new WrapperEqualityComparer<TGroupKey>(m_keyComparer));
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
            var groupKeyData = (GroupKeyData)null;
            if (hashLookup.TryGetValue(key, ref groupKeyData))
            {
                if (m_orderComparer.Compare(currentKey, groupKeyData.m_orderKey) < 0)
                {
                    groupKeyData.m_orderKey = currentKey;
                }
            }
            else
            {
                groupKeyData = new GroupKeyData(currentKey, key.Value, m_orderComparer);
                hashLookup.Add(key, groupKeyData);
            }

            groupKeyData.m_grouping.Add(currentElement.First, currentKey);
        }

        for (var index = 0; index < hashLookup.Count; ++index)
        {
            hashLookup[index].Value.m_grouping.DoneAdding();
        }

        return hashLookup;
    }
}