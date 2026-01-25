#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DefaultIfEmptyQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
{
    private readonly TSource m_defaultValue;

    internal DefaultIfEmptyQueryOperator(IEnumerable<TSource> child, TSource defaultValue)
        : base(child)
    {
        m_defaultValue = defaultValue;
        SetOrdinalIndexState(Child.OrdinalIndexState.Worse(OrdinalIndexState.Correct));
    }

    internal override bool LimitsParallelism => false;

    internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
    {
        return Child.AsSequentialQuery(token).DefaultIfEmpty(m_defaultValue);
    }

    internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, preferStriping), this, settings, preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TSource, TKey> inputStream,
        IPartitionedStreamRecipient<TSource> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var sharedEmptyCount = new Shared<int>(0);
        var sharedLatch = new CountdownEvent(partitionCount - 1);
        var partitionedStream =
            new PartitionedStream<TSource, TKey>(partitionCount, inputStream.KeyComparer, OrdinalIndexState);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = new DefaultIfEmptyQueryOperatorEnumerator<TKey>(inputStream[index],
                m_defaultValue, index, partitionCount, sharedEmptyCount, sharedLatch,
                settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream);
    }

    private class DefaultIfEmptyQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, TKey>
    {
        private readonly CancellationToken m_cancelToken;
        private readonly TSource m_defaultValue;
        private readonly int m_partitionCount;
        private readonly int m_partitionIndex;
        private readonly Shared<int> m_sharedEmptyCount;
        private readonly CountdownEvent m_sharedLatch;
        private readonly QueryOperatorEnumerator<TSource, TKey> m_source;
        private bool m_lookedForEmpty;

        internal DefaultIfEmptyQueryOperatorEnumerator(
            QueryOperatorEnumerator<TSource, TKey> source,
            TSource defaultValue,
            int partitionIndex,
            int partitionCount,
            Shared<int> sharedEmptyCount,
            CountdownEvent sharedLatch,
            CancellationToken cancelToken)
        {
            m_source = source;
            m_defaultValue = defaultValue;
            m_partitionIndex = partitionIndex;
            m_partitionCount = partitionCount;
            m_sharedEmptyCount = sharedEmptyCount;
            m_sharedLatch = sharedLatch;
            m_cancelToken = cancelToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TSource currentElement, ref TKey currentKey)
        {
            var flag = m_source.MoveNext(ref currentElement, ref currentKey);
            if (!m_lookedForEmpty)
            {
                m_lookedForEmpty = true;
                if (!flag)
                {
                    if (m_partitionIndex == 0)
                    {
                        m_sharedLatch.Wait(m_cancelToken);
                        m_sharedLatch.Dispose();
                        if (m_sharedEmptyCount.Value != m_partitionCount - 1)
                        {
                            return false;
                        }

                        currentElement = m_defaultValue;
                        currentKey = default;
                        return true;
                    }

                    Interlocked.Increment(ref m_sharedEmptyCount.Value);
                }

                if (m_partitionIndex != 0)
                {
                    m_sharedLatch.Signal();
                }
            }

            return flag;
        }
    }
}