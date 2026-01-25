#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ForAllOperator<TInput> : UnaryQueryOperator<TInput, TInput>
{
    private readonly Action<TInput> m_elementAction;

    internal ForAllOperator(IEnumerable<TInput> child, Action<TInput> elementAction)
        : base(child)
    {
        m_elementAction = elementAction;
    }

    internal override bool LimitsParallelism => false;

    internal override IEnumerable<TInput> AsSequentialQuery(CancellationToken token)
    {
        throw new InvalidOperationException();
    }

    internal override QueryResults<TInput> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, preferStriping), this, settings, preferStriping);
    }

    internal void RunSynchronously()
    {
        var topLevelDisposedFlag = new Shared<bool>(false);
        var topLevelCancellationTokenSource = new CancellationTokenSource();
        var querySettings1 = SpecifiedQuerySettings;
        querySettings1 = querySettings1.WithPerExecutionSettings(topLevelCancellationTokenSource, topLevelDisposedFlag);
        var querySettings2 = querySettings1.WithDefaults();
        QueryLifecycle.LogicalQueryExecutionBegin(querySettings2.QueryId);
        GetOpenedEnumerator(ParallelMergeOptions.FullyBuffered, true, true, querySettings2);
        querySettings2.CleanStateAtQueryEnd();
        QueryLifecycle.LogicalQueryExecutionEnd(querySettings2.QueryId);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TInput, TKey> inputStream,
        IPartitionedStreamRecipient<TInput> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var partitionedStream = new PartitionedStream<TInput, int>(partitionCount, Util.GetDefaultComparer<int>(),
            OrdinalIndexState.Correct);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = new ForAllEnumerator<TKey>(inputStream[index], m_elementAction,
                settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream);
    }

    private class ForAllEnumerator<TKey> : QueryOperatorEnumerator<TInput, int>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly Action<TInput> m_elementAction;
        private readonly QueryOperatorEnumerator<TInput, TKey> m_source;

        internal ForAllEnumerator(
            QueryOperatorEnumerator<TInput, TKey> source,
            Action<TInput> elementAction,
            CancellationToken cancellationToken)
        {
            m_source = source;
            m_elementAction = elementAction;
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_source.Dispose();
        }

        internal override bool MoveNext(ref TInput currentElement, ref int currentKey)
        {
            var currentElement1 = default(TInput);
            var currentKey1 = default(TKey);
            var num = 0;
            while (m_source.MoveNext(ref currentElement1, ref currentKey1))
            {
                if ((num++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                m_elementAction(currentElement1);
            }

            return false;
        }
    }
}