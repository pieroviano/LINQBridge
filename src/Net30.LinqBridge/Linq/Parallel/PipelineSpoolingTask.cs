#nullable disable
namespace System.Linq.Parallel;

internal class PipelineSpoolingTask<TInputOutput, TIgnoreKey> : SpoolingTaskBase
{
    private readonly AsynchronousChannel<TInputOutput> m_destination;
    private readonly QueryOperatorEnumerator<TInputOutput, TIgnoreKey> m_source;

    internal PipelineSpoolingTask(
        int taskIndex,
        QueryTaskGroupState groupState,
        QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source,
        AsynchronousChannel<TInputOutput> destination)
        : base(taskIndex, groupState)
    {
        m_source = source;
        m_destination = destination;
    }

    protected override void SpoolingFinally()
    {
        base.SpoolingFinally();
        if (m_destination != null)
        {
            m_destination.SetDone();
        }

        m_source.Dispose();
    }

    protected override void SpoolingWork()
    {
        var currentElement = default(TInputOutput);
        var currentKey = default(TIgnoreKey);
        var source = m_source;
        var destination = m_destination;
        var cancellationToken = m_groupState.CancellationState.MergedCancellationToken;
        while (source.MoveNext(ref currentElement, ref currentKey) && !cancellationToken.IsCancellationRequested)
        {
            destination.Enqueue(currentElement);
        }

        destination.FlushBuffers();
    }
}