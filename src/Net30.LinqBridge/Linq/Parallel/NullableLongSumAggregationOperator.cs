#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableLongSumAggregationOperator :
    InlinedAggregationOperator<long?, long?, long?>
{
    internal NullableLongSumAggregationOperator(IEnumerable<long?> child)
        : base(child)
    {
    }

    protected override QueryOperatorEnumerator<long?, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<long?, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new NullableLongSumAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
    }

    protected override long? InternalAggregate(ref Exception singularExceptionToThrow)
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            long num = 0;
            while (enumerator.MoveNext())
            {
                checked
                {
                    num += enumerator.Current.GetValueOrDefault();
                }
            }

            return num;
        }
    }

    private class NullableLongSumAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<long?>
    {
        private readonly QueryOperatorEnumerator<long?, TKey> m_source;

        internal NullableLongSumAggregationOperatorEnumerator(
            QueryOperatorEnumerator<long?, TKey> source,
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

        protected override bool MoveNextCore(ref long? currentElement)
        {
            var currentElement1 = new long?();
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
                    num1 += currentElement1.GetValueOrDefault();
                }
            } while (source.MoveNext(ref currentElement1, ref currentKey));

            currentElement = num1;
            return true;
        }
    }
}