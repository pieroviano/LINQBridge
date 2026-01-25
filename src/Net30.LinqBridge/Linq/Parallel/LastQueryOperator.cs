#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class LastQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
{
    private readonly Func<TSource, bool> m_predicate;
    private readonly bool m_prematureMergeNeeded;

    internal LastQueryOperator(IEnumerable<TSource> child, Func<TSource, bool> predicate)
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
        var operatorState = new LastQueryOperatorState<TKey>();
        var sharedBarrier = new CountdownEvent(partitionCount);
        var partitionedStream = new PartitionedStream<TSource, int>(partitionCount, Util.GetDefaultComparer<int>(),
            OrdinalIndexState.Shuffled);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = new LastQueryOperatorEnumerator<TKey>(inputStream[index], m_predicate,
                operatorState, sharedBarrier, settings.CancellationState.MergedCancellationToken,
                inputStream.KeyComparer, index);
        }

        recipient.Receive(partitionedStream);
    }

    private class LastQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly IComparer<TKey> m_keyComparer;
        private readonly LastQueryOperatorState<TKey> m_operatorState;
        private readonly int m_partitionId;
        private readonly Func<TSource, bool> m_predicate;
        private readonly CountdownEvent m_sharedBarrier;
        private readonly QueryOperatorEnumerator<TSource, TKey> m_source;
        private bool m_alreadySearched;

        internal LastQueryOperatorEnumerator(
            QueryOperatorEnumerator<TSource, TKey> source,
            Func<TSource, bool> predicate,
            LastQueryOperatorState<TKey> operatorState,
            CountdownEvent sharedBarrier,
            CancellationToken cancelToken,
            IComparer<TKey> keyComparer,
            int partitionId)
        {
            m_source = source;
            m_predicate = predicate;
            m_operatorState = operatorState;
            m_sharedBarrier = sharedBarrier;
            m_cancellationToken = cancelToken;
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
            var x = default(TKey);
            var flag = false;
            try
            {
                var num = 0;
                var currentElement1 = default(TSource);
                var currentKey1 = default(TKey);
                while (m_source.MoveNext(ref currentElement1, ref currentKey1))
                {
                    if ((num & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (m_predicate == null || m_predicate(currentElement1))
                    {
                        source = currentElement1;
                        x = currentKey1;
                        flag = true;
                    }

                    ++num;
                }

                if (flag)
                {
                    lock (m_operatorState)
                    {
                        if (m_operatorState.m_partitionId != -1)
                        {
                            if (m_keyComparer.Compare(x, m_operatorState.m_key) <= 0)
                            {
                                goto label_19;
                            }
                        }

                        m_operatorState.m_partitionId = m_partitionId;
                        m_operatorState.m_key = x;
                    }
                }
            }
            finally
            {
                m_sharedBarrier.Signal();
            }

            label_19:
            m_alreadySearched = true;
            if (m_partitionId == m_operatorState.m_partitionId)
            {
                m_sharedBarrier.Wait(m_cancellationToken);
                if (m_operatorState.m_partitionId == m_partitionId)
                {
                    currentElement = source;
                    currentKey = 0;
                    return true;
                }
            }

            return false;
        }
    }

    private class LastQueryOperatorState<TKey>
    {
        internal TKey m_key;
        internal int m_partitionId = -1;
    }
}