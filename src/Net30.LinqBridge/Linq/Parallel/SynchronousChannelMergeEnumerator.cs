#nullable disable
using System.Linq.Expressions;

namespace System.Linq.Parallel;

internal sealed class SynchronousChannelMergeEnumerator<T> : MergeEnumerator<T>
{
    private readonly SynchronousChannel<T>[] m_channels;
    private int m_channelIndex;
    private T m_currentElement;

    internal SynchronousChannelMergeEnumerator(
        QueryTaskGroupState taskGroupState,
        SynchronousChannel<T>[] channels)
        : base(taskGroupState)
    {
        m_channels = channels;
        m_channelIndex = -1;
    }

    public override T Current
    {
        get
        {
            if (m_channelIndex == -1 || m_channelIndex == m_channels.Length)
            {
                throw new InvalidOperationException(Strings.PLINQ_CommonEnumerator_Current_NotStarted());
            }

            return m_currentElement;
        }
    }

    public override bool MoveNext()
    {
        if (m_channelIndex == -1)
        {
            m_channelIndex = 0;
        }

        for (; m_channelIndex != m_channels.Length; ++m_channelIndex)
        {
            var channel = m_channels[m_channelIndex];
            if (channel.Count == 0)
            {
                continue;
            }

            m_currentElement = channel.Dequeue();
            return true;
        }

        return false;
    }
}