#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class PartitionedStream<TElement, TKey>
{
    protected QueryOperatorEnumerator<TElement, TKey>[] m_partitions;

    internal PartitionedStream(
        int partitionCount,
        IComparer<TKey> keyComparer,
        OrdinalIndexState indexState)
    {
        m_partitions = new QueryOperatorEnumerator<TElement, TKey>[partitionCount];
        KeyComparer = keyComparer;
        OrdinalIndexState = indexState;
    }

    internal QueryOperatorEnumerator<TElement, TKey> this[int index]
    {
        get => m_partitions[index];
        set => m_partitions[index] = value;
    }

    public int PartitionCount => m_partitions.Length;

    internal IComparer<TKey> KeyComparer { get; }

    internal OrdinalIndexState OrdinalIndexState { get; }
}