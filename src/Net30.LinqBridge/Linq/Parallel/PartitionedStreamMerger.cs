#nullable disable
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal class PartitionedStreamMerger<TOutput> : IPartitionedStreamRecipient<TOutput>
{
    private readonly CancellationState m_cancellationState;
    private readonly bool m_forEffectMerge;
    private readonly bool m_isOrdered;
    private readonly ParallelMergeOptions m_mergeOptions;
    private readonly int m_queryId;
    private readonly TaskScheduler m_taskScheduler;

    internal PartitionedStreamMerger(
        bool forEffectMerge,
        ParallelMergeOptions mergeOptions,
        TaskScheduler taskScheduler,
        bool outputOrdered,
        CancellationState cancellationState,
        int queryId)
    {
        m_forEffectMerge = forEffectMerge;
        m_mergeOptions = mergeOptions;
        m_isOrdered = outputOrdered;
        m_taskScheduler = taskScheduler;
        m_cancellationState = cancellationState;
        m_queryId = queryId;
    }

    internal MergeExecutor<TOutput> MergeExecutor { get; private set; }

    public void Receive<TKey>(PartitionedStream<TOutput, TKey> partitionedStream)
    {
        MergeExecutor = MergeExecutor<TOutput>.Execute(partitionedStream, m_forEffectMerge, m_mergeOptions,
            m_taskScheduler, m_isOrdered, m_cancellationState, m_queryId);
    }
}