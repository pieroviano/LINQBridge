#nullable disable
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal class QueryTaskGroupState
{
    private int m_alreadyEnded;
    private Task m_rootTask;

    internal QueryTaskGroupState(CancellationState cancellationState, int queryId)
    {
        CancellationState = cancellationState;
        QueryId = queryId;
    }

    internal bool IsAlreadyEnded => m_alreadyEnded == 1;

    internal CancellationState CancellationState { get; }

    internal int QueryId { get; }

    internal void QueryBegin(Task rootTask)
    {
        m_rootTask = rootTask;
    }

    internal void QueryEnd(bool userInitiatedDispose)
    {
        if (Interlocked.Exchange(ref m_alreadyEnded, 1) != 0)
        {
            return;
        }

        try
        {
            m_rootTask.Wait();
        }
        catch (AggregateException ex)
        {
            var aggregateException = ex.Flatten();
            var flag = true;
            for (var index = 0; index < aggregateException.InnerExceptions.Count; ++index)
            {
                if (!(aggregateException.InnerExceptions[index] is OperationCanceledException innerException) ||
                    !innerException.CancellationToken.IsCancellationRequested || innerException.CancellationToken !=
                    CancellationState.ExternalCancellationToken)
                {
                    flag = false;
                    break;
                }
            }

            if (!flag)
            {
                throw aggregateException;
            }
        }
        finally
        {
            m_rootTask.Dispose();
        }

        if (!CancellationState.MergedCancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (!CancellationState.TopLevelDisposedFlag.Value)
        {
            CancellationState.ThrowWithStandardMessageIfCanceled(CancellationState.ExternalCancellationToken);
        }

        if (!userInitiatedDispose)
        {
            throw new ObjectDisposedException("enumerator", Strings.PLINQ_DisposeRequested());
        }
    }
}