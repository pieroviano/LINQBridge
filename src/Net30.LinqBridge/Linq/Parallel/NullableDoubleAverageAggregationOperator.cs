#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableDoubleAverageAggregationOperator :
    InlinedAggregationOperator<double?, Pair<double, long>, double?>
{
    internal NullableDoubleAverageAggregationOperator(IEnumerable<double?> child)
        : base(child)
    {
    }

    protected override QueryOperatorEnumerator<Pair<double, long>, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<double?, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new NullableDoubleAverageAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
    }

    protected override double? InternalAggregate(ref Exception singularExceptionToThrow)
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            if (!enumerator.MoveNext())
            {
                return new double?();
            }

            var current1 = enumerator.Current;
            while (enumerator.MoveNext())
            {
                ref var local1 = ref current1;
                var first1 = local1.First;
                var current2 = enumerator.Current;
                var first2 = current2.First;
                local1.First = first1 + first2;
                ref var local2 = ref current1;
                var second1 = local2.Second;
                current2 = enumerator.Current;
                var second2 = current2.Second;
                local2.Second = checked(second1 + second2);
            }

            return current1.First / current1.Second;
        }
    }

    private class NullableDoubleAverageAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<Pair<double, long>>
    {
        private readonly QueryOperatorEnumerator<double?, TKey> m_source;

        internal NullableDoubleAverageAggregationOperatorEnumerator(
            QueryOperatorEnumerator<double?, TKey> source,
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

        protected override bool MoveNextCore(ref Pair<double, long> currentElement)
        {
            var first = 0.0;
            long second = 0;
            var source = m_source;
            var currentElement1 = new double?();
            var currentKey = default(TKey);
            var num = 0;
            while (source.MoveNext(ref currentElement1, ref currentKey))
            {
                if (currentElement1.HasValue)
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    first += currentElement1.GetValueOrDefault();
                    ++second;
                }
            }

            currentElement = new Pair<double, long>(first, second);
            return second > 0L;
        }
    }
}