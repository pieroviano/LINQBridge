#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class RangeEnumerable : ParallelQuery<int>, IParallelPartitionable<int>
{
    private readonly int m_count;
    private readonly int m_from;

    internal RangeEnumerable(int from, int count)
        : base(QuerySettings.Empty)
    {
        m_from = from;
        m_count = count;
    }

    public QueryOperatorEnumerator<int, int>[] GetPartitions(int partitionCount)
    {
        var num1 = m_count / partitionCount;
        var num2 = m_count % partitionCount;
        var initialIndex = 0;
        var partitions = new QueryOperatorEnumerator<int, int>[partitionCount];
        for (var index = 0; index < partitionCount; ++index)
        {
            var count = index < num2 ? num1 + 1 : num1;
            partitions[index] = new RangeEnumerator(m_from + initialIndex, count, initialIndex);
            initialIndex += count;
        }

        return partitions;
    }

    public override IEnumerator<int> GetEnumerator()
    {
        return new RangeEnumerator(m_from, m_count, 0).AsClassicEnumerator();
    }

    private class RangeEnumerator : QueryOperatorEnumerator<int, int>
    {
        private readonly int m_count;
        private readonly int m_from;
        private readonly int m_initialIndex;
        private Shared<int> m_currentCount;

        internal RangeEnumerator(int from, int count, int initialIndex)
        {
            m_from = from;
            m_count = count;
            m_initialIndex = initialIndex;
        }

        internal override bool MoveNext(ref int currentElement, ref int currentKey)
        {
            if (m_currentCount == null)
            {
                m_currentCount = new Shared<int>(-1);
            }

            var num = m_currentCount.Value + 1;
            if (num >= m_count)
            {
                return false;
            }

            m_currentCount.Value = num;
            currentElement = num + m_from;
            currentKey = num + m_initialIndex;
            return true;
        }

        internal override void Reset()
        {
            m_currentCount = null;
        }
    }
}