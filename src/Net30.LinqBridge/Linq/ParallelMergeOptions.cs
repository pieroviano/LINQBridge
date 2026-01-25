namespace System.Linq;

/// <summary>
///     Specifies the preferred type of output merge to use in a query. In other words, it indicates how PLINQ should
///     merge the results from the various partitions back into a single result sequence. This is a hint only, and may not
///     be respected by the system when parallelizing all queries.
/// </summary>
public enum ParallelMergeOptions
{
    /// <summary>Use the default merge type, which is AutoBuffered.</summary>
    Default,

    /// <summary>
    ///     Use a merge without output buffers. As soon as result elements have been computed, make that element available
    ///     to the consumer of the query.
    /// </summary>
    NotBuffered,

    /// <summary>
    ///     Use a merge with output buffers of a size chosen by the system. Results will accumulate into an output buffer
    ///     before they are available to the consumer of the query.
    /// </summary>
    AutoBuffered,

    /// <summary>
    ///     Use a merge with full output buffers. The system will accumulate all of the results before making any of them
    ///     available to the consumer of the query.
    /// </summary>
    FullyBuffered
}