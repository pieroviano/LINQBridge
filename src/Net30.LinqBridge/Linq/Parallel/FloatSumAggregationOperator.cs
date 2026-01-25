#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class FloatSumAggregationOperator : InlinedAggregationOperator<float, double, float>
{
    internal FloatSumAggregationOperator(IEnumerable<float> child)
        : base(child)
    {
    }

    protected override QueryOperatorEnumerator<double, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<float, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new FloatSumAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
    }

    protected override float InternalAggregate(ref Exception singularExceptionToThrow)
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            var num = 0.0;
            while (enumerator.MoveNext())
            {
                num += enumerator.Current;
            }

            return (float)num;
        }
    }

    private class FloatSumAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<double>
    {
        private readonly QueryOperatorEnumerator<float, TKey> m_source;

        internal FloatSumAggregationOperatorEnumerator(
            QueryOperatorEnumerator<float, TKey> source,
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

        protected override bool MoveNextCore(ref double currentElement)
        {
            var currentElement1 = 0.0f;
            var currentKey = default(TKey);
            var source = m_source;
            if (!source.MoveNext(ref currentElement1, ref currentKey))
            {
                return false;
            }

            var num1 = 0.0;
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