#nullable disable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal class OrderPreservingPipeliningSpoolingTask<TOutput, TKey> : SpoolingTaskBase
{
    private const int PRODUCER_BUFFER_AUTO_SIZE = 16 /*0x10*/;
    private readonly bool m_autoBuffered;
    private readonly object m_bufferLock;
    private readonly Queue<Pair<TKey, TOutput>>[] m_buffers;
    private readonly bool[] m_consumerWaiting;
    private readonly QueryOperatorEnumerator<TOutput, TKey> m_partition;
    private readonly int m_partitionIndex;
    private readonly bool[] m_producerDone;
    private readonly bool[] m_producerWaiting;
    private readonly QueryTaskGroupState m_taskGroupState;
    private readonly TaskScheduler m_taskScheduler;

    internal OrderPreservingPipeliningSpoolingTask(
        QueryOperatorEnumerator<TOutput, TKey> partition,
        QueryTaskGroupState taskGroupState,
        bool[] consumerWaiting,
        bool[] producerWaiting,
        bool[] producerDone,
        int partitionIndex,
        Queue<Pair<TKey, TOutput>>[] buffers,
        object bufferLock,
        TaskScheduler taskScheduler,
        bool autoBuffered)
        : base(partitionIndex, taskGroupState)
    {
        m_partition = partition;
        m_taskGroupState = taskGroupState;
        m_producerDone = producerDone;
        m_consumerWaiting = consumerWaiting;
        m_producerWaiting = producerWaiting;
        m_partitionIndex = partitionIndex;
        m_buffers = buffers;
        m_bufferLock = bufferLock;
        m_taskScheduler = taskScheduler;
        m_autoBuffered = autoBuffered;
    }

    public static void Spool(
        QueryTaskGroupState groupState,
        PartitionedStream<TOutput, TKey> partitions,
        bool[] consumerWaiting,
        bool[] producerWaiting,
        bool[] producerDone,
        Queue<Pair<TKey, TOutput>>[] buffers,
        object[] bufferLocks,
        TaskScheduler taskScheduler,
        bool autoBuffered)
    {
        var degreeOfParallelism = partitions.PartitionCount;
        for (var index = 0; index < degreeOfParallelism; ++index)
        {
            buffers[index] = new Queue<Pair<TKey, TOutput>>(128 /*0x80*/);
            bufferLocks[index] = new object();
        }

        var rootTask = new Task((Action)(() =>
        {
            for (var index = 0; index < degreeOfParallelism; ++index)
            {
                new OrderPreservingPipeliningSpoolingTask<TOutput, TKey>(partitions[index], groupState, consumerWaiting,
                        producerWaiting, producerDone, index, buffers, bufferLocks[index], taskScheduler, autoBuffered)
                    .RunAsynchronously(taskScheduler);
            }
        }));
        groupState.QueryBegin(rootTask);
        rootTask.Start(taskScheduler);
    }

    protected override void SpoolingFinally()
    {
        lock (m_bufferLock)
        {
            m_producerDone[m_partitionIndex] = true;
            if (m_consumerWaiting[m_partitionIndex])
            {
                Monitor.Pulse(m_bufferLock);
                m_consumerWaiting[m_partitionIndex] = false;
            }
        }

        base.SpoolingFinally();
        m_partition.Dispose();
    }

    protected override void SpoolingWork()
    {
        var currentElement = default(TOutput);
        var currentKey = default(TKey);
        var length = m_autoBuffered ? 16 /*0x10*/ : 1;
        var pairArray = new Pair<TKey, TOutput>[length];
        var partition = m_partition;
        var cancellationToken = m_taskGroupState.CancellationState.MergedCancellationToken;
        int index1;
        do
        {
            for (index1 = 0; index1 < length && partition.MoveNext(ref currentElement, ref currentKey); ++index1)
            {
                pairArray[index1] = new Pair<TKey, TOutput>(currentKey, currentElement);
            }

            if (index1 != 0)
            {
                lock (m_bufferLock)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    for (var index2 = 0; index2 < index1; ++index2)
                    {
                        m_buffers[m_partitionIndex].Enqueue(pairArray[index2]);
                    }

                    if (m_consumerWaiting[m_partitionIndex])
                    {
                        Monitor.Pulse(m_bufferLock);
                        m_consumerWaiting[m_partitionIndex] = false;
                    }

                    if (m_buffers[m_partitionIndex].Count >= 8192 /*0x2000*/)
                    {
                        m_producerWaiting[m_partitionIndex] = true;
                        Monitor.Wait(m_bufferLock);
                    }
                }
            }
            else
            {
                goto label_15;
            }
        } while (index1 == length);

        goto label_7;
        label_15:
        return;
        label_7: ;
    }
}