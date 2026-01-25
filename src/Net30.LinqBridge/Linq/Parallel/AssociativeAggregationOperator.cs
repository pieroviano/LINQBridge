#nullable disable
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class AssociativeAggregationOperator<TInput, TIntermediate, TOutput> :
    UnaryQueryOperator<TInput, TIntermediate>
{
    private readonly Func<TIntermediate, TIntermediate, TIntermediate> m_finalReduce;
    private readonly Func<TIntermediate, TInput, TIntermediate> m_intermediateReduce;
    private readonly Func<TIntermediate, TOutput> m_resultSelector;
    private readonly TIntermediate m_seed;
    private readonly Func<TIntermediate> m_seedFactory;
    private readonly bool m_seedIsSpecified;
    private readonly bool m_throwIfEmpty;

    internal AssociativeAggregationOperator(
        IEnumerable<TInput> child,
        TIntermediate seed,
        Func<TIntermediate> seedFactory,
        bool seedIsSpecified,
        Func<TIntermediate, TInput, TIntermediate> intermediateReduce,
        Func<TIntermediate, TIntermediate, TIntermediate> finalReduce,
        Func<TIntermediate, TOutput> resultSelector,
        bool throwIfEmpty,
        QueryAggregationOptions options)
        : base(child)
    {
        m_seed = seed;
        m_seedFactory = seedFactory;
        m_seedIsSpecified = seedIsSpecified;
        m_intermediateReduce = intermediateReduce;
        m_finalReduce = finalReduce;
        m_resultSelector = resultSelector;
        m_throwIfEmpty = throwIfEmpty;
    }

    internal override bool LimitsParallelism => false;

    internal TOutput Aggregate()
    {
        var intermediate = default(TIntermediate);
        var flag = false;
        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
        {
            while (enumerator.MoveNext())
            {
                if (flag)
                {
                    try
                    {
                        intermediate = m_finalReduce(intermediate, enumerator.Current);
                    }
                    catch (ThreadAbortException ex)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new AggregateException(ex);
                    }
                }
                else
                {
                    intermediate = enumerator.Current;
                    flag = true;
                }
            }

            if (!flag)
            {
                if (m_throwIfEmpty)
                {
                    throw new InvalidOperationException(Strings.NoElements());
                }

                intermediate = m_seedFactory == null ? m_seed : m_seedFactory();
            }
        }

        try
        {
            return m_resultSelector(intermediate);
        }
        catch (ThreadAbortException ex)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AggregateException(ex);
        }
    }

    internal override IEnumerable<TIntermediate> AsSequentialQuery(CancellationToken token)
    {
        throw new NotSupportedException();
    }

    internal override QueryResults<TIntermediate> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, preferStriping), this, settings, preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TInput, TKey> inputStream,
        IPartitionedStreamRecipient<TIntermediate> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var partitionedStream = new PartitionedStream<TIntermediate, int>(partitionCount,
            Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = new AssociativeAggregationOperatorEnumerator<TKey>(inputStream[index], this,
                index, settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream);
    }

    private class AssociativeAggregationOperatorEnumerator<TKey> :
        QueryOperatorEnumerator<TIntermediate, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly int m_partitionIndex;
        private readonly AssociativeAggregationOperator<TInput, TIntermediate, TOutput> m_reduceOperator;
        private readonly QueryOperatorEnumerator<TInput, TKey> m_source;
        private bool m_accumulated;

        internal AssociativeAggregationOperatorEnumerator(
            QueryOperatorEnumerator<TInput, TKey> source,
            AssociativeAggregationOperator<TInput, TIntermediate, TOutput> reduceOperator,
            int partitionIndex,
            CancellationToken cancellationToken)
        {
            m_source = source;
            m_reduceOperator = reduceOperator;
            m_partitionIndex = partitionIndex;
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TIntermediate currentElement, ref int currentKey)
        {
            if (m_accumulated)
            {
                return false;
            }

            m_accumulated = true;
            var flag = false;
            var intermediate1 = default(TIntermediate);
            TIntermediate intermediate2;
            if (m_reduceOperator.m_seedIsSpecified)
            {
                intermediate2 = m_reduceOperator.m_seedFactory == null
                    ? m_reduceOperator.m_seed
                    : m_reduceOperator.m_seedFactory();
            }
            else
            {
                var currentElement1 = default(TInput);
                var currentKey1 = default(TKey);
                if (!m_source.MoveNext(ref currentElement1, ref currentKey1))
                {
                    return false;
                }

                flag = true;
                intermediate2 = (TIntermediate)(object)currentElement1;
            }

            var currentElement2 = default(TInput);
            var currentKey2 = default(TKey);
            var num = 0;
            while (m_source.MoveNext(ref currentElement2, ref currentKey2))
            {
                if ((num++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                flag = true;
                intermediate2 = m_reduceOperator.m_intermediateReduce(intermediate2, currentElement2);
            }

            if (!flag)
            {
                return false;
            }

            currentElement = intermediate2;
            currentKey = m_partitionIndex;
            return true;
        }
    }
}