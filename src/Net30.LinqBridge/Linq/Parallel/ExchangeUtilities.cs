#nullable disable
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace System.Linq.Parallel;

internal static class ExchangeUtilities
{
    internal static PartitionedStream<Pair<TElement, THashKey>, int> HashRepartition<TElement, THashKey, TIgnoreKey>(
        PartitionedStream<TElement, TIgnoreKey> source,
        Func<TElement, THashKey> keySelector,
        IEqualityComparer<THashKey> keyComparer,
        IEqualityComparer<TElement> elementComparer,
        CancellationToken cancellationToken)
    {
        return new UnorderedHashRepartitionStream<TElement, THashKey, TIgnoreKey>(source, keySelector, keyComparer,
            elementComparer, cancellationToken);
    }

    internal static PartitionedStream<Pair<TElement, THashKey>, TOrderKey> HashRepartitionOrdered<TElement, THashKey,
        TOrderKey>(
        PartitionedStream<TElement, TOrderKey> source,
        Func<TElement, THashKey> keySelector,
        IEqualityComparer<THashKey> keyComparer,
        IEqualityComparer<TElement> elementComparer,
        CancellationToken cancellationToken)
    {
        return new OrderedHashRepartitionStream<TElement, THashKey, TOrderKey>(source, keySelector, keyComparer,
            elementComparer, cancellationToken);
    }

    internal static bool IsWorseThan(this OrdinalIndexState state1, OrdinalIndexState state2)
    {
        return state1 > state2;
    }

    internal static PartitionedStream<T, int> PartitionDataSource<T>(
        IEnumerable<T> source,
        int partitionCount,
        bool useStriping)
    {
        PartitionedStream<T, int> partitionedStream1;
        if (source is IParallelPartitionable<T> parallelPartitionable)
        {
            var partitions = parallelPartitionable.GetPartitions(partitionCount);
            if (partitions == null)
            {
                throw new InvalidOperationException(Strings.ParallelPartitionable_NullReturn());
            }

            if (partitions.Length != partitionCount)
            {
                throw new InvalidOperationException(Strings.ParallelPartitionable_IncorretElementCount());
            }

            var partitionedStream2 = new PartitionedStream<T, int>(partitionCount, Util.GetDefaultComparer<int>(),
                OrdinalIndexState.Correct);
            for (var index = 0; index < partitionCount; ++index)
            {
                partitionedStream2[index] = partitions[index] ??
                                            throw new InvalidOperationException(
                                                Strings.ParallelPartitionable_NullElement());
            }

            partitionedStream1 = partitionedStream2;
        }
        else
        {
            partitionedStream1 = new PartitionedDataSource<T>(source, partitionCount, useStriping);
        }

        return partitionedStream1;
    }

    internal static OrdinalIndexState Worse(this OrdinalIndexState state1, OrdinalIndexState state2)
    {
        return state1 <= state2 ? state2 : state1;
    }
}