#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ElementAtQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
{
    private readonly int m_index;
    private readonly bool m_prematureMerge;

    internal ElementAtQueryOperator(IEnumerable<TSource> child, int index)
        : base(child)
    {
        m_index = index;
        var ordinalIndexState = Child.OrdinalIndexState;
        if (!ordinalIndexState.IsWorseThan(OrdinalIndexState.Correct))
        {
            return;
        }

        m_prematureMerge = true;
        LimitsParallelism = ordinalIndexState != OrdinalIndexState.Shuffled;
    }

    internal override bool LimitsParallelism { get; }

    internal bool Aggregate(out TSource result, bool withDefaultValue)
    {
        if (LimitsParallelism)
        {
            var querySettings = SpecifiedQuerySettings;
            querySettings = querySettings.WithDefaults();
            if (querySettings.ExecutionMode.Value != ParallelExecutionMode.ForceParallelism)
            {
                var cancellationState = SpecifiedQuerySettings.CancellationState;
                if (withDefaultValue)
                {
                    var source = CancellableEnumerable.Wrap(
                        Child.AsSequentialQuery(cancellationState.ExternalCancellationToken),
                        cancellationState.ExternalCancellationToken);
                    result = ExceptionAggregator.WrapEnumerable(source, cancellationState).ElementAtOrDefault(m_index);
                }
                else
                {
                    var source = CancellableEnumerable.Wrap(
                        Child.AsSequentialQuery(cancellationState.ExternalCancellationToken),
                        cancellationState.ExternalCancellationToken);
                    result = ExceptionAggregator.WrapEnumerable(source, cancellationState).ElementAt(m_index);
                }

                return true;
            }
        }

        using (var enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered))
        {
            if (enumerator.MoveNext())
            {
                var current = enumerator.Current;
                result = current;
                return true;
            }
        }

        result = default;
        return false;
    }

    internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
    {
        throw new NotSupportedException();
    }

    internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, false), this, settings, preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TSource, TKey> inputStream,
        IPartitionedStreamRecipient<TSource> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var partitionedStream1 = !m_prematureMerge
            ? (PartitionedStream<TSource, int>)(object)inputStream
            : ExecuteAndCollectResults(inputStream, partitionCount, Child.OutputOrdered, preferStriping, settings)
                .GetPartitionedStream();
        var resultFoundFlag = new Shared<bool>(false);
        var partitionedStream2 = new PartitionedStream<TSource, int>(partitionCount, Util.GetDefaultComparer<int>(),
            OrdinalIndexState.Correct);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream2[index] = new ElementAtQueryOperatorEnumerator(partitionedStream1[index], m_index,
                resultFoundFlag, settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream2);
    }

    private class ElementAtQueryOperatorEnumerator : QueryOperatorEnumerator<TSource, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly int m_index;
        private readonly Shared<bool> m_resultFoundFlag;
        private readonly QueryOperatorEnumerator<TSource, int> m_source;

        internal ElementAtQueryOperatorEnumerator(
            QueryOperatorEnumerator<TSource, int> source,
            int index,
            Shared<bool> resultFoundFlag,
            CancellationToken cancellationToken)
        {
            m_source = source;
            m_index = index;
            m_resultFoundFlag = resultFoundFlag;
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TSource currentElement, ref int currentKey)
        {
            var num = 0;
            while (m_source.MoveNext(ref currentElement, ref currentKey))
            {
                if ((num++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                if (!m_resultFoundFlag.Value)
                {
                    if (currentKey == m_index)
                    {
                        m_resultFoundFlag.Value = true;
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }

            return false;
        }
    }
}