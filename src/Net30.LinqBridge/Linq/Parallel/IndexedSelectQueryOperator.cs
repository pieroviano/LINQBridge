#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class IndexedSelectQueryOperator<TInput, TOutput> :
    UnaryQueryOperator<TInput, TOutput>
{
    private readonly Func<TInput, int, TOutput> m_selector;
    private bool m_limitsParallelism;
    private bool m_prematureMerge;

    internal IndexedSelectQueryOperator(
        IEnumerable<TInput> child,
        Func<TInput, int, TOutput> selector)
        : base(child)
    {
        m_selector = selector;
        m_outputOrdered = true;
        InitOrdinalIndexState();
    }

    internal override bool LimitsParallelism => m_limitsParallelism;

    internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
    {
        return Child.AsSequentialQuery(token).Select(m_selector);
    }

    internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
    {
        return IndexedSelectQueryOperatorResults.NewResults(Child.Open(settings, preferStriping), this, settings,
            preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TInput, TKey> inputStream,
        IPartitionedStreamRecipient<TOutput> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var partitionedStream1 = !m_prematureMerge
            ? (PartitionedStream<TInput, int>)(object)inputStream
            : QueryOperator<TInput>
                .ExecuteAndCollectResults(inputStream, partitionCount, Child.OutputOrdered, preferStriping, settings)
                .GetPartitionedStream();
        var partitionedStream2 =
            new PartitionedStream<TOutput, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream2[index] = new IndexedSelectQueryOperatorEnumerator(partitionedStream1[index], m_selector);
        }

        recipient.Receive(partitionedStream2);
    }

    private void InitOrdinalIndexState()
    {
        var ordinalIndexState = Child.OrdinalIndexState;
        var indexState = ordinalIndexState;
        if (ordinalIndexState.IsWorseThan(OrdinalIndexState.Correct))
        {
            m_prematureMerge = true;
            m_limitsParallelism = ordinalIndexState != OrdinalIndexState.Shuffled;
            indexState = OrdinalIndexState.Correct;
        }

        SetOrdinalIndexState(indexState);
    }

    private class IndexedSelectQueryOperatorEnumerator : QueryOperatorEnumerator<TOutput, int>
    {
        private readonly Func<TInput, int, TOutput> m_selector;
        private readonly QueryOperatorEnumerator<TInput, int> m_source;

        internal IndexedSelectQueryOperatorEnumerator(
            QueryOperatorEnumerator<TInput, int> source,
            Func<TInput, int, TOutput> selector)
        {
            m_source = source;
            m_selector = selector;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TOutput currentElement, ref int currentKey)
        {
            var currentElement1 = default(TInput);
            if (!m_source.MoveNext(ref currentElement1, ref currentKey))
            {
                return false;
            }

            currentElement = m_selector(currentElement1, currentKey);
            return true;
        }
    }

    private class IndexedSelectQueryOperatorResults :
        UnaryQueryOperatorResults
    {
        private readonly IndexedSelectQueryOperator<TInput, TOutput> m_selectOp;
        private int m_childCount;

        private IndexedSelectQueryOperatorResults(
            QueryResults<TInput> childQueryResults,
            IndexedSelectQueryOperator<TInput, TOutput> op,
            QuerySettings settings,
            bool preferStriping)
            : base(childQueryResults, op, settings, preferStriping)
        {
            m_selectOp = op;
            m_childCount = m_childQueryResults.ElementsCount;
        }

        internal override int ElementsCount => m_childQueryResults.ElementsCount;

        internal override bool IsIndexible => true;

        public static QueryResults<TOutput> NewResults(
            QueryResults<TInput> childQueryResults,
            IndexedSelectQueryOperator<TInput, TOutput> op,
            QuerySettings settings,
            bool preferStriping)
        {
            return childQueryResults.IsIndexible
                ? new IndexedSelectQueryOperatorResults(childQueryResults, op, settings, preferStriping)
                : (QueryResults<TOutput>)new UnaryQueryOperatorResults(childQueryResults, op, settings, preferStriping);
        }

        internal override TOutput GetElement(int index)
        {
            return m_selectOp.m_selector(m_childQueryResults.GetElement(index), index);
        }
    }
}