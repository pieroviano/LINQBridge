#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class AnyAllSearchOperator<TInput> : UnaryQueryOperator<TInput, bool>
{
    private readonly Func<TInput, bool> m_predicate;
    private readonly bool m_qualification;

    internal AnyAllSearchOperator(
        IEnumerable<TInput> child,
        bool qualification,
        Func<TInput, bool> predicate)
        : base(child)
    {
        m_qualification = qualification;
        m_predicate = predicate;
    }

    internal override bool LimitsParallelism => false;

    internal bool Aggregate()
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            while (enumerator.MoveNext())
            {
                if (enumerator.Current == m_qualification)
                {
                    return m_qualification;
                }
            }
        }

        return !m_qualification;
    }

    internal override IEnumerable<bool> AsSequentialQuery(CancellationToken token)
    {
        throw new NotSupportedException();
    }

    internal override QueryResults<bool> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, preferStriping), this, settings, preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TInput, TKey> inputStream,
        IPartitionedStreamRecipient<bool> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var resultFoundFlag = new Shared<bool>(false);
        var partitionCount = inputStream.PartitionCount;
        var partitionedStream =
            new PartitionedStream<bool, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = new AnyAllSearchOperatorEnumerator<TKey>(inputStream[index], m_qualification,
                m_predicate, index, resultFoundFlag, settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream);
    }

    private class AnyAllSearchOperatorEnumerator<TKey> : QueryOperatorEnumerator<bool, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly int m_partitionIndex;
        private readonly Func<TInput, bool> m_predicate;
        private readonly bool m_qualification;
        private readonly Shared<bool> m_resultFoundFlag;
        private readonly QueryOperatorEnumerator<TInput, TKey> m_source;

        internal AnyAllSearchOperatorEnumerator(
            QueryOperatorEnumerator<TInput, TKey> source,
            bool qualification,
            Func<TInput, bool> predicate,
            int partitionIndex,
            Shared<bool> resultFoundFlag,
            CancellationToken cancellationToken)
        {
            m_source = source;
            m_qualification = qualification;
            m_predicate = predicate;
            m_partitionIndex = partitionIndex;
            m_resultFoundFlag = resultFoundFlag;
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref bool currentElement, ref int currentKey)
        {
            if (m_resultFoundFlag.Value)
            {
                return false;
            }

            var currentElement1 = default(TInput);
            var currentKey1 = default(TKey);
            if (!m_source.MoveNext(ref currentElement1, ref currentKey1))
            {
                return false;
            }

            currentElement = !m_qualification;
            currentKey = m_partitionIndex;
            var num = 0;
            do
            {
                if ((num++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                if (m_resultFoundFlag.Value)
                {
                    return false;
                }

                if (m_predicate(currentElement1) == m_qualification)
                {
                    m_resultFoundFlag.Value = true;
                    currentElement = m_qualification;
                    break;
                }
            } while (m_source.MoveNext(ref currentElement1, ref currentKey1));

            return true;
        }
    }
}