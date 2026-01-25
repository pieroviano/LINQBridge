namespace System.Linq;

/// <summary>
///     The query execution mode is a hint that specifies how the system should handle performance trade-offs when
///     parallelizing queries.
/// </summary>
public enum ParallelExecutionMode
{
    /// <summary>
    ///     This is the default setting. PLINQ will examine the query's structure and will only parallelize the query if
    ///     will likely result in speedup. If the query structure indicates that speedup is not likely to be obtained, then
    ///     PLINQ will execute the query as an ordinary LINQ to Objects query.
    /// </summary>
    Default,

    /// <summary>
    ///     Parallelize the entire query, even if that means using high-overhead algorithms. Use this flag in cases where
    ///     you know that parallel execution of the query will result in speedup, but PLINQ in the Default mode would execute
    ///     it as sequential.
    /// </summary>
    ForceParallelism
}