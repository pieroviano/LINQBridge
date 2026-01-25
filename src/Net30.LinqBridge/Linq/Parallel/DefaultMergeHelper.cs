#nullable disable
using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal class DefaultMergeHelper<TInputOutput, TIgnoreKey> : IMergeHelper<TInputOutput>
{
    private readonly AsynchronousChannel<TInputOutput>[] m_asyncChannels;
    private readonly IEnumerator<TInputOutput> m_channelEnumerator;
    private readonly bool m_ignoreOutput;
    private readonly PartitionedStream<TInputOutput, TIgnoreKey> m_partitions;
    private readonly SynchronousChannel<TInputOutput>[] m_syncChannels;
    private readonly QueryTaskGroupState m_taskGroupState;
    private readonly TaskScheduler m_taskScheduler;

    internal DefaultMergeHelper(
        PartitionedStream<TInputOutput, TIgnoreKey> partitions,
        bool ignoreOutput,
        ParallelMergeOptions options,
        TaskScheduler taskScheduler,
        CancellationState cancellationState,
        int queryId)
    {
        m_taskGroupState = new QueryTaskGroupState(cancellationState, queryId);
        m_partitions = partitions;
        m_taskScheduler = taskScheduler;
        m_ignoreOutput = ignoreOutput;
        var consumerEvent = new IntValueEvent();
        if (ignoreOutput)
        {
            return;
        }

        if (options != ParallelMergeOptions.FullyBuffered)
        {
            if (partitions.PartitionCount > 1)
            {
                m_asyncChannels = MergeExecutor<TInputOutput>.MakeAsynchronousChannels(partitions.PartitionCount,
                    options, consumerEvent, cancellationState.MergedCancellationToken);
                m_channelEnumerator =
                    new AsynchronousChannelMergeEnumerator<TInputOutput>(m_taskGroupState, m_asyncChannels,
                        consumerEvent);
            }
            else
            {
                m_channelEnumerator = ExceptionAggregator
                    .WrapQueryEnumerator(partitions[0], m_taskGroupState.CancellationState).GetEnumerator();
            }
        }
        else
        {
            m_syncChannels = MergeExecutor<TInputOutput>.MakeSynchronousChannels(partitions.PartitionCount);
            m_channelEnumerator = new SynchronousChannelMergeEnumerator<TInputOutput>(m_taskGroupState, m_syncChannels);
        }
    }

    void IMergeHelper<TInputOutput>.Execute()
    {
        if (m_asyncChannels != null)
        {
            SpoolingTask.SpoolPipeline(m_taskGroupState, m_partitions, m_asyncChannels, m_taskScheduler);
        }
        else if (m_syncChannels != null)
        {
            SpoolingTask.SpoolStopAndGo(m_taskGroupState, m_partitions, m_syncChannels, m_taskScheduler);
        }
        else
        {
            if (!m_ignoreOutput)
            {
                return;
            }

            SpoolingTask.SpoolForAll(m_taskGroupState, m_partitions, m_taskScheduler);
        }
    }

    IEnumerator<TInputOutput> IMergeHelper<TInputOutput>.GetEnumerator()
    {
        return m_channelEnumerator;
    }

    public TInputOutput[] GetResultsAsArray()
    {
        if (m_syncChannels != null)
        {
            var length = 0;
            for (var index = 0; index < m_syncChannels.Length; ++index)
            {
                length += m_syncChannels[index].Count;
            }

            var array = new TInputOutput[length];
            var arrayIndex = 0;
            for (var index = 0; index < m_syncChannels.Length; ++index)
            {
                m_syncChannels[index].CopyTo(array, arrayIndex);
                arrayIndex += m_syncChannels[index].Count;
            }

            return array;
        }

        var inputOutputList = new List<TInputOutput>();
        foreach (var inputOutput in (IMergeHelper<TInputOutput>)this)
        {
            inputOutputList.Add(inputOutput);
        }

        return inputOutputList.ToArray();
    }
}