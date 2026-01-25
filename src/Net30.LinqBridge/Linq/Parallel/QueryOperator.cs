#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal abstract class QueryOperator<TOutput> : ParallelQuery<TOutput>
{
    protected bool m_outputOrdered;

    internal QueryOperator(QuerySettings settings)
        : this(false, settings)
    {
    }

    internal QueryOperator(bool isOrdered, QuerySettings settings)
        : base(settings)
    {
        m_outputOrdered = isOrdered;
    }

    internal bool OutputOrdered => m_outputOrdered;

    internal abstract bool LimitsParallelism { get; }

    internal abstract OrdinalIndexState OrdinalIndexState { get; }

    public override IEnumerator<TOutput> GetEnumerator()
    {
        return GetEnumerator(new ParallelMergeOptions?(), false);
    }

    public IEnumerator<TOutput> GetEnumerator(ParallelMergeOptions? mergeOptions)
    {
        return GetEnumerator(mergeOptions, false);
    }

    internal static QueryOperator<TOutput> AsQueryOperator(IEnumerable<TOutput> source)
    {
        var scanQueryOperator = source as QueryOperator<TOutput>;
        if (scanQueryOperator == null)
        {
            var orderedParallelQuery = source as OrderedParallelQuery<TOutput>;
            if (orderedParallelQuery == null)
            {
                scanQueryOperator = new ScanQueryOperator<TOutput>(source);
            }
            else
            {
                scanQueryOperator = orderedParallelQuery.SortOperator;
            }
        }

        return scanQueryOperator;
    }

    internal abstract IEnumerable<TOutput> AsSequentialQuery(CancellationToken token);

    internal static ListQueryResults<TOutput> ExecuteAndCollectResults<TKey>(
        PartitionedStream<TOutput, TKey> openedChild,
        int partitionCount,
        bool outputOrdered,
        bool useStriping,
        QuerySettings settings)
    {
        var taskScheduler = settings.TaskScheduler;
        return new ListQueryResults<TOutput>(
            MergeExecutor<TOutput>.Execute(openedChild, false, ParallelMergeOptions.FullyBuffered, taskScheduler,
                outputOrdered, settings.CancellationState, settings.QueryId).GetResultsAsArray(), partitionCount,
            useStriping);
    }

    internal TOutput[] ExecuteAndGetResultsAsArray()
    {
        var querySettings1 = SpecifiedQuerySettings;
        querySettings1 = querySettings1.WithPerExecutionSettings();
        var querySettings2 = querySettings1.WithDefaults();
        QueryLifecycle.LogicalQueryExecutionBegin(querySettings2.QueryId);
        try
        {
            if (querySettings2.ExecutionMode.Value == ParallelExecutionMode.Default && LimitsParallelism)
            {
                return ExceptionAggregator
                    .WrapEnumerable(
                        CancellableEnumerable.Wrap(
                            AsSequentialQuery(querySettings2.CancellationState.ExternalCancellationToken),
                            querySettings2.CancellationState.ExternalCancellationToken),
                        querySettings2.CancellationState).ToArray();
            }

            var queryResults = GetQueryResults(querySettings2);
            if (queryResults.IsIndexible && OutputOrdered)
            {
                var arrayMergeHelper = new ArrayMergeHelper<TOutput>(SpecifiedQuerySettings, queryResults);
                arrayMergeHelper.Execute();
                var resultsAsArray = arrayMergeHelper.GetResultsAsArray();
                querySettings2.CleanStateAtQueryEnd();
                return resultsAsArray;
            }

            var recipient = new PartitionedStreamMerger<TOutput>(false, ParallelMergeOptions.FullyBuffered,
                querySettings2.TaskScheduler, OutputOrdered, querySettings2.CancellationState, querySettings2.QueryId);
            queryResults.GivePartitionedStream(recipient);
            var resultsAsArray1 = recipient.MergeExecutor.GetResultsAsArray();
            querySettings2.CleanStateAtQueryEnd();
            return resultsAsArray1;
        }
        finally
        {
            QueryLifecycle.LogicalQueryExecutionEnd(querySettings2.QueryId);
        }
    }

    internal virtual IEnumerator<TOutput> GetEnumerator(
        ParallelMergeOptions? mergeOptions,
        bool suppressOrderPreservation)
    {
        return new QueryOpeningEnumerator<TOutput>(this, mergeOptions, suppressOrderPreservation);
    }

    internal IEnumerator<TOutput> GetOpenedEnumerator(
        ParallelMergeOptions? mergeOptions,
        bool suppressOrder,
        bool forEffect,
        QuerySettings querySettings)
    {
        if (querySettings.ExecutionMode.Value == ParallelExecutionMode.Default && LimitsParallelism)
        {
            return ExceptionAggregator
                .WrapEnumerable(AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken),
                    querySettings.CancellationState).GetEnumerator();
        }

        var queryResults = GetQueryResults(querySettings);
        if (!mergeOptions.HasValue)
        {
            mergeOptions = querySettings.MergeOptions;
        }

        if (querySettings.CancellationState.MergedCancellationToken.IsCancellationRequested)
        {
            if (querySettings.CancellationState.ExternalCancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            throw new OperationCanceledException();
        }

        var outputOrdered = OutputOrdered && !suppressOrder;
        var recipient = new PartitionedStreamMerger<TOutput>(forEffect, mergeOptions.GetValueOrDefault(),
            querySettings.TaskScheduler, outputOrdered, querySettings.CancellationState, querySettings.QueryId);
        queryResults.GivePartitionedStream(recipient);
        return forEffect ? null : recipient.MergeExecutor.GetEnumerator();
    }

    internal abstract QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping);

    private QueryResults<TOutput> GetQueryResults(QuerySettings querySettings)
    {
        return Open(querySettings, false);
    }
}