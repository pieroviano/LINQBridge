#nullable disable
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal class OrderPreservingSpoolingTask<TInputOutput, TKey> : SpoolingTaskBase
{
    private readonly Shared<TInputOutput[]> m_results;
    private readonly SortHelper<TInputOutput> m_sortHelper;

    private OrderPreservingSpoolingTask(
        int taskIndex,
        QueryTaskGroupState groupState,
        Shared<TInputOutput[]> results,
        SortHelper<TInputOutput> sortHelper)
        : base(taskIndex, groupState)
    {
        m_results = results;
        m_sortHelper = sortHelper;
    }

    protected override void SpoolingWork()
    {
        var inputOutputArray = m_sortHelper.Sort();
        if (m_groupState.CancellationState.MergedCancellationToken.IsCancellationRequested || m_taskIndex != 0)
        {
            return;
        }

        m_results.Value = inputOutputArray;
    }

    internal static void Spool(
        QueryTaskGroupState groupState,
        PartitionedStream<TInputOutput, TKey> partitions,
        Shared<TInputOutput[]> results,
        TaskScheduler taskScheduler)
    {
        var maxToRunInParallel = partitions.PartitionCount - 1;
        var sortHelpers = SortHelper<TInputOutput, TKey>.GenerateSortHelpers(partitions, groupState);
        var rootTask = new Task((Action)(() =>
        {
            for (var taskIndex = 0; taskIndex < maxToRunInParallel; ++taskIndex)
            {
                new OrderPreservingSpoolingTask<TInputOutput, TKey>(taskIndex, groupState, results,
                    sortHelpers[taskIndex]).RunAsynchronously(taskScheduler);
            }

            new OrderPreservingSpoolingTask<TInputOutput, TKey>(maxToRunInParallel, groupState, results,
                sortHelpers[maxToRunInParallel]).RunSynchronously(taskScheduler);
        }));
        groupState.QueryBegin(rootTask);
        rootTask.RunSynchronously(taskScheduler);
        for (var index = 0; index < sortHelpers.Length; ++index)
        {
            sortHelpers[index].Dispose();
        }

        groupState.QueryEnd(false);
    }
}