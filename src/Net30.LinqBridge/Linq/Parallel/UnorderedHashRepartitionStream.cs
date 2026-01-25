#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal class UnorderedHashRepartitionStream<TInputOutput, THashKey, TIgnoreKey> :
    HashRepartitionStream<TInputOutput, THashKey, int>
{
    internal UnorderedHashRepartitionStream(
        PartitionedStream<TInputOutput, TIgnoreKey> inputStream,
        Func<TInputOutput, THashKey> keySelector,
        IEqualityComparer<THashKey> keyComparer,
        IEqualityComparer<TInputOutput> elementComparer,
        CancellationToken cancellationToken)
        : base(inputStream.PartitionCount, Util.GetDefaultComparer<int>(), keyComparer, elementComparer)
    {
        m_partitions = new HashRepartitionEnumerator<TInputOutput, THashKey, TIgnoreKey>[inputStream.PartitionCount];
        var barrier = new CountdownEvent(inputStream.PartitionCount);
        var valueExchangeMatrix =
            new ListChunk<Pair<TInputOutput, THashKey>>[inputStream.PartitionCount, inputStream.PartitionCount];
        for (var index = 0; index < inputStream.PartitionCount; ++index)
        {
            m_partitions[index] = new HashRepartitionEnumerator<TInputOutput, THashKey, TIgnoreKey>(inputStream[index],
                inputStream.PartitionCount, index, keySelector, this, barrier, valueExchangeMatrix, cancellationToken);
        }
    }
}