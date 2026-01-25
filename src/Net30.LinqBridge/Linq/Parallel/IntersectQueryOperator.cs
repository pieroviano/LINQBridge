#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class IntersectQueryOperator<TInputOutput> :
    BinaryQueryOperator<TInputOutput, TInputOutput, TInputOutput>
{
    private readonly IEqualityComparer<TInputOutput> m_comparer;

    internal IntersectQueryOperator(
        ParallelQuery<TInputOutput> left,
        ParallelQuery<TInputOutput> right,
        IEqualityComparer<TInputOutput> comparer)
        : base(left, right)
    {
        m_comparer = comparer;
        m_outputOrdered = LeftChild.OutputOrdered;
        SetOrdinalIndex(OrdinalIndexState.Shuffled);
    }

    internal override bool LimitsParallelism => false;

    public override void WrapPartitionedStream<TLeftKey, TRightKey>(
        PartitionedStream<TInputOutput, TLeftKey> leftPartitionedStream,
        PartitionedStream<TInputOutput, TRightKey> rightPartitionedStream,
        IPartitionedStreamRecipient<TInputOutput> outputRecipient,
        bool preferStriping,
        QuerySettings settings)
    {
        if (OutputOrdered)
        {
            WrapPartitionedStreamHelper(
                ExchangeUtilities.HashRepartitionOrdered(leftPartitionedStream, null,
                    (IEqualityComparer<NoKeyMemoizationRequired>)null, m_comparer,
                    settings.CancellationState.MergedCancellationToken), rightPartitionedStream, outputRecipient,
                settings.CancellationState.MergedCancellationToken);
        }
        else
        {
            WrapPartitionedStreamHelper(
                ExchangeUtilities.HashRepartition(leftPartitionedStream, null,
                    (IEqualityComparer<NoKeyMemoizationRequired>)null, m_comparer,
                    settings.CancellationState.MergedCancellationToken), rightPartitionedStream, outputRecipient,
                settings.CancellationState.MergedCancellationToken);
        }
    }

    internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
    {
        return CancellableEnumerable.Wrap(LeftChild.AsSequentialQuery(token), token)
            .Intersect(CancellableEnumerable.Wrap(RightChild.AsSequentialQuery(token), token), m_comparer);
    }

    internal override QueryResults<TInputOutput> Open(QuerySettings settings, bool preferStriping)
    {
        return new BinaryQueryOperatorResults(LeftChild.Open(settings, false), RightChild.Open(settings, false), this,
            settings, false);
    }

    private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
        PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftHashStream,
        PartitionedStream<TInputOutput, TRightKey> rightPartitionedStream,
        IPartitionedStreamRecipient<TInputOutput> outputRecipient,
        CancellationToken cancellationToken)
    {
        var partitionCount = leftHashStream.PartitionCount;
        var partitionedStream1 = ExchangeUtilities.HashRepartition(rightPartitionedStream, null,
            (IEqualityComparer<NoKeyMemoizationRequired>)null, m_comparer, cancellationToken);
        var partitionedStream2 = new PartitionedStream<TInputOutput, TLeftKey>(partitionCount,
            leftHashStream.KeyComparer, OrdinalIndexState.Shuffled);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream2[index] = !OutputOrdered
                ? (QueryOperatorEnumerator<TInputOutput, TLeftKey>)(object)new
                    IntersectQueryOperatorEnumerator<TLeftKey>(leftHashStream[index], partitionedStream1[index],
                        m_comparer, cancellationToken)
                : new OrderedIntersectQueryOperatorEnumerator<TLeftKey>(leftHashStream[index],
                    partitionedStream1[index], m_comparer, leftHashStream.KeyComparer, cancellationToken);
        }

        outputRecipient.Receive(partitionedStream2);
    }

    private class IntersectQueryOperatorEnumerator<TLeftKey> :
        QueryOperatorEnumerator<TInputOutput, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly IEqualityComparer<TInputOutput> m_comparer;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> m_leftSource;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> m_rightSource;
        private Set<TInputOutput> m_hashLookup;
        private Shared<int> m_outputLoopCount;

        internal IntersectQueryOperatorEnumerator(
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource,
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightSource,
            IEqualityComparer<TInputOutput> comparer,
            CancellationToken cancellationToken)
        {
            m_leftSource = leftSource;
            m_rightSource = rightSource;
            m_comparer = comparer;
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_leftSource.Dispose();
            m_rightSource.Dispose();
        }

        internal override bool MoveNext(ref TInputOutput currentElement, ref int currentKey)
        {
            if (m_hashLookup == null)
            {
                m_outputLoopCount = new Shared<int>(0);
                m_hashLookup = new Set<TInputOutput>(m_comparer);
                var currentElement1 = new Pair<TInputOutput, NoKeyMemoizationRequired>();
                var currentKey1 = 0;
                var num = 0;
                while (m_rightSource.MoveNext(ref currentElement1, ref currentKey1))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    m_hashLookup.Add(currentElement1.First);
                }
            }

            var currentElement2 = new Pair<TInputOutput, NoKeyMemoizationRequired>();
            var currentKey2 = default(TLeftKey);
            while (m_leftSource.MoveNext(ref currentElement2, ref currentKey2))
            {
                if ((m_outputLoopCount.Value++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                if (m_hashLookup.Contains(currentElement2.First))
                {
                    m_hashLookup.Remove(currentElement2.First);
                    currentElement = currentElement2.First;
                    return true;
                }
            }

            return false;
        }
    }

    private class OrderedIntersectQueryOperatorEnumerator<TLeftKey> :
        QueryOperatorEnumerator<TInputOutput, TLeftKey>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly IEqualityComparer<Wrapper<TInputOutput>> m_comparer;
        private readonly IComparer<TLeftKey> m_leftKeyComparer;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> m_leftSource;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> m_rightSource;
        private Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>> m_hashLookup;

        internal OrderedIntersectQueryOperatorEnumerator(
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource,
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightSource,
            IEqualityComparer<TInputOutput> comparer,
            IComparer<TLeftKey> leftKeyComparer,
            CancellationToken cancellationToken)
        {
            m_leftSource = leftSource;
            m_rightSource = rightSource;
            m_comparer = new WrapperEqualityComparer<TInputOutput>(comparer);
            m_leftKeyComparer = leftKeyComparer;
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_leftSource.Dispose();
            m_rightSource.Dispose();
        }

        internal override bool MoveNext(ref TInputOutput currentElement, ref TLeftKey currentKey)
        {
            var num = 0;
            if (m_hashLookup == null)
            {
                m_hashLookup = new Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>>(m_comparer);
                var currentElement1 = new Pair<TInputOutput, NoKeyMemoizationRequired>();
                var currentKey1 = default(TLeftKey);
                while (m_leftSource.MoveNext(ref currentElement1, ref currentKey1))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    var key = new Wrapper<TInputOutput>(currentElement1.First);
                    Pair<TInputOutput, TLeftKey> pair;
                    if (!m_hashLookup.TryGetValue(key, out pair) ||
                        m_leftKeyComparer.Compare(currentKey1, pair.Second) < 0)
                    {
                        m_hashLookup[key] = new Pair<TInputOutput, TLeftKey>(currentElement1.First, currentKey1);
                    }
                }
            }

            var currentElement2 = new Pair<TInputOutput, NoKeyMemoizationRequired>();
            var currentKey2 = 0;
            while (m_rightSource.MoveNext(ref currentElement2, ref currentKey2))
            {
                if ((num++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                Pair<TInputOutput, TLeftKey> pair;
                if (m_hashLookup.TryGetValue(new Wrapper<TInputOutput>(currentElement2.First), out pair))
                {
                    currentElement = pair.First;
                    currentKey = pair.Second;
                    m_hashLookup.Remove(new Wrapper<TInputOutput>(pair.First));
                    return true;
                }
            }

            return false;
        }
    }
}