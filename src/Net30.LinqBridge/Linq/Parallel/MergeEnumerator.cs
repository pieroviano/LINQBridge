#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal abstract class MergeEnumerator<TInputOutput> :
    IEnumerator<TInputOutput>,
    IDisposable,
    IEnumerator
{
    protected QueryTaskGroupState m_taskGroupState;

    protected MergeEnumerator(QueryTaskGroupState taskGroupState)
    {
        m_taskGroupState = taskGroupState;
    }

    public abstract TInputOutput Current { get; }

    public abstract bool MoveNext();

    object IEnumerator.Current => Current;

    public virtual void Reset()
    {
    }

    public virtual void Dispose()
    {
        if (m_taskGroupState.IsAlreadyEnded)
        {
            return;
        }

        m_taskGroupState.QueryEnd(true);
    }
}