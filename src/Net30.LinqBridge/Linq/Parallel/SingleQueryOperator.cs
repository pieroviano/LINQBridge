#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class SingleQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
{
    private readonly Func<TSource, bool> m_predicate;

    internal SingleQueryOperator(IEnumerable<TSource> child, Func<TSource, bool> predicate)
        : base(child)
    {
        m_predicate = predicate;
    }

    internal override bool LimitsParallelism => false;

    internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
    {
        throw new NotSupportedException();
    }

    internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, false), this, settings, preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TSource, TKey> inputStream,
        IPartitionedStreamRecipient<TSource> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var partitionedStream = new PartitionedStream<TSource, int>(partitionCount, Util.GetDefaultComparer<int>(),
            OrdinalIndexState.Shuffled);
        var totalElementCount = new Shared<int>(0);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] =
                new SingleQueryOperatorEnumerator<TKey>(inputStream[index], m_predicate, totalElementCount);
        }

        recipient.Receive(partitionedStream);
    }

    private class SingleQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, int>
    {
        private readonly Func<TSource, bool> m_predicate;
        private readonly QueryOperatorEnumerator<TSource, TKey> m_source;
        private readonly Shared<int> m_totalElementCount;
        private bool m_alreadySearched;
        private bool m_yieldExtra;

        internal SingleQueryOperatorEnumerator(
            QueryOperatorEnumerator<TSource, TKey> source,
            Func<TSource, bool> predicate,
            Shared<int> totalElementCount)
        {
            m_source = source;
            m_predicate = predicate;
            m_totalElementCount = totalElementCount;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TSource currentElement, ref int currentKey)
        {
            if (m_alreadySearched)
            {
                if (!m_yieldExtra)
                {
                    return false;
                }

                m_yieldExtra = false;
                currentElement = default;
                currentKey = 0;
                return true;
            }

            var flag = false;
            var currentElement1 = default(TSource);
            var currentKey1 = default(TKey);
            while (m_source.MoveNext(ref currentElement1, ref currentKey1))
            {
                if (m_predicate == null || m_predicate(currentElement1))
                {
                    Interlocked.Increment(ref m_totalElementCount.Value);
                    currentElement = currentElement1;
                    currentKey = 0;
                    if (flag)
                    {
                        m_yieldExtra = true;
                        break;
                    }

                    flag = true;
                }
#pragma warning disable CS0436
                if (Volatile.Read(ref m_totalElementCount.Value) > 1)
                {
                    break;
                }
#pragma warning restore CS0436
            }

            m_alreadySearched = true;
            return flag;
        }
    }
}