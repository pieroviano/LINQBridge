#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class FirstQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
{
    private readonly Func<TSource, bool> m_predicate;
    private readonly bool m_prematureMergeNeeded;

    internal FirstQueryOperator(IEnumerable<TSource> child, Func<TSource, bool> predicate)
        : base(child)
    {
        m_predicate = predicate;
        m_prematureMergeNeeded = Child.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);
    }

    internal override bool LimitsParallelism => false;

    internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
    {
        throw new NotSupportedException();
    }

    internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, false), this, settings, preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TSource, TKey> inputStream,
        IPartitionedStreamRecipient<TSource> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        if (m_prematureMergeNeeded)
        {
            WrapHelper(
                ExecuteAndCollectResults(inputStream, inputStream.PartitionCount, Child.OutputOrdered, preferStriping,
                    settings).GetPartitionedStream(), recipient, settings);
        }
        else
        {
            WrapHelper(inputStream, recipient, settings);
        }
    }

    private void WrapHelper<TKey>(
        PartitionedStream<TSource, TKey> inputStream,
        IPartitionedStreamRecipient<TSource> recipient,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var operatorState = new FirstQueryOperatorState<TKey>();
        var sharedBarrier = new CountdownEvent(partitionCount);
        var partitionedStream = new PartitionedStream<TSource, int>(partitionCount, Util.GetDefaultComparer<int>(),
            OrdinalIndexState.Shuffled);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = new FirstQueryOperatorEnumerator<TKey>(inputStream[index], m_predicate,
                operatorState, sharedBarrier, settings.CancellationState.MergedCancellationToken,
                inputStream.KeyComparer, index);
        }

        recipient.Receive(partitionedStream);
    }

    private class FirstQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly IComparer<TKey> m_keyComparer;
        private readonly FirstQueryOperatorState<TKey> m_operatorState;
        private readonly int m_partitionId;
        private readonly Func<TSource, bool> m_predicate;
        private readonly CountdownEvent m_sharedBarrier;
        private readonly QueryOperatorEnumerator<TSource, TKey> m_source;
        private bool m_alreadySearched;

        internal FirstQueryOperatorEnumerator(
            QueryOperatorEnumerator<TSource, TKey> source,
            Func<TSource, bool> predicate,
            FirstQueryOperatorState<TKey> operatorState,
            CountdownEvent sharedBarrier,
            CancellationToken cancellationToken,
            IComparer<TKey> keyComparer,
            int partitionId)
        {
            m_source = source;
            m_predicate = predicate;
            m_operatorState = operatorState;
            m_sharedBarrier = sharedBarrier;
            m_cancellationToken = cancellationToken;
            m_keyComparer = keyComparer;
            m_partitionId = partitionId;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TSource currentElement, ref int currentKey)
        {
            if (m_alreadySearched)
            {
                return false;
            }

            var source = default(TSource);
            var key = default(TKey);
            try
            {
                var currentElement1 = default(TSource);
                var currentKey1 = default(TKey);
                var num = 0;
                while (m_source.MoveNext(ref currentElement1, ref currentKey1))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (m_predicate == null || m_predicate(currentElement1))
                    {
                        source = currentElement1;
                        var x = currentKey1;
                        lock (m_operatorState)
                        {
                            if (m_operatorState.m_partitionId != -1)
                            {
                                if (m_keyComparer.Compare(x, m_operatorState.m_key) >= 0)
                                {
                                    break;
                                }
                            }

                            m_operatorState.m_key = x;
                            m_operatorState.m_partitionId = m_partitionId;
                            break;
                        }
                    }
                }
            }
            finally
            {
                m_sharedBarrier.Signal();
            }

            m_alreadySearched = true;
            if (m_partitionId == m_operatorState.m_partitionId)
            {
                m_sharedBarrier.Wait(m_cancellationToken);
                if (m_partitionId == m_operatorState.m_partitionId)
                {
                    currentElement = source;
                    currentKey = 0;
                    return true;
                }
            }

            return false;
        }
    }

    private class FirstQueryOperatorState<TKey>
    {
        internal TKey m_key;
        internal int m_partitionId = -1;
    }
}