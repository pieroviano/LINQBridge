#nullable disable
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class LongMinMaxAggregationOperator : InlinedAggregationOperator<long, long, long>
{
    private readonly int m_sign;

    internal LongMinMaxAggregationOperator(IEnumerable<long> child, int sign)
        : base(child)
    {
        m_sign = sign;
    }

    protected override QueryOperatorEnumerator<long, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<long, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new LongMinMaxAggregationOperatorEnumerator<TKey>(source, index, m_sign, cancellationToken);
    }

    protected override long InternalAggregate(ref Exception singularExceptionToThrow)
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            if (!enumerator.MoveNext())
            {
                singularExceptionToThrow = new InvalidOperationException(Strings.NoElements());
                return 0;
            }

            var num = enumerator.Current;
            if (m_sign == -1)
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (current < num)
                    {
                        num = current;
                    }
                }
            }
            else
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (current > num)
                    {
                        num = current;
                    }
                }
            }

            return num;
        }
    }

    private class LongMinMaxAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<long>
    {
        private readonly int m_sign;
        private readonly QueryOperatorEnumerator<long, TKey> m_source;

        internal LongMinMaxAggregationOperatorEnumerator(
            QueryOperatorEnumerator<long, TKey> source,
            int partitionIndex,
            int sign,
            CancellationToken cancellationToken)
            : base(partitionIndex, cancellationToken)
        {
            m_source = source;
            m_sign = sign;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        protected override bool MoveNextCore(ref long currentElement)
        {
            var source = m_source;
            var currentKey = default(TKey);
            if (!source.MoveNext(ref currentElement, ref currentKey))
            {
                return false;
            }

            var num = 0;
            if (m_sign == -1)
            {
                long currentElement1 = 0;
                while (source.MoveNext(ref currentElement1, ref currentKey))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (currentElement1 < currentElement)
                    {
                        currentElement = currentElement1;
                    }
                }
            }
            else
            {
                long currentElement2 = 0;
                while (source.MoveNext(ref currentElement2, ref currentKey))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (currentElement2 > currentElement)
                    {
                        currentElement = currentElement2;
                    }
                }
            }

            return true;
        }
    }
}