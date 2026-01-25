#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class SelectQueryOperator<TInput, TOutput> : UnaryQueryOperator<TInput, TOutput>
{
    private readonly Func<TInput, TOutput> m_selector;

    internal SelectQueryOperator(IEnumerable<TInput> child, Func<TInput, TOutput> selector)
        : base(child)
    {
        m_selector = selector;
        SetOrdinalIndexState(Child.OrdinalIndexState);
    }

    internal override bool LimitsParallelism => false;

    internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
    {
        return Child.AsSequentialQuery(token).Select(m_selector);
    }

    internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
    {
        return SelectQueryOperatorResults.NewResults(Child.Open(settings, preferStriping), this, settings,
            preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TInput, TKey> inputStream,
        IPartitionedStreamRecipient<TOutput> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionedStream =
            new PartitionedStream<TOutput, TKey>(inputStream.PartitionCount, inputStream.KeyComparer,
                OrdinalIndexState);
        for (var index = 0; index < inputStream.PartitionCount; ++index)
        {
            partitionedStream[index] = new SelectQueryOperatorEnumerator<TKey>(inputStream[index], m_selector);
        }

        recipient.Receive(partitionedStream);
    }

    private class SelectQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TOutput, TKey>
    {
        private readonly Func<TInput, TOutput> m_selector;
        private readonly QueryOperatorEnumerator<TInput, TKey> m_source;

        internal SelectQueryOperatorEnumerator(
            QueryOperatorEnumerator<TInput, TKey> source,
            Func<TInput, TOutput> selector)
        {
            m_source = source;
            m_selector = selector;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TOutput currentElement, ref TKey currentKey)
        {
            var currentElement1 = default(TInput);
            if (!m_source.MoveNext(ref currentElement1, ref currentKey))
            {
                return false;
            }

            currentElement = m_selector(currentElement1);
            return true;
        }
    }

    private class SelectQueryOperatorResults :
        UnaryQueryOperatorResults
    {
        private readonly int m_childCount;
        private readonly Func<TInput, TOutput> m_selector;

        private SelectQueryOperatorResults(
            QueryResults<TInput> childQueryResults,
            SelectQueryOperator<TInput, TOutput> op,
            QuerySettings settings,
            bool preferStriping)
            : base(childQueryResults, op, settings, preferStriping)
        {
            m_selector = op.m_selector;
            m_childCount = m_childQueryResults.ElementsCount;
        }

        internal override bool IsIndexible => true;

        internal override int ElementsCount => m_childCount;

        public static QueryResults<TOutput> NewResults(
            QueryResults<TInput> childQueryResults,
            SelectQueryOperator<TInput, TOutput> op,
            QuerySettings settings,
            bool preferStriping)
        {
            return childQueryResults.IsIndexible
                ? new SelectQueryOperatorResults(childQueryResults, op, settings, preferStriping)
                : (QueryResults<TOutput>)new UnaryQueryOperatorResults(childQueryResults, op, settings, preferStriping);
        }

        internal override TOutput GetElement(int index)
        {
            return m_selector(m_childQueryResults.GetElement(index));
        }
    }
}