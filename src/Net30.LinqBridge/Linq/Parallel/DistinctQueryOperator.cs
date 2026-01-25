#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DistinctQueryOperator<TInputOutput> :
    UnaryQueryOperator<TInputOutput, TInputOutput>
{
    private readonly IEqualityComparer<TInputOutput> m_comparer;

    internal DistinctQueryOperator(
        IEnumerable<TInputOutput> source,
        IEqualityComparer<TInputOutput> comparer)
        : base(source)
    {
        m_comparer = comparer;
        SetOrdinalIndexState(OrdinalIndexState.Shuffled);
    }

    internal override bool LimitsParallelism => false;

    internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
    {
        return CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token).Distinct(m_comparer);
    }

    internal override QueryResults<TInputOutput> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, false), this, settings, false);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TInputOutput, TKey> inputStream,
        IPartitionedStreamRecipient<TInputOutput> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        if (OutputOrdered)
        {
            WrapPartitionedStreamHelper(
                ExchangeUtilities.HashRepartitionOrdered(inputStream, null,
                    (IEqualityComparer<NoKeyMemoizationRequired>)null, m_comparer,
                    settings.CancellationState.MergedCancellationToken), recipient,
                settings.CancellationState.MergedCancellationToken);
        }
        else
        {
            WrapPartitionedStreamHelper(
                ExchangeUtilities.HashRepartition(inputStream, null, (IEqualityComparer<NoKeyMemoizationRequired>)null,
                    m_comparer, settings.CancellationState.MergedCancellationToken), recipient,
                settings.CancellationState.MergedCancellationToken);
        }
    }

    private void WrapPartitionedStreamHelper<TKey>(
        PartitionedStream<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> hashStream,
        IPartitionedStreamRecipient<TInputOutput> recipient,
        CancellationToken cancellationToken)
    {
        var partitionCount = hashStream.PartitionCount;
        var partitionedStream =
            new PartitionedStream<TInputOutput, TKey>(partitionCount, hashStream.KeyComparer,
                OrdinalIndexState.Shuffled);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = !OutputOrdered
                ? (QueryOperatorEnumerator<TInputOutput, TKey>)(object)new DistinctQueryOperatorEnumerator<TKey>(
                    hashStream[index], m_comparer, cancellationToken)
                : new OrderedDistinctQueryOperatorEnumerator<TKey>(hashStream[index], m_comparer,
                    hashStream.KeyComparer, cancellationToken);
        }

        recipient.Receive(partitionedStream);
    }

    private class DistinctQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TInputOutput, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly Set<TInputOutput> m_hashLookup;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> m_source;
        private Shared<int> m_outputLoopCount;

        internal DistinctQueryOperatorEnumerator(
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> source,
            IEqualityComparer<TInputOutput> comparer,
            CancellationToken cancellationToken)
        {
            m_source = source;
            m_hashLookup = new Set<TInputOutput>(comparer);
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TInputOutput currentElement, ref int currentKey)
        {
            var currentKey1 = default(TKey);
            var currentElement1 = new Pair<TInputOutput, NoKeyMemoizationRequired>();
            if (m_outputLoopCount == null)
            {
                m_outputLoopCount = new Shared<int>(0);
            }

            while (m_source.MoveNext(ref currentElement1, ref currentKey1))
            {
                if ((m_outputLoopCount.Value++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                if (m_hashLookup.Add(currentElement1.First))
                {
                    currentElement = currentElement1.First;
                    return true;
                }
            }

            return false;
        }
    }

    private class OrderedDistinctQueryOperatorEnumerator<TKey> :
        QueryOperatorEnumerator<TInputOutput, TKey>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly Dictionary<Wrapper<TInputOutput>, TKey> m_hashLookup;
        private readonly IComparer<TKey> m_keyComparer;
        private readonly QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> m_source;
        private IEnumerator<KeyValuePair<Wrapper<TInputOutput>, TKey>> m_hashLookupEnumerator;

        internal OrderedDistinctQueryOperatorEnumerator(
            QueryOperatorEnumerator<Pair<TInputOutput, NoKeyMemoizationRequired>, TKey> source,
            IEqualityComparer<TInputOutput> comparer,
            IComparer<TKey> keyComparer,
            CancellationToken cancellationToken)
        {
            m_source = source;
            m_keyComparer = keyComparer;
            m_hashLookup =
                new Dictionary<Wrapper<TInputOutput>, TKey>(new WrapperEqualityComparer<TInputOutput>(comparer));
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
            if (m_hashLookupEnumerator == null)
            {
                return;
            }

            m_hashLookupEnumerator.Dispose();
        }

        internal override bool MoveNext(ref TInputOutput currentElement, ref TKey currentKey)
        {
            if (m_hashLookupEnumerator == null)
            {
                var currentElement1 = new Pair<TInputOutput, NoKeyMemoizationRequired>();
                var currentKey1 = default(TKey);
                var num = 0;
                while (m_source.MoveNext(ref currentElement1, ref currentKey1))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    var key = new Wrapper<TInputOutput>(currentElement1.First);
                    TKey y;
                    if (!m_hashLookup.TryGetValue(key, out y) || m_keyComparer.Compare(currentKey1, y) < 0)
                    {
                        m_hashLookup[key] = currentKey1;
                    }
                }

                m_hashLookupEnumerator = m_hashLookup.GetEnumerator();
            }

            if (!m_hashLookupEnumerator.MoveNext())
            {
                return false;
            }

            var current = m_hashLookupEnumerator.Current;
            currentElement = current.Key.Value;
            currentKey = current.Value;
            return true;
        }
    }
}