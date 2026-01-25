#nullable disable
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class FloatMinMaxAggregationOperator :
    InlinedAggregationOperator<float, float, float>
{
    private readonly int m_sign;

    internal FloatMinMaxAggregationOperator(IEnumerable<float> child, int sign)
        : base(child)
    {
        m_sign = sign;
    }

    protected override QueryOperatorEnumerator<float, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<float, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new FloatMinMaxAggregationOperatorEnumerator<TKey>(source, index, m_sign, cancellationToken);
    }

    protected override float InternalAggregate(ref Exception singularExceptionToThrow)
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            if (!enumerator.MoveNext())
            {
                singularExceptionToThrow = new InvalidOperationException(Strings.NoElements());
                return 0.0f;
            }

            var f = enumerator.Current;
            if (m_sign == -1)
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (current < (double)f || float.IsNaN(current))
                    {
                        f = current;
                    }
                }
            }
            else
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (current > (double)f || float.IsNaN(f))
                    {
                        f = current;
                    }
                }
            }

            return f;
        }
    }

    private class FloatMinMaxAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<float>
    {
        private readonly int m_sign;
        private readonly QueryOperatorEnumerator<float, TKey> m_source;

        internal FloatMinMaxAggregationOperatorEnumerator(
            QueryOperatorEnumerator<float, TKey> source,
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

        protected override bool MoveNextCore(ref float currentElement)
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
                var currentElement1 = 0.0f;
                while (source.MoveNext(ref currentElement1, ref currentKey))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (currentElement1 < (double)currentElement || float.IsNaN(currentElement1))
                    {
                        currentElement = currentElement1;
                    }
                }
            }
            else
            {
                var currentElement2 = 0.0f;
                while (source.MoveNext(ref currentElement2, ref currentKey))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (currentElement2 > (double)currentElement || float.IsNaN(currentElement))
                    {
                        currentElement = currentElement2;
                    }
                }
            }

            return true;
        }
    }
}