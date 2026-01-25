#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class WhereQueryOperator<TInputOutput> :
    UnaryQueryOperator<TInputOutput, TInputOutput>
{
    private readonly Func<TInputOutput, bool> m_predicate;

    internal WhereQueryOperator(IEnumerable<TInputOutput> child, Func<TInputOutput, bool> predicate)
        : base(child)
    {
        SetOrdinalIndexState(Child.OrdinalIndexState.Worse(OrdinalIndexState.Increasing));
        m_predicate = predicate;
    }

    internal override bool LimitsParallelism => false;

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
        var partitionedStream = new PartitionedStream<TInputOutput, TKey>(inputStream.PartitionCount,
            inputStream.KeyComparer, OrdinalIndexState);
        for (var index = 0; index < inputStream.PartitionCount; ++index)
        {
            partitionedStream[index] = new WhereQueryOperatorEnumerator<TKey>(inputStream[index], m_predicate,
                settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream);
    }

    private class WhereQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TInputOutput, TKey>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly Func<TInputOutput, bool> m_predicate;
        private readonly QueryOperatorEnumerator<TInputOutput, TKey> m_source;
        private Shared<int> m_outputLoopCount;

        internal WhereQueryOperatorEnumerator(
            QueryOperatorEnumerator<TInputOutput, TKey> source,
            Func<TInputOutput, bool> predicate,
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

        internal override bool MoveNext(ref TInputOutput currentElement, ref TKey currentKey)
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

                if (m_predicate(currentElement))
                {
                    return true;
                }
            }

            return false;
        }
    }
}