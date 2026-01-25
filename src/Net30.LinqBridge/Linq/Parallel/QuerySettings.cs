#nullable disable
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal struct QuerySettings
{
    internal CancellationState CancellationState { get; set; }

    internal TaskScheduler TaskScheduler { get; set; }

    internal int? DegreeOfParallelism { get; set; }

    internal ParallelExecutionMode? ExecutionMode { get; set; }

    internal ParallelMergeOptions? MergeOptions { get; set; }

    internal int QueryId { get; private set; }

    internal QuerySettings(
        TaskScheduler taskScheduler,
        int? degreeOfParallelism,
        CancellationToken externalCancellationToken,
        ParallelExecutionMode? executionMode,
        ParallelMergeOptions? mergeOptions)
    {
        TaskScheduler = taskScheduler;
        DegreeOfParallelism = degreeOfParallelism;
        CancellationState = new CancellationState(externalCancellationToken);
        ExecutionMode = executionMode;
        MergeOptions = mergeOptions;
        QueryId = -1;
    }

    internal QuerySettings Merge(QuerySettings settings2)
    {
        if (TaskScheduler != null && settings2.TaskScheduler != null)
        {
            throw new InvalidOperationException(Strings.ParallelQuery_DuplicateTaskScheduler());
        }

        if (DegreeOfParallelism.HasValue && settings2.DegreeOfParallelism.HasValue)
        {
            throw new InvalidOperationException(Strings.ParallelQuery_DuplicateDOP());
        }

        if (CancellationState.ExternalCancellationToken.CanBeCanceled &&
            settings2.CancellationState.ExternalCancellationToken.CanBeCanceled)
        {
            throw new InvalidOperationException(Strings.ParallelQuery_DuplicateWithCancellation());
        }

        if (ExecutionMode.HasValue && settings2.ExecutionMode.HasValue)
        {
            throw new InvalidOperationException(Strings.ParallelQuery_DuplicateExecutionMode());
        }

        if (MergeOptions.HasValue && settings2.MergeOptions.HasValue)
        {
            throw new InvalidOperationException(Strings.ParallelQuery_DuplicateMergeOptions());
        }

        return new QuerySettings(TaskScheduler == null ? settings2.TaskScheduler : TaskScheduler,
            DegreeOfParallelism.HasValue ? DegreeOfParallelism : settings2.DegreeOfParallelism,
            CancellationState.ExternalCancellationToken.CanBeCanceled
                ? CancellationState.ExternalCancellationToken
                : settings2.CancellationState.ExternalCancellationToken,
            ExecutionMode.HasValue ? ExecutionMode : settings2.ExecutionMode,
            MergeOptions.HasValue ? MergeOptions : settings2.MergeOptions);
    }

    internal QuerySettings WithPerExecutionSettings()
    {
        return WithPerExecutionSettings(new CancellationTokenSource(), new Shared<bool>(false));
    }

    internal QuerySettings WithPerExecutionSettings(
        CancellationTokenSource topLevelCancellationTokenSource,
        Shared<bool> topLevelDisposedFlag)
    {
        var querySettings = new QuerySettings(TaskScheduler, DegreeOfParallelism,
            CancellationState.ExternalCancellationToken, ExecutionMode, MergeOptions)
        {
            CancellationState =
            {
                InternalCancellationTokenSource = topLevelCancellationTokenSource
            }
        };
        querySettings.CancellationState.MergedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            querySettings.CancellationState.InternalCancellationTokenSource.Token,
            querySettings.CancellationState.ExternalCancellationToken);
        querySettings.CancellationState.TopLevelDisposedFlag = topLevelDisposedFlag;
        querySettings.QueryId = PlinqEtwProvider.NextQueryId();
        return querySettings;
    }

    internal QuerySettings WithDefaults()
    {
        var querySettings = this;
        if (querySettings.TaskScheduler == null)
        {
            querySettings.TaskScheduler = TaskScheduler.Default;
        }

        if (!querySettings.DegreeOfParallelism.HasValue)
        {
            querySettings.DegreeOfParallelism = Scheduling.GetDefaultDegreeOfParallelism();
        }

        if (!querySettings.ExecutionMode.HasValue)
        {
            querySettings.ExecutionMode = ParallelExecutionMode.Default;
        }

        if (!querySettings.MergeOptions.HasValue)
        {
            querySettings.MergeOptions = ParallelMergeOptions.Default;
        }

        var mergeOptions = querySettings.MergeOptions;
        var parallelMergeOptions = ParallelMergeOptions.Default;
        if ((mergeOptions.GetValueOrDefault() == parallelMergeOptions) & mergeOptions.HasValue)
        {
            querySettings.MergeOptions = ParallelMergeOptions.AutoBuffered;
        }

        return querySettings;
    }

    internal static QuerySettings Empty => new(null, new int?(), new CancellationToken(), new ParallelExecutionMode?(),
        new ParallelMergeOptions?());

    public void CleanStateAtQueryEnd()
    {
        CancellationState.MergedCancellationTokenSource.Dispose();
    }
}

internal class PlinqEtwProvider
{
    private static int s_queryId;

    internal static int NextQueryId()
    {
        return Interlocked.Increment(ref s_queryId);
    }
}