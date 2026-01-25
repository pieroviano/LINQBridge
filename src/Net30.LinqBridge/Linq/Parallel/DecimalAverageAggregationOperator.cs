#nullable disable
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DecimalAverageAggregationOperator :
    InlinedAggregationOperator<decimal, Pair<decimal, long>, decimal>
{
    internal DecimalAverageAggregationOperator(IEnumerable<decimal> child)
        : base(child)
    {
    }

    protected override QueryOperatorEnumerator<Pair<decimal, long>, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<decimal, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new DecimalAverageAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
    }

    protected override decimal InternalAggregate(ref Exception singularExceptionToThrow)
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            if (!enumerator.MoveNext())
            {
                singularExceptionToThrow = new InvalidOperationException(Strings.NoElements());
                return 0M;
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

    private class DecimalAverageAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<Pair<decimal, long>>
    {
        private readonly QueryOperatorEnumerator<decimal, TKey> m_source;

        internal DecimalAverageAggregationOperatorEnumerator(
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

        protected override bool MoveNextCore(ref Pair<decimal, long> currentElement)
        {
            var first = 0.0M;
            long second = 0;
            var source = m_source;
            var currentElement1 = 0M;
            var currentKey = default(TKey);
            if (!source.MoveNext(ref currentElement1, ref currentKey))
            {
                return false;
            }

            var num = 0;
            do
            {
                if ((num++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                first += currentElement1;
                checked
                {
                    ++second;
                }
            } while (source.MoveNext(ref currentElement1, ref currentKey));

            currentElement = new Pair<decimal, long>(first, second);
            return true;
        }
    }
}