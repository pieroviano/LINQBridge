#nullable disable
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class LongAverageAggregationOperator :
    InlinedAggregationOperator<long, Pair<long, long>, double>
{
    internal LongAverageAggregationOperator(IEnumerable<long> child)
        : base(child)
    {
    }

    protected override QueryOperatorEnumerator<Pair<long, long>, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<long, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new LongAverageAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
    }

    protected override double InternalAggregate(ref Exception singularExceptionToThrow)
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            if (!enumerator.MoveNext())
            {
                singularExceptionToThrow = new InvalidOperationException(Strings.NoElements());
                return 0.0;
            }

            var current1 = enumerator.Current;
            while (enumerator.MoveNext())
            {
                ref var local1 = ref current1;
                var first1 = local1.First;
                var current2 = enumerator.Current;
                var first2 = current2.First;
                local1.First = checked(first1 + first2);
                ref var local2 = ref current1;
                var second1 = local2.Second;
                current2 = enumerator.Current;
                var second2 = current2.Second;
                local2.Second = checked(second1 + second2);
            }

            return current1.First / (double)current1.Second;
        }
    }

    private class LongAverageAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<Pair<long, long>>
    {
        private readonly QueryOperatorEnumerator<long, TKey> m_source;

        internal LongAverageAggregationOperatorEnumerator(
            QueryOperatorEnumerator<long, TKey> source,
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

        protected override bool MoveNextCore(ref Pair<long, long> currentElement)
        {
            long first = 0;
            long second = 0;
            var source = m_source;
            long currentElement1 = 0;
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

                checked
                {
                    first += currentElement1;
                }

                checked
                {
                    ++second;
                }
            } while (source.MoveNext(ref currentElement1, ref currentKey));

            currentElement = new Pair<long, long>(first, second);
            return true;
        }
    }
}