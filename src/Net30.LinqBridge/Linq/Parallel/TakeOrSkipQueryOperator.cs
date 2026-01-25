#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class TakeOrSkipQueryOperator<TResult> : UnaryQueryOperator<TResult, TResult>
{
    private readonly int m_count;
    private readonly bool m_take;
    private bool m_prematureMerge;

    internal TakeOrSkipQueryOperator(IEnumerable<TResult> child, int count, bool take)
        : base(child)
    {
        m_count = count;
        m_take = take;
        SetOrdinalIndexState(OutputOrdinalIndexState());
    }

    internal override bool LimitsParallelism => false;

    internal override IEnumerable<TResult> AsSequentialQuery(CancellationToken token)
    {
        return m_take
            ? Child.AsSequentialQuery(token).Take(m_count)
            : CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token).Skip(m_count);
    }

    internal override QueryResults<TResult> Open(QuerySettings settings, bool preferStriping)
    {
        return TakeOrSkipQueryOperatorResults.NewResults(Child.Open(settings, true), this, settings, preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TResult, TKey> inputStream,
        IPartitionedStreamRecipient<TResult> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        if (m_prematureMerge)
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

    private OrdinalIndexState OutputOrdinalIndexState()
    {
        var state1 = Child.OrdinalIndexState;
        if (state1 == OrdinalIndexState.Indexible)
        {
            return OrdinalIndexState.Indexible;
        }

        if (state1.IsWorseThan(OrdinalIndexState.Increasing))
        {
            m_prematureMerge = true;
            state1 = OrdinalIndexState.Correct;
        }

        if (!m_take && state1 == OrdinalIndexState.Correct)
        {
            state1 = OrdinalIndexState.Increasing;
        }

        return state1;
    }

    private void WrapHelper<TKey>(
        PartitionedStream<TResult, TKey> inputStream,
        IPartitionedStreamRecipient<TResult> recipient,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var sharedIndices = new FixedMaxHeap<TKey>(m_count, inputStream.KeyComparer);
        var sharedBarrier = new CountdownEvent(partitionCount);
        var partitionedStream =
            new PartitionedStream<TResult, TKey>(partitionCount, inputStream.KeyComparer, OrdinalIndexState);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = new TakeOrSkipQueryOperatorEnumerator<TKey>(inputStream[index], m_take,
                sharedIndices, sharedBarrier, settings.CancellationState.MergedCancellationToken,
                inputStream.KeyComparer);
        }

        recipient.Receive(partitionedStream);
    }

    private class TakeOrSkipQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TResult, TKey>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly int m_count;
        private readonly IComparer<TKey> m_keyComparer;
        private readonly CountdownEvent m_sharedBarrier;
        private readonly FixedMaxHeap<TKey> m_sharedIndices;
        private readonly QueryOperatorEnumerator<TResult, TKey> m_source;
        private readonly bool m_take;
        private List<Pair<TResult, TKey>> m_buffer;
        private Shared<int> m_bufferIndex;

        internal TakeOrSkipQueryOperatorEnumerator(
            QueryOperatorEnumerator<TResult, TKey> source,
            bool take,
            FixedMaxHeap<TKey> sharedIndices,
            CountdownEvent sharedBarrier,
            CancellationToken cancellationToken,
            IComparer<TKey> keyComparer)
        {
            m_source = source;
            m_count = sharedIndices.Size;
            m_take = take;
            m_sharedIndices = sharedIndices;
            m_sharedBarrier = sharedBarrier;
            m_cancellationToken = cancellationToken;
            m_keyComparer = keyComparer;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TResult currentElement, ref TKey currentKey)
        {
            if (m_buffer == null && m_count > 0)
            {
                var pairList = new List<Pair<TResult, TKey>>();
                var currentElement1 = default(TResult);
                var currentKey1 = default(TKey);
                var num = 0;
                while (pairList.Count < m_count && m_source.MoveNext(ref currentElement1, ref currentKey1))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    pairList.Add(new Pair<TResult, TKey>(currentElement1, currentKey1));
                    lock (m_sharedIndices)
                    {
                        if (!m_sharedIndices.Insert(currentKey1))
                        {
                            break;
                        }
                    }
                }

                m_sharedBarrier.Signal();
                m_sharedBarrier.Wait(m_cancellationToken);
                m_buffer = pairList;
                m_bufferIndex = new Shared<int>(-1);
            }

            if (m_take)
            {
                if (m_count == 0 || m_bufferIndex.Value >= m_buffer.Count - 1)
                {
                    return false;
                }

                ++m_bufferIndex.Value;
                ref var local1 = ref currentElement;
                var pair = m_buffer[m_bufferIndex.Value];
                var first = pair.First;
                local1 = first;
                ref var local2 = ref currentKey;
                pair = m_buffer[m_bufferIndex.Value];
                var second1 = pair.Second;
                local2 = second1;
                if (m_sharedIndices.Count == 0)
                {
                    return true;
                }

                var keyComparer = m_keyComparer;
                pair = m_buffer[m_bufferIndex.Value];
                var second2 = pair.Second;
                var maxValue = m_sharedIndices.MaxValue;
                return keyComparer.Compare(second2, maxValue) <= 0;
            }

            var key = default(TKey);
            if (m_count > 0)
            {
                if (m_sharedIndices.Count < m_count)
                {
                    return false;
                }

                var maxValue = m_sharedIndices.MaxValue;
                if (m_bufferIndex.Value < m_buffer.Count - 1)
                {
                    for (++m_bufferIndex.Value; m_bufferIndex.Value < m_buffer.Count; ++m_bufferIndex.Value)
                    {
                        if (m_keyComparer.Compare(m_buffer[m_bufferIndex.Value].Second, maxValue) > 0)
                        {
                            ref var local3 = ref currentElement;
                            var pair = m_buffer[m_bufferIndex.Value];
                            var first = pair.First;
                            local3 = first;
                            ref var local4 = ref currentKey;
                            pair = m_buffer[m_bufferIndex.Value];
                            var second = pair.Second;
                            local4 = second;
                            return true;
                        }
                    }
                }
            }

            return m_source.MoveNext(ref currentElement, ref currentKey);
        }
    }

    private class TakeOrSkipQueryOperatorResults :
        UnaryQueryOperatorResults
    {
        private readonly int m_childCount;
        private readonly TakeOrSkipQueryOperator<TResult> m_takeOrSkipOp;

        private TakeOrSkipQueryOperatorResults(
            QueryResults<TResult> childQueryResults,
            TakeOrSkipQueryOperator<TResult> takeOrSkipOp,
            QuerySettings settings,
            bool preferStriping)
            : base(childQueryResults, takeOrSkipOp, settings, preferStriping)
        {
            m_takeOrSkipOp = takeOrSkipOp;
            m_childCount = m_childQueryResults.ElementsCount;
        }

        internal override bool IsIndexible => m_childCount >= 0;

        internal override int ElementsCount => m_takeOrSkipOp.m_take
            ? Math.Min(m_childCount, m_takeOrSkipOp.m_count)
            : Math.Max(m_childCount - m_takeOrSkipOp.m_count, 0);

        public static QueryResults<TResult> NewResults(
            QueryResults<TResult> childQueryResults,
            TakeOrSkipQueryOperator<TResult> op,
            QuerySettings settings,
            bool preferStriping)
        {
            return childQueryResults.IsIndexible
                ? new TakeOrSkipQueryOperatorResults(childQueryResults, op, settings, preferStriping)
                : (QueryResults<TResult>)new UnaryQueryOperatorResults(childQueryResults, op, settings, preferStriping);
        }

        internal override TResult GetElement(int index)
        {
            return m_takeOrSkipOp.m_take
                ? m_childQueryResults.GetElement(index)
                : m_childQueryResults.GetElement(m_takeOrSkipOp.m_count + index);
        }
    }
}