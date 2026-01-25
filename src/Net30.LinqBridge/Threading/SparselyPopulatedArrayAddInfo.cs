namespace System.Threading;

internal struct SparselyPopulatedArrayAddInfo<T>
    where T : class
{
    internal int Index { get; }

    internal SparselyPopulatedArrayFragment<T> Source { get; }

    internal SparselyPopulatedArrayAddInfo(SparselyPopulatedArrayFragment<T> source, int index)
    {
        Source = source;
        Index = index;
    }
}