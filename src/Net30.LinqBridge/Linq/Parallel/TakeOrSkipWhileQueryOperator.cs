#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class TakeOrSkipWhileQueryOperator<TResult> : UnaryQueryOperator<TResult, TResult>
{
    private readonly Func<TResult, int, bool> m_indexedPredicate;
    private readonly Func<TResult, bool> m_predicate;
    private readonly bool m_take;
    private bool m_limitsParallelism;
    private bool m_prematureMerge;

    internal TakeOrSkipWhileQueryOperator(
        IEnumerable<TResult> child,
        Func<TResult, bool> predicate,
        Func<TResult, int, bool> indexedPredicate,
        bool take)
        : base(child)
    {
        m_predicate = predicate;
        m_indexedPredicate = indexedPredicate;
        m_take = take;
        InitOrderIndexState();
    }

    internal override bool LimitsParallelism => m_limitsParallelism;

    internal override IEnumerable<TResult> AsSequentialQuery(CancellationToken token)
    {
        return m_take
            ?
            m_indexedPredicate != null
                ? Child.AsSequentialQuery(token).TakeWhile(m_indexedPredicate)
                : Child.AsSequentialQuery(token).TakeWhile(m_predicate)
            : m_indexedPredicate != null
                ? CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token).SkipWhile(m_indexedPredicate)
                : CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token).SkipWhile(m_predicate);
    }

    internal override QueryResults<TResult> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, true), this, settings, preferStriping);
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

    private void InitOrderIndexState()
    {
        var state2 = OrdinalIndexState.Increasing;
        var ordinalIndexState1 = Child.OrdinalIndexState;
        if (m_indexedPredicate != null)
        {
            state2 = OrdinalIndexState.Correct;
            m_limitsParallelism = ordinalIndexState1 == OrdinalIndexState.Increasing;
        }

        var ordinalIndexState2 = ordinalIndexState1.Worse(OrdinalIndexState.Correct);
        if (ordinalIndexState2.IsWorseThan(state2))
        {
            m_prematureMerge = true;
        }

        if (!m_take)
        {
            ordinalIndexState2 = ordinalIndexState2.Worse(OrdinalIndexState.Increasing);
        }

        SetOrdinalIndexState(ordinalIndexState2);
    }

    private void WrapHelper<TKey>(
        PartitionedStream<TResult, TKey> inputStream,
        IPartitionedStreamRecipient<TResult> recipient,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var operatorState = new OperatorState<TKey>();
        var sharedBarrier = new CountdownEvent(partitionCount);
        var indexedPredicate = (Func<TResult, TKey, bool>)(object)m_indexedPredicate;
        var partitionedStream =
            new PartitionedStream<TResult, TKey>(partitionCount, inputStream.KeyComparer, OrdinalIndexState);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = new TakeOrSkipWhileQueryOperatorEnumerator<TKey>(inputStream[index], m_predicate,
                indexedPredicate, m_take, operatorState, sharedBarrier,
                settings.CancellationState.MergedCancellationToken, inputStream.KeyComparer);
        }

        recipient.Receive(partitionedStream);
    }

    private class TakeOrSkipWhileQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TResult, TKey>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly Func<TResult, TKey, bool> m_indexedPredicate;
        private readonly IComparer<TKey> m_keyComparer;
        private readonly OperatorState<TKey> m_operatorState;
        private readonly Func<TResult, bool> m_predicate;
        private readonly CountdownEvent m_sharedBarrier;
        private readonly QueryOperatorEnumerator<TResult, TKey> m_source;
        private readonly bool m_take;
        private List<Pair<TResult, TKey>> m_buffer;
        private Shared<int> m_bufferIndex;
        private TKey m_currentLowKey;
        private int m_updatesSeen;

        internal TakeOrSkipWhileQueryOperatorEnumerator(
            QueryOperatorEnumerator<TResult, TKey> source,
            Func<TResult, bool> predicate,
            Func<TResult, TKey, bool> indexedPredicate,
            bool take,
            OperatorState<TKey> operatorState,
            CountdownEvent sharedBarrier,
            CancellationToken cancelToken,
            IComparer<TKey> keyComparer)
        {
            m_source = source;
            m_predicate = predicate;
            m_indexedPredicate = indexedPredicate;
            m_take = take;
            m_operatorState = operatorState;
            m_sharedBarrier = sharedBarrier;
            m_cancellationToken = cancelToken;
            m_keyComparer = keyComparer;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TResult currentElement, ref TKey currentKey)
        {
            if (m_buffer == null)
            {
                var pairList = new List<Pair<TResult, TKey>>();
                try
                {
                    var currentElement1 = default(TResult);
                    var currentKey1 = default(TKey);
                    var num = 0;
                    while (m_source.MoveNext(ref currentElement1, ref currentKey1))
                    {
                        if ((num++ & 63 /*0x3F*/) == 0)
                        {
                            CancellationState.ThrowIfCanceled(m_cancellationToken);
                        }

                        pairList.Add(new Pair<TResult, TKey>(currentElement1, currentKey1));
                        if (m_updatesSeen != m_operatorState.m_updatesDone)
                        {
                            lock (m_operatorState)
                            {
                                m_currentLowKey = m_operatorState.m_currentLowKey;
                                m_updatesSeen = m_operatorState.m_updatesDone;
                            }
                        }

                        if (m_updatesSeen > 0)
                        {
                            if (m_keyComparer.Compare(currentKey1, m_currentLowKey) > 0)
                            {
                                break;
                            }
                        }

                        if (!(m_predicate == null
                                ? m_indexedPredicate(currentElement1, currentKey1)
                                : m_predicate(currentElement1)))
                        {
                            lock (m_operatorState)
                            {
                                if (m_operatorState.m_updatesDone != 0)
                                {
                                    if (m_keyComparer.Compare(m_operatorState.m_currentLowKey, currentKey1) <= 0)
                                    {
                                        break;
                                    }
                                }

                                m_currentLowKey = m_operatorState.m_currentLowKey = currentKey1;
                                m_updatesSeen = ++m_operatorState.m_updatesDone;
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    m_sharedBarrier.Signal();
                }

                m_sharedBarrier.Wait(m_cancellationToken);
                m_buffer = pairList;
                m_bufferIndex = new Shared<int>(-1);
            }

            if (m_take)
            {
                if (m_bufferIndex.Value >= m_buffer.Count - 1)
                {
                    return false;
                }

                ++m_bufferIndex.Value;
                currentElement = m_buffer[m_bufferIndex.Value].First;
                currentKey = m_buffer[m_bufferIndex.Value].Second;
                return m_operatorState.m_updatesDone == 0 ||
                       m_keyComparer.Compare(m_operatorState.m_currentLowKey, currentKey) > 0;
            }

            if (m_operatorState.m_updatesDone == 0)
            {
                return false;
            }

            if (m_bufferIndex.Value < m_buffer.Count - 1)
            {
                for (++m_bufferIndex.Value; m_bufferIndex.Value < m_buffer.Count; ++m_bufferIndex.Value)
                {
                    if (m_keyComparer.Compare(m_buffer[m_bufferIndex.Value].Second, m_operatorState.m_currentLowKey) >=
                        0)
                    {
                        ref var local1 = ref currentElement;
                        var pair = m_buffer[m_bufferIndex.Value];
                        var first = pair.First;
                        local1 = first;
                        ref var local2 = ref currentKey;
                        pair = m_buffer[m_bufferIndex.Value];
                        var second = pair.Second;
                        local2 = second;
                        return true;
                    }
                }
            }

            return m_source.MoveNext(ref currentElement, ref currentKey);
        }
    }

    private class OperatorState<TKey>
    {
        internal TKey m_currentLowKey;
        internal volatile int m_updatesDone;
    }
}