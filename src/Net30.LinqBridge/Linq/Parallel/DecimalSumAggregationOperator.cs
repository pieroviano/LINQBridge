#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DecimalSumAggregationOperator :
    InlinedAggregationOperator<decimal, decimal, decimal>
{
    internal DecimalSumAggregationOperator(IEnumerable<decimal> child)
        : base(child)
    {
    }

    protected override QueryOperatorEnumerator<decimal, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<decimal, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new DecimalSumAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
    }

    protected override decimal InternalAggregate(ref Exception singularExceptionToThrow)
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            var num = 0.0M;
            while (enumerator.MoveNext())
            {
                num += enumerator.Current;
            }

            return num;
        }
    }

    private class DecimalSumAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<decimal>
    {
        private readonly QueryOperatorEnumerator<decimal, TKey> m_source;

        internal DecimalSumAggregationOperatorEnumerator(
            QueryOperatorEnumerator<decimal, TKey> source,
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

        protected override bool MoveNextCore(ref decimal currentElement)
        {
            var currentElement1 = 0M;
            var currentKey = default(TKey);
            var source = m_source;
            if (!source.MoveNext(ref currentElement1, ref currentKey))
            {
                return false;
            }

            var num1 = 0.0M;
            var num2 = 0;
            do
            {
                if ((num2++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                num1 += currentElement1;
            } while (source.MoveNext(ref currentElement1, ref currentKey));

            currentElement = num1;
            return true;
        }
    }
}