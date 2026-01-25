#nullable disable
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal static class SpoolingTask
{
    internal static void SpoolForAll<TInputOutput, TIgnoreKey>(
        QueryTaskGroupState groupState,
        PartitionedStream<TInputOutput, TIgnoreKey> partitions,
        TaskScheduler taskScheduler)
    {
        var rootTask = new Task((Action)(() =>
        {
            var num = partitions.PartitionCount - 1;
            for (var index = 0; index < num; ++index)
            {
                new ForAllSpoolingTask<TInputOutput, TIgnoreKey>(index, groupState, partitions[index])
                    .RunAsynchronously(taskScheduler);
            }

            new ForAllSpoolingTask<TInputOutput, TIgnoreKey>(num, groupState, partitions[num]).RunSynchronously(
                taskScheduler);
        }));
        groupState.QueryBegin(rootTask);
        rootTask.RunSynchronously(taskScheduler);
        groupState.QueryEnd(false);
    }

    internal static void SpoolPipeline<TInputOutput, TIgnoreKey>(
        QueryTaskGroupState groupState,
        PartitionedStream<TInputOutput, TIgnoreKey> partitions,
        AsynchronousChannel<TInputOutput>[] channels,
        TaskScheduler taskScheduler)
    {
        var rootTask = new Task((Action)(() =>
        {
            for (var index = 0; index < partitions.PartitionCount; ++index)
            {
                new PipelineSpoolingTask<TInputOutput, TIgnoreKey>(index, groupState, partitions[index],
                    channels[index]).RunAsynchronously(taskScheduler);
            }
        }));
        groupState.QueryBegin(rootTask);
        rootTask.Start(taskScheduler);
    }

    internal static void SpoolStopAndGo<TInputOutput, TIgnoreKey>(
        QueryTaskGroupState groupState,
        PartitionedStream<TInputOutput, TIgnoreKey> partitions,
        SynchronousChannel<TInputOutput>[] channels,
        TaskScheduler taskScheduler)
    {
        var rootTask = new Task((Action)(() =>
        {
            var index1 = partitions.PartitionCount - 1;
            for (var index2 = 0; index2 < index1; ++index2)
            {
                new StopAndGoSpoolingTask<TInputOutput, TIgnoreKey>(index2, groupState, partitions[index2],
                    channels[index2]).RunAsynchronously(taskScheduler);
            }

            new StopAndGoSpoolingTask<TInputOutput, TIgnoreKey>(index1, groupState, partitions[index1],
                channels[index1]).RunSynchronously(taskScheduler);
        }));
        groupState.QueryBegin(rootTask);
        rootTask.RunSynchronously(taskScheduler);
        groupState.QueryEnd(false);
    }
}