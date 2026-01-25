#nullable disable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal class OrderPreservingPipeliningMergeHelper<TOutput, TKey> : IMergeHelper<TOutput>
{
    internal const int INITIAL_BUFFER_SIZE = 128 /*0x80*/;
    internal const int STEAL_BUFFER_SIZE = 1024 /*0x0400*/;
    internal const int MAX_BUFFER_SIZE = 8192 /*0x2000*/;
    private readonly bool m_autoBuffered;
    private readonly object[] m_bufferLocks;
    private readonly Queue<Pair<TKey, TOutput>>[] m_buffers;
    private readonly bool[] m_consumerWaiting;
    private readonly PartitionedStream<TOutput, TKey> m_partitions;
    private readonly IComparer<Producer<TKey>> m_producerComparer;
    private readonly bool[] m_producerDone;
    private readonly bool[] m_producerWaiting;
    private readonly QueryTaskGroupState m_taskGroupState;
    private readonly TaskScheduler m_taskScheduler;

    internal OrderPreservingPipeliningMergeHelper(
        PartitionedStream<TOutput, TKey> partitions,
        TaskScheduler taskScheduler,
        CancellationState cancellationState,
        bool autoBuffered,
        int queryId,
        IComparer<TKey> keyComparer)
    {
        m_taskGroupState = new QueryTaskGroupState(cancellationState, queryId);
        m_partitions = partitions;
        m_taskScheduler = taskScheduler;
        m_autoBuffered = autoBuffered;
        var partitionCount = m_partitions.PartitionCount;
        m_buffers = new Queue<Pair<TKey, TOutput>>[partitionCount];
        m_producerDone = new bool[partitionCount];
        m_consumerWaiting = new bool[partitionCount];
        m_producerWaiting = new bool[partitionCount];
        m_bufferLocks = new object[partitionCount];
        if (keyComparer == Util.GetDefaultComparer<int>())
        {
            m_producerComparer = (IComparer<Producer<TKey>>)new ProducerComparerInt();
        }
        else
        {
            m_producerComparer = new ProducerComparer(keyComparer);
        }
    }

    void IMergeHelper<TOutput>.Execute()
    {
        OrderPreservingPipeliningSpoolingTask<TOutput, TKey>.Spool(m_taskGroupState, m_partitions, m_consumerWaiting,
            m_producerWaiting, m_producerDone, m_buffers, m_bufferLocks, m_taskScheduler, m_autoBuffered);
    }

    IEnumerator<TOutput> IMergeHelper<TOutput>.GetEnumerator()
    {
        return new OrderedPipeliningMergeEnumerator(this, m_producerComparer);
    }

    public TOutput[] GetResultsAsArray()
    {
        throw new InvalidOperationException();
    }

    private class ProducerComparer : IComparer<Producer<TKey>>
    {
        private readonly IComparer<TKey> _keyComparer;

        internal ProducerComparer(IComparer<TKey> keyComparer)
        {
            _keyComparer = keyComparer;
        }

        public int Compare(Producer<TKey> x, Producer<TKey> y)
        {
            return _keyComparer.Compare(y.MaxKey, x.MaxKey);
        }
    }

    private class OrderedPipeliningMergeEnumerator : MergeEnumerator<TOutput>
    {
        private readonly OrderPreservingPipeliningMergeHelper<TOutput, TKey> m_mergeHelper;
        private readonly Queue<Pair<TKey, TOutput>>[] m_privateBuffer;
        private readonly FixedMaxHeap<Producer<TKey>> m_producerHeap;
        private readonly TOutput[] m_producerNextElement;
        private bool m_initialized;

        internal OrderedPipeliningMergeEnumerator(
            OrderPreservingPipeliningMergeHelper<TOutput, TKey> mergeHelper,
            IComparer<Producer<TKey>> producerComparer)
            : base(mergeHelper.m_taskGroupState)
        {
            var partitionCount = mergeHelper.m_partitions.PartitionCount;
            m_mergeHelper = mergeHelper;
            m_producerHeap = new FixedMaxHeap<Producer<TKey>>(partitionCount, producerComparer);
            m_privateBuffer = new Queue<Pair<TKey, TOutput>>[partitionCount];
            m_producerNextElement = new TOutput[partitionCount];
        }

        public override TOutput Current => m_producerNextElement[m_producerHeap.MaxValue.ProducerIndex];

        public override void Dispose()
        {
            var length = m_mergeHelper.m_buffers.Length;
            for (var index = 0; index < length; ++index)
            {
                var bufferLock = m_mergeHelper.m_bufferLocks[index];
                lock (bufferLock)
                {
                    if (m_mergeHelper.m_producerWaiting[index])
                    {
                        Monitor.Pulse(bufferLock);
                    }
                }
            }

            base.Dispose();
        }

        public override bool MoveNext()
        {
            if (!m_initialized)
            {
                m_initialized = true;
                for (var index = 0; index < m_mergeHelper.m_partitions.PartitionCount; ++index)
                {
                    var element = new Pair<TKey, TOutput>();
                    if (TryWaitForElement(index, ref element))
                    {
                        m_producerHeap.Insert(new Producer<TKey>(element.First, index));
                        m_producerNextElement[index] = element.Second;
                    }
                    else
                    {
                        ThrowIfInTearDown();
                    }
                }
            }
            else
            {
                if (m_producerHeap.Count == 0)
                {
                    return false;
                }

                var producerIndex = m_producerHeap.MaxValue.ProducerIndex;
                var element = new Pair<TKey, TOutput>();
                if (TryGetPrivateElement(producerIndex, ref element) || TryWaitForElement(producerIndex, ref element))
                {
                    m_producerHeap.ReplaceMax(new Producer<TKey>(element.First, producerIndex));
                    m_producerNextElement[producerIndex] = element.Second;
                }
                else
                {
                    ThrowIfInTearDown();
                    m_producerHeap.RemoveMax();
                }
            }

            return m_producerHeap.Count > 0;
        }

        private void ThrowIfInTearDown()
        {
            if (!m_mergeHelper.m_taskGroupState.CancellationState.MergedCancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var bufferLocks = m_mergeHelper.m_bufferLocks;
                for (var index = 0; index < bufferLocks.Length; ++index)
                {
                    lock (bufferLocks[index])
                    {
                        Monitor.Pulse(bufferLocks[index]);
                    }
                }

                m_taskGroupState.QueryEnd(false);
            }
            finally
            {
                m_producerHeap.Clear();
            }
        }

        private bool TryGetPrivateElement(int producer, ref Pair<TKey, TOutput> element)
        {
            var pairQueue = m_privateBuffer[producer];
            if (pairQueue != null)
            {
                if (pairQueue.Count > 0)
                {
                    element = pairQueue.Dequeue();
                    return true;
                }

                m_privateBuffer[producer] = null;
            }

            return false;
        }

        private bool TryWaitForElement(int producer, ref Pair<TKey, TOutput> element)
        {
            var buffer = m_mergeHelper.m_buffers[producer];
            var bufferLock = m_mergeHelper.m_bufferLocks[producer];
            lock (bufferLock)
            {
                if (buffer.Count == 0)
                {
                    if (m_mergeHelper.m_producerDone[producer])
                    {
                        element = new Pair<TKey, TOutput>();
                        return false;
                    }

                    m_mergeHelper.m_consumerWaiting[producer] = true;
                    Monitor.Wait(bufferLock);
                    if (buffer.Count == 0)
                    {
                        element = new Pair<TKey, TOutput>();
                        return false;
                    }
                }

                if (m_mergeHelper.m_producerWaiting[producer])
                {
                    Monitor.Pulse(bufferLock);
                    m_mergeHelper.m_producerWaiting[producer] = false;
                }

                if (buffer.Count < 1024 /*0x0400*/)
                {
                    element = buffer.Dequeue();
                    return true;
                }

                m_privateBuffer[producer] = m_mergeHelper.m_buffers[producer];
                m_mergeHelper.m_buffers[producer] = new Queue<Pair<TKey, TOutput>>(128 /*0x80*/);
            }

            TryGetPrivateElement(producer, ref element);
            return true;
        }
    }
}