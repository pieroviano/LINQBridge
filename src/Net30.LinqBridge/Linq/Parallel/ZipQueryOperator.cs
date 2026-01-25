#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ZipQueryOperator<TLeftInput, TRightInput, TOutput> : QueryOperator<TOutput>
{
    private readonly QueryOperator<TLeftInput> m_leftChild;
    private readonly bool m_prematureMergeLeft;
    private readonly bool m_prematureMergeRight;
    private readonly Func<TLeftInput, TRightInput, TOutput> m_resultSelector;
    private readonly QueryOperator<TRightInput> m_rightChild;

    internal ZipQueryOperator(
        ParallelQuery<TLeftInput> leftChildSource,
        IEnumerable<TRightInput> rightChildSource,
        Func<TLeftInput, TRightInput, TOutput> resultSelector)
        : this(QueryOperator<TLeftInput>.AsQueryOperator(leftChildSource),
            QueryOperator<TRightInput>.AsQueryOperator(rightChildSource), resultSelector)
    {
    }

    private ZipQueryOperator(
        QueryOperator<TLeftInput> left,
        QueryOperator<TRightInput> right,
        Func<TLeftInput, TRightInput, TOutput> resultSelector)
        : base(left.SpecifiedQuerySettings.Merge(right.SpecifiedQuerySettings))
    {
        m_leftChild = left;
        m_rightChild = right;
        m_resultSelector = resultSelector;
        m_outputOrdered = m_leftChild.OutputOrdered || m_rightChild.OutputOrdered;
        var ordinalIndexState1 = m_leftChild.OrdinalIndexState;
        var ordinalIndexState2 = m_rightChild.OrdinalIndexState;
        m_prematureMergeLeft = ordinalIndexState1 != 0;
        m_prematureMergeRight = ordinalIndexState2 != 0;
        LimitsParallelism = (m_prematureMergeLeft && ordinalIndexState1 != OrdinalIndexState.Shuffled) ||
                            (m_prematureMergeRight && ordinalIndexState2 != OrdinalIndexState.Shuffled);
    }

    internal override OrdinalIndexState OrdinalIndexState => OrdinalIndexState.Indexible;

    internal override bool LimitsParallelism { get; }

    internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
    {
        using (var leftEnumerator = m_leftChild.AsSequentialQuery(token).GetEnumerator())
        {
            using (var rightEnumerator = m_rightChild.AsSequentialQuery(token).GetEnumerator())
            {
                while (leftEnumerator.MoveNext() && rightEnumerator.MoveNext())
                {
                    yield return m_resultSelector(leftEnumerator.Current, rightEnumerator.Current);
                }
            }
        }
    }

    internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
    {
        var leftChildResults = m_leftChild.Open(settings, preferStriping);
        var rightChildResults = m_rightChild.Open(settings, preferStriping);
        var partitionCount = settings.DegreeOfParallelism.Value;
        if (m_prematureMergeLeft)
        {
            var recipient = new PartitionedStreamMerger<TLeftInput>(false, ParallelMergeOptions.FullyBuffered,
                settings.TaskScheduler, m_leftChild.OutputOrdered, settings.CancellationState, settings.QueryId);
            leftChildResults.GivePartitionedStream(recipient);
            leftChildResults = new ListQueryResults<TLeftInput>(recipient.MergeExecutor.GetResultsAsArray(),
                partitionCount, preferStriping);
        }

        if (m_prematureMergeRight)
        {
            var recipient = new PartitionedStreamMerger<TRightInput>(false, ParallelMergeOptions.FullyBuffered,
                settings.TaskScheduler, m_rightChild.OutputOrdered, settings.CancellationState, settings.QueryId);
            rightChildResults.GivePartitionedStream(recipient);
            rightChildResults = new ListQueryResults<TRightInput>(recipient.MergeExecutor.GetResultsAsArray(),
                partitionCount, preferStriping);
        }

        return new ZipQueryOperatorResults(leftChildResults, rightChildResults, m_resultSelector, partitionCount,
            preferStriping);
    }

    internal class ZipQueryOperatorResults : QueryResults<TOutput>
    {
        private readonly int m_count;
        private readonly QueryResults<TLeftInput> m_leftChildResults;
        private readonly int m_partitionCount;
        private readonly bool m_preferStriping;
        private readonly Func<TLeftInput, TRightInput, TOutput> m_resultSelector;
        private readonly QueryResults<TRightInput> m_rightChildResults;

        internal ZipQueryOperatorResults(
            QueryResults<TLeftInput> leftChildResults,
            QueryResults<TRightInput> rightChildResults,
            Func<TLeftInput, TRightInput, TOutput> resultSelector,
            int partitionCount,
            bool preferStriping)
        {
            m_leftChildResults = leftChildResults;
            m_rightChildResults = rightChildResults;
            m_resultSelector = resultSelector;
            m_partitionCount = partitionCount;
            m_preferStriping = preferStriping;
            m_count = Math.Min(m_leftChildResults.Count, m_rightChildResults.Count);
        }

        internal override int ElementsCount => m_count;

        internal override bool IsIndexible => true;

        internal override TOutput GetElement(int index)
        {
            return m_resultSelector(m_leftChildResults.GetElement(index), m_rightChildResults.GetElement(index));
        }

        internal override void GivePartitionedStream(IPartitionedStreamRecipient<TOutput> recipient)
        {
            var partitionedStream = ExchangeUtilities.PartitionDataSource(this, m_partitionCount, m_preferStriping);
            recipient.Receive(partitionedStream);
        }
    }
}