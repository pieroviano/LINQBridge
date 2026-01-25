#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal class PartitionedDataSource<T> : PartitionedStream<T, int>
{
    internal PartitionedDataSource(IEnumerable<T> source, int partitionCount, bool useStriping)
        : base(partitionCount, Util.GetDefaultComparer<int>(),
            source is IList<T> ? OrdinalIndexState.Indexible : OrdinalIndexState.Correct)
    {
        InitializePartitions(source, partitionCount, useStriping);
    }

    private void InitializePartitions(IEnumerable<T> source, int partitionCount, bool useStriping)
    {
        if (source is ParallelEnumerableWrapper<T> enumerableWrapper)
        {
            source = enumerableWrapper.WrappedEnumerable;
        }

        if (source is IList<T> data1)
        {
            var operatorEnumeratorArray = new QueryOperatorEnumerator<T, int>[partitionCount];
            var count = data1.Count;
            var data = source as T[];
            var maxChunkSize = -1;
            if (useStriping)
            {
                maxChunkSize = Scheduling.GetDefaultChunkSize<T>();
                if (maxChunkSize < 1)
                {
                    maxChunkSize = 1;
                }
            }

            for (var partitionIndex = 0; partitionIndex < partitionCount; ++partitionIndex)
            {
                operatorEnumeratorArray[partitionIndex] = data == null
                    ?
                    !useStriping
                        ? new ListContiguousIndexRangeEnumerator(data1, partitionCount, partitionIndex)
                        : new ListIndexRangeEnumerator(data1, partitionCount, partitionIndex, maxChunkSize)
                    : !useStriping
                        ? new ArrayContiguousIndexRangeEnumerator(data, partitionCount, partitionIndex)
                        : new ArrayIndexRangeEnumerator(data, partitionCount, partitionIndex, maxChunkSize);
            }

            m_partitions = operatorEnumeratorArray;
        }
        else
        {
            m_partitions = MakePartitions(source.GetEnumerator(), partitionCount);
        }
    }

    private static QueryOperatorEnumerator<T, int>[] MakePartitions(
        IEnumerator<T> source,
        int partitionCount)
    {
        var operatorEnumeratorArray = new QueryOperatorEnumerator<T, int>[partitionCount];
        var sourceSyncLock = new object();
        var currentIndex = new Shared<int>(0);
        var degreeOfParallelism = new Shared<int>(partitionCount);
        var exceptionTracker = new Shared<bool>(false);
        for (var index = 0; index < partitionCount; ++index)
        {
            operatorEnumeratorArray[index] = new ContiguousChunkLazyEnumerator(source, exceptionTracker, sourceSyncLock,
                currentIndex, degreeOfParallelism);
        }

        return operatorEnumeratorArray;
    }

    internal sealed class ArrayIndexRangeEnumerator : QueryOperatorEnumerator<T, int>
    {
        private readonly T[] m_data;
        private readonly int m_elementCount;
        private readonly int m_maxChunkSize;
        private readonly int m_partitionCount;
        private readonly int m_partitionIndex;
        private readonly int m_sectionCount;
        private Mutables m_mutables;

        internal ArrayIndexRangeEnumerator(
            T[] data,
            int partitionCount,
            int partitionIndex,
            int maxChunkSize)
        {
            m_data = data;
            m_elementCount = data.Length;
            m_partitionCount = partitionCount;
            m_partitionIndex = partitionIndex;
            m_maxChunkSize = maxChunkSize;
            var num = maxChunkSize * partitionCount;
            m_sectionCount = m_elementCount / num + (m_elementCount % num == 0 ? 0 : 1);
        }

        internal override bool MoveNext(ref T currentElement, ref int currentKey)
        {
            var mutables = m_mutables ?? (m_mutables = new Mutables());
            if (++mutables.m_currentPositionInChunk >= mutables.m_currentChunkSize && !MoveNextSlowPath())
            {
                return false;
            }

            currentKey = mutables.m_currentChunkOffset + mutables.m_currentPositionInChunk;
            currentElement = m_data[currentKey];
            return true;
        }

        private bool MoveNextSlowPath()
        {
            var mutables = m_mutables;
            var num1 = ++mutables.m_currentSection;
            var num2 = m_sectionCount - num1;
            if (num2 <= 0)
            {
                return false;
            }

            var num3 = num1 * m_partitionCount * m_maxChunkSize;
            mutables.m_currentPositionInChunk = 0;
            if (num2 > 1)
            {
                mutables.m_currentChunkSize = m_maxChunkSize;
                mutables.m_currentChunkOffset = num3 + m_partitionIndex * m_maxChunkSize;
            }
            else
            {
                var num4 = m_elementCount - num3;
                var num5 = num4 / m_partitionCount;
                var num6 = num4 % m_partitionCount;
                mutables.m_currentChunkSize = num5;
                if (m_partitionIndex < num6)
                {
                    ++mutables.m_currentChunkSize;
                }

                if (mutables.m_currentChunkSize == 0)
                {
                    return false;
                }

                mutables.m_currentChunkOffset = num3 + m_partitionIndex * num5 +
                                                (m_partitionIndex < num6 ? m_partitionIndex : num6);
            }

            return true;
        }

        private class Mutables
        {
            internal int m_currentChunkOffset;
            internal int m_currentChunkSize;
            internal int m_currentPositionInChunk;
            internal int m_currentSection;

            internal Mutables()
            {
                m_currentSection = -1;
            }
        }
    }

    internal sealed class ArrayContiguousIndexRangeEnumerator : QueryOperatorEnumerator<T, int>
    {
        private readonly T[] m_data;
        private readonly int m_maximumIndex;
        private readonly int m_startIndex;
        private Shared<int> m_currentIndex;

        internal ArrayContiguousIndexRangeEnumerator(T[] data, int partitionCount, int partitionIndex)
        {
            m_data = data;
            var num1 = data.Length / partitionCount;
            var num2 = data.Length % partitionCount;
            var num3 = partitionIndex * num1 + (partitionIndex < num2 ? partitionIndex : num2);
            m_startIndex = num3 - 1;
            m_maximumIndex = num3 + num1 + (partitionIndex < num2 ? 1 : 0);
        }

        internal override bool MoveNext(ref T currentElement, ref int currentKey)
        {
            if (m_currentIndex == null)
            {
                m_currentIndex = new Shared<int>(m_startIndex);
            }

            var index = ++m_currentIndex.Value;
            if (index >= m_maximumIndex)
            {
                return false;
            }

            currentKey = index;
            currentElement = m_data[index];
            return true;
        }
    }

    internal sealed class ListIndexRangeEnumerator : QueryOperatorEnumerator<T, int>
    {
        private readonly IList<T> m_data;
        private readonly int m_elementCount;
        private readonly int m_maxChunkSize;
        private readonly int m_partitionCount;
        private readonly int m_partitionIndex;
        private readonly int m_sectionCount;
        private Mutables m_mutables;

        internal ListIndexRangeEnumerator(
            IList<T> data,
            int partitionCount,
            int partitionIndex,
            int maxChunkSize)
        {
            m_data = data;
            m_elementCount = data.Count;
            m_partitionCount = partitionCount;
            m_partitionIndex = partitionIndex;
            m_maxChunkSize = maxChunkSize;
            var num = maxChunkSize * partitionCount;
            m_sectionCount = m_elementCount / num + (m_elementCount % num == 0 ? 0 : 1);
        }

        internal override bool MoveNext(ref T currentElement, ref int currentKey)
        {
            var mutables = m_mutables ?? (m_mutables = new Mutables());
            if (++mutables.m_currentPositionInChunk >= mutables.m_currentChunkSize && !MoveNextSlowPath())
            {
                return false;
            }

            currentKey = mutables.m_currentChunkOffset + mutables.m_currentPositionInChunk;
            currentElement = m_data[currentKey];
            return true;
        }

        private bool MoveNextSlowPath()
        {
            var mutables = m_mutables;
            var num1 = ++mutables.m_currentSection;
            var num2 = m_sectionCount - num1;
            if (num2 <= 0)
            {
                return false;
            }

            var num3 = num1 * m_partitionCount * m_maxChunkSize;
            mutables.m_currentPositionInChunk = 0;
            if (num2 > 1)
            {
                mutables.m_currentChunkSize = m_maxChunkSize;
                mutables.m_currentChunkOffset = num3 + m_partitionIndex * m_maxChunkSize;
            }
            else
            {
                var num4 = m_elementCount - num3;
                var num5 = num4 / m_partitionCount;
                var num6 = num4 % m_partitionCount;
                mutables.m_currentChunkSize = num5;
                if (m_partitionIndex < num6)
                {
                    ++mutables.m_currentChunkSize;
                }

                if (mutables.m_currentChunkSize == 0)
                {
                    return false;
                }

                mutables.m_currentChunkOffset = num3 + m_partitionIndex * num5 +
                                                (m_partitionIndex < num6 ? m_partitionIndex : num6);
            }

            return true;
        }

        private class Mutables
        {
            internal int m_currentChunkOffset;
            internal int m_currentChunkSize;
            internal int m_currentPositionInChunk;
            internal int m_currentSection;

            internal Mutables()
            {
                m_currentSection = -1;
            }
        }
    }

    internal sealed class ListContiguousIndexRangeEnumerator : QueryOperatorEnumerator<T, int>
    {
        private readonly IList<T> m_data;
        private readonly int m_maximumIndex;
        private readonly int m_startIndex;
        private Shared<int> m_currentIndex;

        internal ListContiguousIndexRangeEnumerator(
            IList<T> data,
            int partitionCount,
            int partitionIndex)
        {
            m_data = data;
            var num1 = data.Count / partitionCount;
            var num2 = data.Count % partitionCount;
            var num3 = partitionIndex * num1 + (partitionIndex < num2 ? partitionIndex : num2);
            m_startIndex = num3 - 1;
            m_maximumIndex = num3 + num1 + (partitionIndex < num2 ? 1 : 0);
        }

        internal override bool MoveNext(ref T currentElement, ref int currentKey)
        {
            if (m_currentIndex == null)
            {
                m_currentIndex = new Shared<int>(m_startIndex);
            }

            var index = ++m_currentIndex.Value;
            if (index >= m_maximumIndex)
            {
                return false;
            }

            currentKey = index;
            currentElement = m_data[index];
            return true;
        }
    }

    private class ContiguousChunkLazyEnumerator : QueryOperatorEnumerator<T, int>
    {
        private const int chunksPerChunkSize = 7;
        private readonly Shared<int> m_activeEnumeratorsCount;
        private readonly Shared<int> m_currentIndex;
        private readonly Shared<bool> m_exceptionTracker;
        private readonly IEnumerator<T> m_source;
        private readonly object m_sourceSyncLock;
        private Mutables m_mutables;

        internal ContiguousChunkLazyEnumerator(
            IEnumerator<T> source,
            Shared<bool> exceptionTracker,
            object sourceSyncLock,
            Shared<int> currentIndex,
            Shared<int> degreeOfParallelism)
        {
            m_source = source;
            m_sourceSyncLock = sourceSyncLock;
            m_currentIndex = currentIndex;
            m_activeEnumeratorsCount = degreeOfParallelism;
            m_exceptionTracker = exceptionTracker;
        }

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.Decrement(ref m_activeEnumeratorsCount.Value) != 0)
            {
                return;
            }

            m_source.Dispose();
        }

        internal override bool MoveNext(ref T currentElement, ref int currentKey)
        {
            var mutables = m_mutables ?? (m_mutables = new Mutables());
            T[] chunkBuffer;
            int index1;
            while (true)
            {
                chunkBuffer = mutables.m_chunkBuffer;
                index1 = ++mutables.m_currentChunkIndex;
                if (index1 >= mutables.m_currentChunkSize)
                {
                    lock (m_sourceSyncLock)
                    {
                        var index2 = 0;
                        if (m_exceptionTracker.Value)
                        {
                            return false;
                        }

                        try
                        {
                            for (; index2 < mutables.m_nextChunkMaxSize; ++index2)
                            {
                                if (m_source.MoveNext())
                                {
                                    chunkBuffer[index2] = m_source.Current;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            m_exceptionTracker.Value = true;
                            throw;
                        }

                        mutables.m_currentChunkSize = index2;
                        if (index2 == 0)
                        {
                            return false;
                        }

                        mutables.m_chunkBaseIndex = m_currentIndex.Value;
                        checked
                        {
                            m_currentIndex.Value += index2;
                        }
                    }

                    if (mutables.m_nextChunkMaxSize < chunkBuffer.Length && (mutables.m_chunkCounter++ & 7) == 7)
                    {
                        mutables.m_nextChunkMaxSize *= 2;
                        if (mutables.m_nextChunkMaxSize > chunkBuffer.Length)
                        {
                            mutables.m_nextChunkMaxSize = chunkBuffer.Length;
                        }
                    }

                    mutables.m_currentChunkIndex = -1;
                }
                else
                {
                    break;
                }
            }

            currentElement = chunkBuffer[index1];
            currentKey = mutables.m_chunkBaseIndex + index1;
            return true;
        }

        private class Mutables
        {
            internal readonly T[] m_chunkBuffer;
            internal int m_chunkBaseIndex;
            internal int m_chunkCounter;
            internal int m_currentChunkIndex;
            internal int m_currentChunkSize;
            internal int m_nextChunkMaxSize;

            internal Mutables()
            {
                m_nextChunkMaxSize = 1;
                m_chunkBuffer = new T[Scheduling.GetDefaultChunkSize<T>()];
                m_currentChunkSize = 0;
                m_currentChunkIndex = -1;
                m_chunkBaseIndex = 0;
                m_chunkCounter = 0;
            }
        }
    }
}