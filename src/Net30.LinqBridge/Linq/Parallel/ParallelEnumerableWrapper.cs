#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class ParallelEnumerableWrapper : ParallelQuery<object>
{
    private readonly IEnumerable m_source;

    internal ParallelEnumerableWrapper(IEnumerable source)
        : base(QuerySettings.Empty)
    {
        m_source = source;
    }

    public override IEnumerator<object> GetEnumerator()
    {
        return new EnumerableWrapperWeakToStrong(m_source).GetEnumerator();
    }

    internal override IEnumerator GetEnumeratorUntyped()
    {
        return m_source.GetEnumerator();
    }
}