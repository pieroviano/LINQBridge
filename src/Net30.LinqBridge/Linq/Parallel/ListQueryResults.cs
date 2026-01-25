#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class ListQueryResults<T> : QueryResults<T>
{
    private readonly int m_partitionCount;
    private readonly IList<T> m_source;
    private readonly bool m_useStriping;

    internal ListQueryResults(IList<T> source, int partitionCount, bool useStriping)
    {
        m_source = source;
        m_partitionCount = partitionCount;
        m_useStriping = useStriping;
    }

    internal override bool IsIndexible => true;

    internal override int ElementsCount => m_source.Count;

    internal override T GetElement(int index)
    {
        return m_source[index];
    }

    internal PartitionedStream<T, int> GetPartitionedStream()
    {
        return ExchangeUtilities.PartitionDataSource(m_source, m_partitionCount, m_useStriping);
    }

    internal override void GivePartitionedStream(IPartitionedStreamRecipient<T> recipient)
    {
        var partitionedStream = GetPartitionedStream();
        recipient.Receive(partitionedStream);
    }
}