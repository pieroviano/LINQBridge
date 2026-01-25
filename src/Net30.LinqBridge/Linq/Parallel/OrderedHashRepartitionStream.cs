#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal class OrderedHashRepartitionStream<TInputOutput, THashKey, TOrderKey> :
    HashRepartitionStream<TInputOutput, THashKey, TOrderKey>
{
    internal OrderedHashRepartitionStream(
        PartitionedStream<TInputOutput, TOrderKey> inputStream,
        Func<TInputOutput, THashKey> hashKeySelector,
        IEqualityComparer<THashKey> hashKeyComparer,
        IEqualityComparer<TInputOutput> elementComparer,
        CancellationToken cancellationToken)
        : base(inputStream.PartitionCount, inputStream.KeyComparer, hashKeyComparer, elementComparer)
    {
        m_partitions =
            new OrderedHashRepartitionEnumerator<TInputOutput, THashKey, TOrderKey>[inputStream.PartitionCount];
        var barrier = new CountdownEvent(inputStream.PartitionCount);
        var valueExchangeMatrix =
            new ListChunk<Pair<TInputOutput, THashKey>>[inputStream.PartitionCount, inputStream.PartitionCount];
        var keyExchangeMatrix = new ListChunk<TOrderKey>[inputStream.PartitionCount, inputStream.PartitionCount];
        for (var index = 0; index < inputStream.PartitionCount; ++index)
        {
            m_partitions[index] = new OrderedHashRepartitionEnumerator<TInputOutput, THashKey, TOrderKey>(
                inputStream[index], inputStream.PartitionCount, index, hashKeySelector, this, barrier,
                valueExchangeMatrix, keyExchangeMatrix, cancellationToken);
        }
    }
}