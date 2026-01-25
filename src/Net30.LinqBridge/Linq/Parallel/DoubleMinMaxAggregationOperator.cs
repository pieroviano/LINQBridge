#nullable disable
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class DoubleMinMaxAggregationOperator :
    InlinedAggregationOperator<double, double, double>
{
    private readonly int m_sign;

    internal DoubleMinMaxAggregationOperator(IEnumerable<double> child, int sign)
        : base(child)
    {
        m_sign = sign;
    }

    protected override QueryOperatorEnumerator<double, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<double, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new DoubleMinMaxAggregationOperatorEnumerator<TKey>(source, index, m_sign, cancellationToken);
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

            var d = enumerator.Current;
            if (m_sign == -1)
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (current < d || double.IsNaN(current))
                    {
                        d = current;
                    }
                }
            }
            else
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (current > d || double.IsNaN(d))
                    {
                        d = current;
                    }
                }
            }

            return d;
        }
    }

    private class DoubleMinMaxAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<double>
    {
        private readonly int m_sign;
        private readonly QueryOperatorEnumerator<double, TKey> m_source;

        internal DoubleMinMaxAggregationOperatorEnumerator(
            QueryOperatorEnumerator<double, TKey> source,
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

        protected override bool MoveNextCore(ref double currentElement)
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
                var currentElement1 = 0.0;
                while (source.MoveNext(ref currentElement1, ref currentKey))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (currentElement1 < currentElement || double.IsNaN(currentElement1))
                    {
                        currentElement = currentElement1;
                    }
                }
            }
            else
            {
                var currentElement2 = 0.0;
                while (source.MoveNext(ref currentElement2, ref currentKey))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (currentElement2 > currentElement || double.IsNaN(currentElement))
                    {
                        currentElement = currentElement2;
                    }
                }
            }

            return true;
        }
    }
}