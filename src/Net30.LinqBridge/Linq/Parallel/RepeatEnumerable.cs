#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class RepeatEnumerable<TResult> : ParallelQuery<TResult>, IParallelPartitionable<TResult>
{
    private readonly int m_count;
    private readonly TResult m_element;

    internal RepeatEnumerable(TResult element, int count)
        : base(QuerySettings.Empty)
    {
        m_element = element;
        m_count = count;
    }

    public QueryOperatorEnumerator<TResult, int>[] GetPartitions(int partitionCount)
    {
        var count = (m_count + partitionCount - 1) / partitionCount;
        var partitions = new QueryOperatorEnumerator<TResult, int>[partitionCount];
        var index = 0;
        var indexOffset = 0;
        while (index < partitionCount)
        {
            partitions[index] = indexOffset + count <= m_count
                ? new RepeatEnumerator(m_element, count, indexOffset)
                : (QueryOperatorEnumerator<TResult, int>)new RepeatEnumerator(m_element,
                    indexOffset < m_count ? m_count - indexOffset : 0, indexOffset);
            ++index;
            indexOffset += count;
        }

        return partitions;
    }

    public override IEnumerator<TResult> GetEnumerator()
    {
        return new RepeatEnumerator(m_element, m_count, 0).AsClassicEnumerator();
    }

    private class RepeatEnumerator : QueryOperatorEnumerator<TResult, int>
    {
        private readonly int m_count;
        private readonly TResult m_element;
        private readonly int m_indexOffset;
        private Shared<int> m_currentIndex;

        internal RepeatEnumerator(TResult element, int count, int indexOffset)
        {
            m_element = element;
            m_count = count;
            m_indexOffset = indexOffset;
        }

        internal override bool MoveNext(ref TResult currentElement, ref int currentKey)
        {
            if (m_currentIndex == null)
            {
                m_currentIndex = new Shared<int>(-1);
            }

            if (m_currentIndex.Value >= m_count - 1)
            {
                return false;
            }

            ++m_currentIndex.Value;
            currentElement = m_element;
            currentKey = m_currentIndex.Value + m_indexOffset;
            return true;
        }

        internal override void Reset()
        {
            m_currentIndex = null;
        }
    }
}