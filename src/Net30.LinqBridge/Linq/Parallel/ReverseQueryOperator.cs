#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ReverseQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
{
    internal ReverseQueryOperator(IEnumerable<TSource> child)
        : base(child)
    {
        if (Child.OrdinalIndexState == OrdinalIndexState.Indexible)
        {
            SetOrdinalIndexState(OrdinalIndexState.Indexible);
        }
        else
        {
            SetOrdinalIndexState(OrdinalIndexState.Shuffled);
        }
    }

    internal override bool LimitsParallelism => false;

    internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
    {
        return CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token).Reverse();
    }

    internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
    {
        return ReverseQueryOperatorResults.NewResults(Child.Open(settings, false), this, settings, preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TSource, TKey> inputStream,
        IPartitionedStreamRecipient<TSource> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var partitionedStream = new PartitionedStream<TSource, TKey>(partitionCount,
            new ReverseComparer<TKey>(inputStream.KeyComparer), OrdinalIndexState.Shuffled);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = new ReverseQueryOperatorEnumerator<TKey>(inputStream[index],
                settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream);
    }

    private class ReverseQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, TKey>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly QueryOperatorEnumerator<TSource, TKey> m_source;
        private List<Pair<TSource, TKey>> m_buffer;
        private Shared<int> m_bufferIndex;

        internal ReverseQueryOperatorEnumerator(
            QueryOperatorEnumerator<TSource, TKey> source,
            CancellationToken cancellationToken)
        {
            m_source = source;
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TSource currentElement, ref TKey currentKey)
        {
            if (m_buffer == null)
            {
                m_bufferIndex = new Shared<int>(0);
                m_buffer = new List<Pair<TSource, TKey>>();
                var currentElement1 = default(TSource);
                var currentKey1 = default(TKey);
                var num = 0;
                while (m_source.MoveNext(ref currentElement1, ref currentKey1))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    m_buffer.Add(new Pair<TSource, TKey>(currentElement1, currentKey1));
                    ++m_bufferIndex.Value;
                }
            }

            if (--m_bufferIndex.Value < 0)
            {
                return false;
            }

            currentElement = m_buffer[m_bufferIndex.Value].First;
            currentKey = m_buffer[m_bufferIndex.Value].Second;
            return true;
        }
    }

    private class ReverseQueryOperatorResults :
        UnaryQueryOperatorResults
    {
        private readonly int m_count;

        private ReverseQueryOperatorResults(
            QueryResults<TSource> childQueryResults,
            ReverseQueryOperator<TSource> op,
            QuerySettings settings,
            bool preferStriping)
            : base(childQueryResults, op, settings, preferStriping)
        {
            m_count = m_childQueryResults.ElementsCount;
        }

        internal override bool IsIndexible => true;

        internal override int ElementsCount => m_count;

        public static QueryResults<TSource> NewResults(
            QueryResults<TSource> childQueryResults,
            ReverseQueryOperator<TSource> op,
            QuerySettings settings,
            bool preferStriping)
        {
            return childQueryResults.IsIndexible
                ? new ReverseQueryOperatorResults(childQueryResults, op, settings, preferStriping)
                : (QueryResults<TSource>)new UnaryQueryOperatorResults(childQueryResults, op, settings, preferStriping);
        }

        internal override TSource GetElement(int index)
        {
            return m_childQueryResults.GetElement(m_count - index - 1);
        }
    }
}