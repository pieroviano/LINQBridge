#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ExceptQueryOperator<TInputOutput> :
    BinaryQueryOperator<TInputOutput, TInputOutput, TInputOutput>
{
    private readonly IEqualityComparer<TInputOutput> m_comparer;

    internal ExceptQueryOperator(
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
        PartitionedStream<TInputOutput, TLeftKey> leftStream,
        PartitionedStream<TInputOutput, TRightKey> rightStream,
        IPartitionedStreamRecipient<TInputOutput> outputRecipient,
        bool preferStriping,
        QuerySettings settings)
    {
        if (OutputOrdered)
        {
            WrapPartitionedStreamHelper(
                ExchangeUtilities.HashRepartitionOrdered(leftStream, null,
                    (IEqualityComparer<NoKeyMemoizationRequired>)null, m_comparer,
                    settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient,
                settings.CancellationState.MergedCancellationToken);
        }
        else
        {
            WrapPartitionedStreamHelper(
                ExchangeUtilities.HashRepartition(leftStream, null, (IEqualityComparer<NoKeyMemoizationRequired>)null,
                    m_comparer, settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient,
                settings.CancellationState.MergedCancellationToken);
        }
    }

    internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
    {
        return CancellableEnumerable.Wrap(LeftChild.AsSequentialQuery(token), token)
            .Except(CancellableEnumerable.Wrap(RightChild.AsSequentialQuery(token), token), m_comparer);
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
                ? (QueryOperatorEnumerator<TInputOutput, TLeftKey>)(object)new ExceptQueryOperatorEnumerator<TLeftKey>(
                    leftHashStream[index], partitionedStream1[index], m_comparer, cancellationToken)
                : new OrderedExceptQueryOperatorEnumerator<TLeftKey>(leftHashStream[index], partitionedStream1[index],
                    m_comparer, leftHashStream.KeyComparer, cancellationToken);
        }

        outputRecipient.Receive(partitionedStream2);
    }

    private class ExceptQueryOperatorEnumerator<TLeftKey> : QueryOperatorEnumerator<TInputOutput, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly IEqualityComparer<TInputOutput> m_comparer;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> m_leftSource;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> m_rightSource;
        private Set<TInputOutput> m_hashLookup;
        private Shared<int> m_outputLoopCount;

        internal ExceptQueryOperatorEnumerator(
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

                if (m_hashLookup.Add(currentElement2.First))
                {
                    currentElement = currentElement2.First;
                    return true;
                }
            }

            return false;
        }
    }

    private class OrderedExceptQueryOperatorEnumerator<TLeftKey> :
        QueryOperatorEnumerator<TInputOutput, TLeftKey>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly IEqualityComparer<TInputOutput> m_comparer;
        private readonly IComparer<TLeftKey> m_leftKeyComparer;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> m_leftSource;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> m_rightSource;
        private IEnumerator<KeyValuePair<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>>> m_outputEnumerator;

        internal OrderedExceptQueryOperatorEnumerator(
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource,
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, int> rightSource,
            IEqualityComparer<TInputOutput> comparer,
            IComparer<TLeftKey> leftKeyComparer,
            CancellationToken cancellationToken)
        {
            m_leftSource = leftSource;
            m_rightSource = rightSource;
            m_comparer = comparer;
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
            if (m_outputEnumerator == null)
            {
                var set = new Set<TInputOutput>(m_comparer);
                var currentElement1 = new Pair<TInputOutput, NoKeyMemoizationRequired>();
                var currentKey1 = 0;
                var num = 0;
                while (m_rightSource.MoveNext(ref currentElement1, ref currentKey1))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    set.Add(currentElement1.First);
                }

                var dictionary =
                    new Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, TLeftKey>>(
                        new WrapperEqualityComparer<TInputOutput>(m_comparer));
                var currentElement2 = new Pair<TInputOutput, NoKeyMemoizationRequired>();
                var currentKey2 = default(TLeftKey);
                while (m_leftSource.MoveNext(ref currentElement2, ref currentKey2))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (!set.Contains(currentElement2.First))
                    {
                        var key = new Wrapper<TInputOutput>(currentElement2.First);
                        Pair<TInputOutput, TLeftKey> pair;
                        if (!dictionary.TryGetValue(key, out pair) ||
                            m_leftKeyComparer.Compare(currentKey2, pair.Second) < 0)
                        {
                            dictionary[key] = new Pair<TInputOutput, TLeftKey>(currentElement2.First, currentKey2);
                        }
                    }
                }

                m_outputEnumerator = dictionary.GetEnumerator();
            }

            if (!m_outputEnumerator.MoveNext())
            {
                return false;
            }

            var pair1 = m_outputEnumerator.Current.Value;
            currentElement = pair1.First;
            currentKey = pair1.Second;
            return true;
        }
    }
}