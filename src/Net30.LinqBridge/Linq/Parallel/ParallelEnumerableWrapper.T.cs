#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class ParallelEnumerableWrapper<T> : ParallelQuery<T>
{
    internal ParallelEnumerableWrapper(IEnumerable<T> wrappedEnumerable)
        : base(QuerySettings.Empty)
    {
        WrappedEnumerable = wrappedEnumerable;
    }

    internal IEnumerable<T> WrappedEnumerable { get; }

    public override IEnumerator<T> GetEnumerator()
    {
        return WrappedEnumerable.GetEnumerator();
    }
}