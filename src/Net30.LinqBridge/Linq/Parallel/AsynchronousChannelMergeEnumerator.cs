#nullable disable
using System.Linq.Expressions;

namespace System.Linq.Parallel;

internal sealed class AsynchronousChannelMergeEnumerator<T> : MergeEnumerator<T>
{
    private readonly AsynchronousChannel<T>[] m_channels;
    private readonly bool[] m_done;
    private int m_channelIndex;
    private IntValueEvent m_consumerEvent;
    private T m_currentElement;

    internal AsynchronousChannelMergeEnumerator(
        QueryTaskGroupState taskGroupState,
        AsynchronousChannel<T>[] channels,
        IntValueEvent consumerEvent)
        : base(taskGroupState)
    {
        m_channels = channels;
        m_channelIndex = -1;
        m_done = new bool[m_channels.Length];
        m_consumerEvent = consumerEvent;
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

    public override void Dispose()
    {
        if (m_consumerEvent == null)
        {
            return;
        }

        base.Dispose();
        m_consumerEvent.Dispose();
        m_consumerEvent = null;
    }

    public override bool MoveNext()
    {
        var index = m_channelIndex;
        if (index == -1)
        {
            m_channelIndex = index = 0;
        }

        if (index == m_channels.Length)
        {
            return false;
        }

        if (m_done[index] || !m_channels[index].TryDequeue(ref m_currentElement))
        {
            return MoveNextSlowPath();
        }

        m_channelIndex = (index + 1) % m_channels.Length;
        return true;
    }

    private bool MoveNextSlowPath()
    {
        var num1 = 0;
        var num2 = m_channelIndex;
        int channelIndex;
        while ((channelIndex = m_channelIndex) != m_channels.Length)
        {
            var channel = m_channels[channelIndex];
            var flag = m_done[channelIndex];
            if (!flag && channel.TryDequeue(ref m_currentElement))
            {
                m_channelIndex = (channelIndex + 1) % m_channels.Length;
                return true;
            }

            if (!flag && channel.IsDone)
            {
                if (!channel.IsChunkBufferEmpty)
                {
                    channel.TryDequeue(ref m_currentElement);
                    return true;
                }

                m_done[channelIndex] = true;
                flag = true;
                channel.Dispose();
            }

            if (flag && ++num1 == m_channels.Length)
            {
                int length;
                m_channelIndex = length = m_channels.Length;
                break;
            }

            int num3;
            m_channelIndex = num3 = (channelIndex + 1) % m_channels.Length;
            if (num3 == num2)
            {
                try
                {
                    num1 = 0;
                    for (var index = 0; index < m_channels.Length; ++index)
                    {
                        var isDone = false;
                        if (!m_done[index] && m_channels[index].TryDequeue(ref m_currentElement, ref isDone))
                        {
                            return true;
                        }

                        if (isDone)
                        {
                            if (!m_done[index])
                            {
                                m_done[index] = true;
                            }

                            if (++num1 == m_channels.Length)
                            {
                                m_channelIndex = num3 = m_channels.Length;
                                break;
                            }
                        }
                    }

                    if (num3 != m_channels.Length)
                    {
                        m_consumerEvent.Wait();
                        int num4;
                        m_channelIndex = num4 = m_consumerEvent.Value;
                        m_consumerEvent.Reset();
                        num2 = num4;
                        num1 = 0;
                    }
                    else
                    {
                        break;
                    }
                }
                finally
                {
                    for (var index = 0; index < m_channels.Length; ++index)
                    {
                        if (!m_done[index])
                        {
                            m_channels[index].DoneWithDequeueWait();
                        }
                    }
                }
            }
        }

        m_taskGroupState.QueryEnd(false);
        return false;
    }
}