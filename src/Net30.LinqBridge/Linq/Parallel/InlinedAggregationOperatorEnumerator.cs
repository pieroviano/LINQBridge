#nullable disable
using System.Threading;

namespace System.Linq.Parallel;

internal abstract class InlinedAggregationOperatorEnumerator<TIntermediate> :
    QueryOperatorEnumerator<TIntermediate, int>
{
    private readonly int m_partitionIndex;
    protected CancellationToken m_cancellationToken;
    private bool m_done;

    internal InlinedAggregationOperatorEnumerator(
        int partitionIndex,
        CancellationToken cancellationToken)
    {
        m_partitionIndex = partitionIndex;
        m_cancellationToken = cancellationToken;
    }

    protected abstract bool MoveNextCore(ref TIntermediate currentElement);

    internal sealed override bool MoveNext(ref TIntermediate currentElement, ref int currentKey)
    {
        if (m_done || !MoveNextCore(ref currentElement))
        {
            return false;
        }

        currentKey = m_partitionIndex;
        m_done = true;
        return true;
    }
}