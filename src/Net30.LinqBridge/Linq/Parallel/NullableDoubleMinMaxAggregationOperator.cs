#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class NullableDoubleMinMaxAggregationOperator :
    InlinedAggregationOperator<double?, double?, double?>
{
    private readonly int m_sign;

    internal NullableDoubleMinMaxAggregationOperator(IEnumerable<double?> child, int sign)
        : base(child)
    {
        m_sign = sign;
    }

    protected override QueryOperatorEnumerator<double?, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<double?, TKey> source,
        object sharedData,
        CancellationToken cancellationToken)
    {
        return new NullableDoubleMinMaxAggregationOperatorEnumerator<TKey>(source, index, m_sign, cancellationToken);
    }

    protected override double? InternalAggregate(ref Exception singularExceptionToThrow)
    {
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            if (!enumerator.MoveNext())
            {
                return new double?();
            }

            var nullable1 = enumerator.Current;
            if (m_sign == -1)
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (current.HasValue)
                    {
                        if (nullable1.HasValue)
                        {
                            var nullable2 = current;
                            var nullable3 = nullable1;
                            if (!((nullable2.GetValueOrDefault() < nullable3.GetValueOrDefault()) & nullable2.HasValue &
                                  nullable3.HasValue) && !double.IsNaN(current.GetValueOrDefault()))
                            {
                                continue;
                            }
                        }

                        nullable1 = current;
                    }
                }
            }
            else
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    if (current.HasValue)
                    {
                        if (nullable1.HasValue)
                        {
                            var nullable4 = current;
                            var nullable5 = nullable1;
                            if (!((nullable4.GetValueOrDefault() > nullable5.GetValueOrDefault()) & nullable4.HasValue &
                                  nullable5.HasValue) && !double.IsNaN(nullable1.GetValueOrDefault()))
                            {
                                continue;
                            }
                        }

                        nullable1 = current;
                    }
                }
            }

            return nullable1;
        }
    }

    private class NullableDoubleMinMaxAggregationOperatorEnumerator<TKey> :
        InlinedAggregationOperatorEnumerator<double?>
    {
        private readonly int m_sign;
        private readonly QueryOperatorEnumerator<double?, TKey> m_source;

        internal NullableDoubleMinMaxAggregationOperatorEnumerator(
            QueryOperatorEnumerator<double?, TKey> source,
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

        protected override bool MoveNextCore(ref double? currentElement)
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
                var currentElement1 = new double?();
                while (source.MoveNext(ref currentElement1, ref currentKey))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (currentElement1.HasValue)
                    {
                        if (currentElement.HasValue)
                        {
                            var nullable1 = currentElement1;
                            var nullable2 = currentElement;
                            if (!((nullable1.GetValueOrDefault() < nullable2.GetValueOrDefault()) & nullable1.HasValue &
                                  nullable2.HasValue) && !double.IsNaN(currentElement1.GetValueOrDefault()))
                            {
                                continue;
                            }
                        }

                        currentElement = currentElement1;
                    }
                }
            }
            else
            {
                var currentElement2 = new double?();
                while (source.MoveNext(ref currentElement2, ref currentKey))
                {
                    if ((num++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (currentElement2.HasValue)
                    {
                        if (currentElement.HasValue)
                        {
                            var nullable3 = currentElement2;
                            var nullable4 = currentElement;
                            if (!((nullable3.GetValueOrDefault() > nullable4.GetValueOrDefault()) & nullable3.HasValue &
                                  nullable4.HasValue) && !double.IsNaN(currentElement.GetValueOrDefault()))
                            {
                                continue;
                            }
                        }

                        currentElement = currentElement2;
                    }
                }
            }

            return true;
        }
    }
}