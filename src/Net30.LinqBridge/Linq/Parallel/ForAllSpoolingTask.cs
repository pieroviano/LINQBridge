#nullable disable
namespace System.Linq.Parallel;

internal class ForAllSpoolingTask<TInputOutput, TIgnoreKey> : SpoolingTaskBase
{
    private readonly QueryOperatorEnumerator<TInputOutput, TIgnoreKey> m_source;

    internal ForAllSpoolingTask(
        int taskIndex,
        QueryTaskGroupState groupState,
        QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source)
        : base(taskIndex, groupState)
    {
        m_source = source;
    }

    protected override void SpoolingFinally()
    {
        base.SpoolingFinally();
        m_source.Dispose();
    }

    protected override void SpoolingWork()
    {
        var currentElement = default(TInputOutput);
        var currentKey = default(TIgnoreKey);
        do
        {
        } while (m_source.MoveNext(ref currentElement, ref currentKey));
    }
}