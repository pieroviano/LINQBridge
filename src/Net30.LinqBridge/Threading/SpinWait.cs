namespace System.Threading;

/// <summary>
///     .NET 2.0 compatible SpinWait replacement
/// </summary>
public struct SpinWait
{
    /// <summary>
    ///     Number of SpinOnce calls so far
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    ///     Performs a single spin operation with progressive backoff
    /// </summary>
    public void SpinOnce()
    {
        if (Count < 10)
        {
            // Phase 1 — pure CPU spinning
            Thread.SpinWait(4 << Count); // exponential backoff
        }
        else if (Count < 20)
        {
            // Phase 2 — give up remainder of time slice
            Thread.Sleep(0);
        }
        else
        {
            // Phase 3 — real sleep to avoid CPU burn
            Thread.Sleep(1);
        }

        Count++;
    }

    /// <summary>
    ///     Suggests whether next SpinOnce will yield the thread
    /// </summary>
    public bool NextSpinWillYield => Count >= 10;

    /// <summary>
    ///     Resets the spin counter
    /// </summary>
    public void Reset()
    {
        Count = 0;
    }
}