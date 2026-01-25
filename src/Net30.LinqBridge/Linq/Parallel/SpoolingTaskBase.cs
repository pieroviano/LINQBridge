#nullable disable
namespace System.Linq.Parallel;

internal abstract class SpoolingTaskBase(int taskIndex, QueryTaskGroupState groupState)
    : QueryTask(taskIndex, groupState)
{
    protected virtual void SpoolingFinally()
    {
    }

    protected abstract void SpoolingWork();

    protected override void Work()
    {
        try
        {
            SpoolingWork();
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException canceledException &&
                canceledException.CancellationToken == m_groupState.CancellationState.MergedCancellationToken &&
                m_groupState.CancellationState.MergedCancellationToken.IsCancellationRequested)
            {
                return;
            }

            m_groupState.CancellationState.InternalCancellationTokenSource.Cancel();
            throw;
        }
        finally
        {
            SpoolingFinally();
        }
    }
}