#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class UnionQueryOperator<TInputOutput> :
    BinaryQueryOperator<TInputOutput, TInputOutput, TInputOutput>
{
    private readonly IEqualityComparer<TInputOutput> m_comparer;

    internal UnionQueryOperator(
        ParallelQuery<TInputOutput> left,
        ParallelQuery<TInputOutput> right,
        IEqualityComparer<TInputOutput> comparer)
        : base(left, right)
    {
        m_comparer = comparer;
        m_outputOrdered = LeftChild.OutputOrdered || RightChild.OutputOrdered;
    }

    internal override bool LimitsParallelism => false;

    public override void WrapPartitionedStream<TLeftKey, TRightKey>(
        PartitionedStream<TInputOutput, TLeftKey> leftStream,
        PartitionedStream<TInputOutput, TRightKey> rightStream,
        IPartitionedStreamRecipient<TInputOutput> outputRecipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionCount = leftStream.PartitionCount;
        if (LeftChild.OutputOrdered)
        {
            WrapPartitionedStreamFixedLeftType(
                ExchangeUtilities.HashRepartitionOrdered(leftStream, null,
                    (IEqualityComparer<NoKeyMemoizationRequired>)null, m_comparer,
                    settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient, partitionCount,
                settings.CancellationState.MergedCancellationToken);
        }
        else
        {
            WrapPartitionedStreamFixedLeftType(
                ExchangeUtilities.HashRepartition(leftStream, null, (IEqualityComparer<NoKeyMemoizationRequired>)null,
                    m_comparer, settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient,
                partitionCount, settings.CancellationState.MergedCancellationToken);
        }
    }

    internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
    {
        return CancellableEnumerable.Wrap(LeftChild.AsSequentialQuery(token), token)
            .Union(CancellableEnumerable.Wrap(RightChild.AsSequentialQuery(token), token), m_comparer);
    }

    internal override QueryResults<TInputOutput> Open(QuerySettings settings, bool preferStriping)
    {
        return new BinaryQueryOperatorResults(LeftChild.Open(settings, false), RightChild.Open(settings, false), this,
            settings, false);
    }

    private void WrapPartitionedStreamFixedBothTypes<TLeftKey, TRightKey>(
        PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftHashStream,
        PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> rightHashStream,
        IPartitionedStreamRecipient<TInputOutput> outputRecipient,
        int partitionCount,
        CancellationToken cancellationToken)
    {
        if (LeftChild.OutputOrdered || RightChild.OutputOrdered)
        {
            var keyComparer =
                ConcatKey<TLeftKey, TRightKey>.MakeComparer(leftHashStream.KeyComparer, rightHashStream.KeyComparer);
            var partitionedStream =
                new PartitionedStream<TInputOutput, ConcatKey<TLeftKey, TRightKey>>(partitionCount, keyComparer,
                    OrdinalIndexState.Shuffled);
            for (var index = 0; index < partitionCount; ++index)
            {
                partitionedStream[index] = new OrderedUnionQueryOperatorEnumerator<TLeftKey, TRightKey>(
                    leftHashStream[index], rightHashStream[index], LeftChild.OutputOrdered, RightChild.OutputOrdered,
                    m_comparer, keyComparer, cancellationToken);
            }

            outputRecipient.Receive(partitionedStream);
        }
        else
        {
            var partitionedStream = new PartitionedStream<TInputOutput, int>(partitionCount,
                Util.GetDefaultComparer<int>(), OrdinalIndexState.Shuffled);
            for (var index = 0; index < partitionCount; ++index)
            {
                partitionedStream[index] = new UnionQueryOperatorEnumerator<TLeftKey, TRightKey>(leftHashStream[index],
                    rightHashStream[index], index, m_comparer, cancellationToken);
            }

            outputRecipient.Receive(partitionedStream);
        }
    }

    private void WrapPartitionedStreamFixedLeftType<TLeftKey, TRightKey>(
        PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftHashStream,
        PartitionedStream<TInputOutput, TRightKey> rightStream,
        IPartitionedStreamRecipient<TInputOutput> outputRecipient,
        int partitionCount,
        CancellationToken cancellationToken)
    {
        if (RightChild.OutputOrdered)
        {
            var rightHashStream = ExchangeUtilities.HashRepartitionOrdered(rightStream, null,
                (IEqualityComparer<NoKeyMemoizationRequired>)null, m_comparer, cancellationToken);
            WrapPartitionedStreamFixedBothTypes(leftHashStream, rightHashStream, outputRecipient, partitionCount,
                cancellationToken);
        }
        else
        {
            var rightHashStream = ExchangeUtilities.HashRepartition(rightStream, null,
                (IEqualityComparer<NoKeyMemoizationRequired>)null, m_comparer, cancellationToken);
            WrapPartitionedStreamFixedBothTypes(leftHashStream, rightHashStream, outputRecipient, partitionCount,
                cancellationToken);
        }
    }

    private class UnionQueryOperatorEnumerator<TLeftKey, TRightKey> :
        QueryOperatorEnumerator<TInputOutput, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly IEqualityComparer<TInputOutput> m_comparer;
        private readonly int m_partitionIndex;
        private Set<TInputOutput> m_hashLookup;
        private QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> m_leftSource;
        private Shared<int> m_outputLoopCount;
        private QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> m_rightSource;

        internal UnionQueryOperatorEnumerator(
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource,
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> rightSource,
            int partitionIndex,
            IEqualityComparer<TInputOutput> comparer,
            CancellationToken cancellationToken)
        {
            m_leftSource = leftSource;
            m_rightSource = rightSource;
            m_partitionIndex = partitionIndex;
            m_comparer = comparer;
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            if (m_leftSource != null)
            {
                m_leftSource.Dispose();
            }

            if (m_rightSource == null)
            {
                return;
            }

            m_rightSource.Dispose();
        }

        internal override bool MoveNext(ref TInputOutput currentElement, ref int currentKey)
        {
            if (m_hashLookup == null)
            {
                m_hashLookup = new Set<TInputOutput>(m_comparer);
                m_outputLoopCount = new Shared<int>(0);
            }

            if (m_leftSource != null)
            {
                var currentKey1 = default(TLeftKey);
                var currentElement1 = new Pair<TInputOutput, NoKeyMemoizationRequired>();
                var num = 0;
                while (m_leftSource.MoveNext(ref currentElement1, ref currentKey1))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (m_hashLookup.Add(currentElement1.First))
                    {
                        currentElement = currentElement1.First;
                        return true;
                    }
                }

                m_leftSource.Dispose();
                m_leftSource = null;
            }

            if (m_rightSource != null)
            {
                var currentKey2 = default(TRightKey);
                var currentElement2 = new Pair<TInputOutput, NoKeyMemoizationRequired>();
                while (m_rightSource.MoveNext(ref currentElement2, ref currentKey2))
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

                m_rightSource.Dispose();
                m_rightSource = null;
            }

            return false;
        }
    }

    private class OrderedUnionQueryOperatorEnumerator<TLeftKey, TRightKey> :
        QueryOperatorEnumerator<TInputOutput, ConcatKey<TLeftKey, TRightKey>>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly IEqualityComparer<TInputOutput> m_comparer;
        private readonly IComparer<ConcatKey<TLeftKey, TRightKey>> m_keyComparer;
        private readonly bool m_leftOrdered;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> m_leftSource;
        private readonly bool m_rightOrdered;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> m_rightSource;

        private IEnumerator<KeyValuePair<Wrapper<TInputOutput>, Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>>>
            m_outputEnumerator;

        internal OrderedUnionQueryOperatorEnumerator(
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TLeftKey> leftSource,
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TRightKey> rightSource,
            bool leftOrdered,
            bool rightOrdered,
            IEqualityComparer<TInputOutput> comparer,
            IComparer<ConcatKey<TLeftKey, TRightKey>> keyComparer,
            CancellationToken cancellationToken)
        {
            m_leftSource = leftSource;
            m_rightSource = rightSource;
            m_keyComparer = keyComparer;
            m_leftOrdered = leftOrdered;
            m_rightOrdered = rightOrdered;
            m_comparer = comparer;
            if (m_comparer == null)
            {
                m_comparer = EqualityComparer<TInputOutput>.Default;
            }

            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_leftSource.Dispose();
            m_rightSource.Dispose();
        }

        internal override bool MoveNext(
            ref TInputOutput currentElement,
            ref ConcatKey<TLeftKey, TRightKey> currentKey)
        {
            if (m_outputEnumerator == null)
            {
                var dictionary =
                    new Dictionary<Wrapper<TInputOutput>, Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>>(
                        new WrapperEqualityComparer<TInputOutput>(m_comparer));
                var currentElement1 = new Pair<TInputOutput, NoKeyMemoizationRequired>();
                var currentKey1 = default(TLeftKey);
                var num = 0;
                while (m_leftSource.MoveNext(ref currentElement1, ref currentKey1))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    var concatKey = ConcatKey<TLeftKey, TRightKey>.MakeLeft(m_leftOrdered ? currentKey1 : default);
                    var key = new Wrapper<TInputOutput>(currentElement1.First);
                    Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>> pair;
                    if (!dictionary.TryGetValue(key, out pair) || m_keyComparer.Compare(concatKey, pair.Second) < 0)
                    {
                        dictionary[key] =
                            new Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>(currentElement1.First, concatKey);
                    }
                }

                var currentKey2 = default(TRightKey);
                while (m_rightSource.MoveNext(ref currentElement1, ref currentKey2))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    var concatKey = ConcatKey<TLeftKey, TRightKey>.MakeRight(m_rightOrdered ? currentKey2 : default);
                    var key = new Wrapper<TInputOutput>(currentElement1.First);
                    Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>> pair;
                    if (!dictionary.TryGetValue(key, out pair) || m_keyComparer.Compare(concatKey, pair.Second) < 0)
                    {
                        dictionary[key] =
                            new Pair<TInputOutput, ConcatKey<TLeftKey, TRightKey>>(currentElement1.First, concatKey);
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