#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ContainsSearchOperator<TInput> : UnaryQueryOperator<TInput, bool>
{
    private readonly IEqualityComparer<TInput> m_comparer;
    private readonly TInput m_searchValue;

    internal ContainsSearchOperator(
        IEnumerable<TInput> child,
        TInput searchValue,
        IEqualityComparer<TInput> comparer)
        : base(child)
    {
        m_searchValue = searchValue;
        if (comparer == null)
        {
            m_comparer = EqualityComparer<TInput>.Default;
        }
        else
        {
            m_comparer = comparer;
        }
    }

    internal override bool LimitsParallelism => false;

    internal bool Aggregate()
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            while (enumerator.MoveNext())
            {
                if (enumerator.Current)
                {
                    return true;
                }
            }
        }

        return false;
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
        var partitionCount = inputStream.PartitionCount;
        var partitionedStream =
            new PartitionedStream<bool, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);
        var resultFoundFlag = new Shared<bool>(false);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = new ContainsSearchOperatorEnumerator<TKey>(inputStream[index], m_searchValue,
                m_comparer, index, resultFoundFlag, settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream);
    }

    private class ContainsSearchOperatorEnumerator<TKey> : QueryOperatorEnumerator<bool, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly IEqualityComparer<TInput> m_comparer;
        private readonly int m_partitionIndex;
        private readonly Shared<bool> m_resultFoundFlag;
        private readonly TInput m_searchValue;
        private readonly QueryOperatorEnumerator<TInput, TKey> m_source;

        internal ContainsSearchOperatorEnumerator(
            QueryOperatorEnumerator<TInput, TKey> source,
            TInput searchValue,
            IEqualityComparer<TInput> comparer,
            int partitionIndex,
            Shared<bool> resultFoundFlag,
            CancellationToken cancellationToken)
        {
            m_source = source;
            m_searchValue = searchValue;
            m_comparer = comparer;
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

            currentElement = false;
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

                if (m_comparer.Equals(currentElement1, m_searchValue))
                {
                    m_resultFoundFlag.Value = true;
                    currentElement = true;
                    break;
                }
            } while (m_source.MoveNext(ref currentElement1, ref currentKey1));

            return true;
        }
    }
}