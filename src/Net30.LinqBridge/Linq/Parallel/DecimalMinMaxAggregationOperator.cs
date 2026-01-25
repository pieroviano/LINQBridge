#nullable disable
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DecimalMinMaxAggregationOperator :
    InlinedAggregationOperator<decimal, decimal, decimal>
{
    private readonly int m_sign;

    internal DecimalMinMaxAggregationOperator(IEnumerable<decimal> child, int sign)
        : base(child)
    {
        m_sign = sign;
    }

    protected override QueryOperatorEnumerator<decimal, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<decimal, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new DecimalMinMaxAggregationOperatorEnumerator<TKey>(source, index, m_sign, cancellationToken);
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

    private class DecimalMinMaxAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<decimal>
    {
        private readonly int m_sign;
        private readonly QueryOperatorEnumerator<decimal, TKey> m_source;

        internal DecimalMinMaxAggregationOperatorEnumerator(
            QueryOperatorEnumerator<decimal, TKey> source,
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

        protected override bool MoveNextCore(ref decimal currentElement)
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
                var currentElement1 = 0M;
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
                var currentElement2 = 0M;
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