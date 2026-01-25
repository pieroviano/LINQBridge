#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal class MergeExecutor<TInputOutput> : IEnumerable<TInputOutput>, IEnumerable
{
    private IMergeHelper<TInputOutput> m_mergeHelper;

    private MergeExecutor()
    {
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<TInputOutput> GetEnumerator()
    {
        return m_mergeHelper.GetEnumerator();
    }

    internal static MergeExecutor<TInputOutput> Execute<TKey>(
        PartitionedStream<TInputOutput, TKey> partitions,
        bool ignoreOutput,
        ParallelMergeOptions options,
        TaskScheduler taskScheduler,
        bool isOrdered,
        CancellationState cancellationState,
        int queryId)
    {
        var mergeExecutor = new MergeExecutor<TInputOutput>();
        if (isOrdered && !ignoreOutput)
        {
            if (options != ParallelMergeOptions.FullyBuffered &&
                !partitions.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing))
            {
                var autoBuffered = options == ParallelMergeOptions.AutoBuffered;
                mergeExecutor.m_mergeHelper = partitions.PartitionCount <= 1
                    ? new DefaultMergeHelper<TInputOutput, TKey>(partitions, false, options, taskScheduler,
                        cancellationState, queryId)
                    : new OrderPreservingPipeliningMergeHelper<TInputOutput, TKey>(partitions, taskScheduler,
                        cancellationState, autoBuffered, queryId, partitions.KeyComparer);
            }
            else
            {
                mergeExecutor.m_mergeHelper =
                    new OrderPreservingMergeHelper<TInputOutput, TKey>(partitions, taskScheduler, cancellationState,
                        queryId);
            }
        }
        else
        {
            mergeExecutor.m_mergeHelper = new DefaultMergeHelper<TInputOutput, TKey>(partitions, ignoreOutput, options,
                taskScheduler, cancellationState, queryId);
        }

        mergeExecutor.Execute();
        return mergeExecutor;
    }

    internal TInputOutput[] GetResultsAsArray()
    {
        return m_mergeHelper.GetResultsAsArray();
    }

    internal static AsynchronousChannel<TInputOutput>[] MakeAsynchronousChannels(
        int partitionCount,
        ParallelMergeOptions options,
        IntValueEvent consumerEvent,
        CancellationToken cancellationToken)
    {
        var asynchronousChannelArray = new AsynchronousChannel<TInputOutput>[partitionCount];
        var chunkSize = 0;
        if (options == ParallelMergeOptions.NotBuffered)
        {
            chunkSize = 1;
        }

        for (var index = 0; index < asynchronousChannelArray.Length; ++index)
        {
            asynchronousChannelArray[index] =
                new AsynchronousChannel<TInputOutput>(index, chunkSize, cancellationToken, consumerEvent);
        }

        return asynchronousChannelArray;
    }

    internal static SynchronousChannel<TInputOutput>[] MakeSynchronousChannels(int partitionCount)
    {
        var synchronousChannelArray = new SynchronousChannel<TInputOutput>[partitionCount];
        for (var index = 0; index < synchronousChannelArray.Length; ++index)
        {
            synchronousChannelArray[index] = new SynchronousChannel<TInputOutput>();
        }

        return synchronousChannelArray;
    }

    private void Execute()
    {
        m_mergeHelper.Execute();
    }
}