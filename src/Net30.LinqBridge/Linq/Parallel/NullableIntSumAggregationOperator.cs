#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableIntSumAggregationOperator :
    InlinedAggregationOperator<int?, int?, int?>
{
    internal NullableIntSumAggregationOperator(IEnumerable<int?> child)
        : base(child)
    {
    }

    protected override QueryOperatorEnumerator<int?, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<int?, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new NullableIntSumAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
    }

    protected override int? InternalAggregate(ref Exception singularExceptionToThrow)
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            var num = 0;
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

    private class NullableIntSumAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<int?>
    {
        private readonly QueryOperatorEnumerator<int?, TKey> m_source;

        internal NullableIntSumAggregationOperatorEnumerator(
            QueryOperatorEnumerator<int?, TKey> source,
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

        protected override bool MoveNextCore(ref int? currentElement)
        {
            var currentElement1 = new int?();
            var currentKey = default(TKey);
            var source = m_source;
            if (!source.MoveNext(ref currentElement1, ref currentKey))
            {
                return false;
            }

            var num1 = 0;
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