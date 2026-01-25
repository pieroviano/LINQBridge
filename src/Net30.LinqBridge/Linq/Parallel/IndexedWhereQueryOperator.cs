#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class IndexedWhereQueryOperator<TInputOutput> :
    UnaryQueryOperator<TInputOutput, TInputOutput>
{
    private readonly Func<TInputOutput, int, bool> m_predicate;
    private bool m_limitsParallelism;
    private bool m_prematureMerge;

    internal IndexedWhereQueryOperator(
        IEnumerable<TInputOutput> child,
        Func<TInputOutput, int, bool> predicate)
        : base(child)
    {
        m_predicate = predicate;
        m_outputOrdered = true;
        InitOrdinalIndexState();
    }

    internal override bool LimitsParallelism => m_limitsParallelism;

    internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
    {
        return CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token).Where(m_predicate);
    }

    internal override QueryResults<TInputOutput> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, preferStriping), this, settings, preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TInputOutput, TKey> inputStream,
        IPartitionedStreamRecipient<TInputOutput> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var partitionedStream1 = !m_prematureMerge
            ? (PartitionedStream<TInputOutput, int>)(object)inputStream
            : ExecuteAndCollectResults(inputStream, partitionCount, Child.OutputOrdered, preferStriping, settings)
                .GetPartitionedStream();
        var partitionedStream2 =
            new PartitionedStream<TInputOutput, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream2[index] = new IndexedWhereQueryOperatorEnumerator(partitionedStream1[index], m_predicate,
                settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream2);
    }

    private void InitOrdinalIndexState()
    {
        var ordinalIndexState = Child.OrdinalIndexState;
        if (ordinalIndexState.IsWorseThan(OrdinalIndexState.Correct))
        {
            m_prematureMerge = true;
            m_limitsParallelism = ordinalIndexState != OrdinalIndexState.Shuffled;
        }

        SetOrdinalIndexState(OrdinalIndexState.Increasing);
    }

    private class IndexedWhereQueryOperatorEnumerator : QueryOperatorEnumerator<TInputOutput, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly Func<TInputOutput, int, bool> m_predicate;
        private readonly QueryOperatorEnumerator<TInputOutput, int> m_source;
        private Shared<int> m_outputLoopCount;

        internal IndexedWhereQueryOperatorEnumerator(
            QueryOperatorEnumerator<TInputOutput, int> source,
            Func<TInputOutput, int, bool> predicate,
            CancellationToken cancellationToken)
        {
            m_source = source;
            m_predicate = predicate;
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TInputOutput currentElement, ref int currentKey)
        {
            if (m_outputLoopCount == null)
            {
                m_outputLoopCount = new Shared<int>(0);
            }

            while (m_source.MoveNext(ref currentElement, ref currentKey))
            {
                if ((m_outputLoopCount.Value++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                if (m_predicate(currentElement, currentKey))
                {
                    return true;
                }
            }

            return false;
        }
    }
}