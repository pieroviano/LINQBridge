#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ConcatQueryOperator<TSource> : BinaryQueryOperator<TSource, TSource, TSource>
{
    private readonly bool m_prematureMergeLeft;
    private readonly bool m_prematureMergeRight;

    internal ConcatQueryOperator(
        ParallelQuery<TSource> firstChild,
        ParallelQuery<TSource> secondChild)
        : base(firstChild, secondChild)
    {
        m_outputOrdered = LeftChild.OutputOrdered || RightChild.OutputOrdered;
        m_prematureMergeLeft = LeftChild.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);
        m_prematureMergeRight = RightChild.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);
        if (LeftChild.OrdinalIndexState == OrdinalIndexState.Indexible &&
            RightChild.OrdinalIndexState == OrdinalIndexState.Indexible)
        {
            SetOrdinalIndex(OrdinalIndexState.Indexible);
        }
        else
        {
            SetOrdinalIndex(
                OrdinalIndexState.Increasing.Worse(LeftChild.OrdinalIndexState.Worse(RightChild.OrdinalIndexState)));
        }
    }

    internal override bool LimitsParallelism => false;

    public override void WrapPartitionedStream<TLeftKey, TRightKey>(
        PartitionedStream<TSource, TLeftKey> leftStream,
        PartitionedStream<TSource, TRightKey> rightStream,
        IPartitionedStreamRecipient<TSource> outputRecipient,
        bool preferStriping,
        QuerySettings settings)
    {
        if (m_prematureMergeLeft)
        {
            WrapHelper(
                ExecuteAndCollectResults(leftStream, leftStream.PartitionCount, LeftChild.OutputOrdered, preferStriping,
                    settings).GetPartitionedStream(), rightStream, outputRecipient, settings, preferStriping);
        }
        else
        {
            WrapHelper(leftStream, rightStream, outputRecipient, settings, preferStriping);
        }
    }

    internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
    {
        return LeftChild.AsSequentialQuery(token).Concat(RightChild.AsSequentialQuery(token));
    }

    internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
    {
        return ConcatQueryOperatorResults.NewResults(LeftChild.Open(settings, preferStriping),
            RightChild.Open(settings, preferStriping), this, settings, preferStriping);
    }

    private void WrapHelper<TLeftKey, TRightKey>(
        PartitionedStream<TSource, TLeftKey> leftStreamInc,
        PartitionedStream<TSource, TRightKey> rightStream,
        IPartitionedStreamRecipient<TSource> outputRecipient,
        QuerySettings settings,
        bool preferStriping)
    {
        if (m_prematureMergeRight)
        {
            var partitionedStream = ExecuteAndCollectResults(rightStream, leftStreamInc.PartitionCount,
                LeftChild.OutputOrdered, preferStriping, settings).GetPartitionedStream();
            WrapHelper2(leftStreamInc, partitionedStream, outputRecipient);
        }
        else
        {
            WrapHelper2(leftStreamInc, rightStream, outputRecipient);
        }
    }

    private void WrapHelper2<TLeftKey, TRightKey>(
        PartitionedStream<TSource, TLeftKey> leftStreamInc,
        PartitionedStream<TSource, TRightKey> rightStreamInc,
        IPartitionedStreamRecipient<TSource> outputRecipient)
    {
        var partitionCount = leftStreamInc.PartitionCount;
        var keyComparer =
            ConcatKey<TLeftKey, TRightKey>.MakeComparer(leftStreamInc.KeyComparer, rightStreamInc.KeyComparer);
        var partitionedStream =
            new PartitionedStream<TSource, ConcatKey<TLeftKey, TRightKey>>(partitionCount, keyComparer,
                OrdinalIndexState);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] =
                new ConcatQueryOperatorEnumerator<TLeftKey, TRightKey>(leftStreamInc[index], rightStreamInc[index]);
        }

        outputRecipient.Receive(partitionedStream);
    }

    private class ConcatQueryOperatorEnumerator<TLeftKey, TRightKey> :
        QueryOperatorEnumerator<TSource, ConcatKey<TLeftKey, TRightKey>>
    {
        private readonly QueryOperatorEnumerator<TSource, TLeftKey> m_firstSource;
        private readonly QueryOperatorEnumerator<TSource, TRightKey> m_secondSource;
        private bool m_begunSecond;

        internal ConcatQueryOperatorEnumerator(
            QueryOperatorEnumerator<TSource, TLeftKey> firstSource,
            QueryOperatorEnumerator<TSource, TRightKey> secondSource)
        {
            m_firstSource = firstSource;
            m_secondSource = secondSource;
        }

        protected override void Dispose(bool disposing)
        {
            m_firstSource.Dispose();
            m_secondSource.Dispose();
        }

        internal override bool MoveNext(
            ref TSource currentElement,
            ref ConcatKey<TLeftKey, TRightKey> currentKey)
        {
            if (!m_begunSecond)
            {
                var currentKey1 = default(TLeftKey);
                if (m_firstSource.MoveNext(ref currentElement, ref currentKey1))
                {
                    currentKey = ConcatKey<TLeftKey, TRightKey>.MakeLeft(currentKey1);
                    return true;
                }

                m_begunSecond = true;
            }

            var currentKey2 = default(TRightKey);
            if (!m_secondSource.MoveNext(ref currentElement, ref currentKey2))
            {
                return false;
            }

            currentKey = ConcatKey<TLeftKey, TRightKey>.MakeRight(currentKey2);
            return true;
        }
    }

    private class ConcatQueryOperatorResults :
        BinaryQueryOperatorResults
    {
        private readonly int m_leftChildCount;
        private readonly int m_rightChildCount;
        private ConcatQueryOperator<TSource> m_concatOp;

        private ConcatQueryOperatorResults(
            QueryResults<TSource> leftChildQueryResults,
            QueryResults<TSource> rightChildQueryResults,
            ConcatQueryOperator<TSource> concatOp,
            QuerySettings settings,
            bool preferStriping)
            : base(leftChildQueryResults, rightChildQueryResults, concatOp, settings, preferStriping)
        {
            m_concatOp = concatOp;
            m_leftChildCount = leftChildQueryResults.ElementsCount;
            m_rightChildCount = rightChildQueryResults.ElementsCount;
        }

        internal override bool IsIndexible => true;

        internal override int ElementsCount => m_leftChildCount + m_rightChildCount;

        public static QueryResults<TSource> NewResults(
            QueryResults<TSource> leftChildQueryResults,
            QueryResults<TSource> rightChildQueryResults,
            ConcatQueryOperator<TSource> op,
            QuerySettings settings,
            bool preferStriping)
        {
            return leftChildQueryResults.IsIndexible && rightChildQueryResults.IsIndexible
                ? new ConcatQueryOperatorResults(leftChildQueryResults, rightChildQueryResults, op, settings,
                    preferStriping)
                : (QueryResults<TSource>)new BinaryQueryOperatorResults(leftChildQueryResults, rightChildQueryResults,
                    op, settings, preferStriping);
        }

        internal override TSource GetElement(int index)
        {
            return index < m_leftChildCount
                ? m_leftChildQueryResults.GetElement(index)
                : m_rightChildQueryResults.GetElement(index - m_leftChildCount);
        }
    }
}