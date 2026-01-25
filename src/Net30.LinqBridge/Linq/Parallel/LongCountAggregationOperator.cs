#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class LongCountAggregationOperator<TSource> :
    InlinedAggregationOperator<TSource, long, long>
{
    internal LongCountAggregationOperator(IEnumerable<TSource> child)
        : base(child)
    {
    }

    protected override QueryOperatorEnumerator<long, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<TSource, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new LongCountAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
    }

    protected override long InternalAggregate(ref Exception singularExceptionToThrow)
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            long num = 0;
            while (enumerator.MoveNext())
            {
                checked
                {
                    num += enumerator.Current;
                }
            }

            return num;
        }
    }

    private class LongCountAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<long>
    {
        private readonly QueryOperatorEnumerator<TSource, TKey> m_source;

        internal LongCountAggregationOperatorEnumerator(
            QueryOperatorEnumerator<TSource, TKey> source,
            int partitionIndex,
            CancellationToken cancellationToken)
            : base(partitionIndex, cancellationToken)
        {
            m_source = source;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        protected override bool MoveNextCore(ref long currentElement)
        {
            var currentElement1 = default(TSource);
            var currentKey = default(TKey);
            var source = m_source;
            if (!source.MoveNext(ref currentElement1, ref currentKey))
            {
                return false;
            }

            long num1 = 0;
            var num2 = 0;
            do
            {
                if ((num2++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                checked
                {
                    ++num1;
                }
            } while (source.MoveNext(ref currentElement1, ref currentKey));

            currentElement = num1;
            return true;
        }
    }
}