#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class PairComparer<T, U> : IComparer<Pair<T, U>>
{
    private readonly IComparer<T> m_comparer1;
    private readonly IComparer<U> m_comparer2;

    public PairComparer(IComparer<T> comparer1, IComparer<U> comparer2)
    {
        m_comparer1 = comparer1;
        m_comparer2 = comparer2;
    }

    public int Compare(Pair<T, U> x, Pair<T, U> y)
    {
        var num = m_comparer1.Compare(x.First, y.First);
        return num != 0 ? num : m_comparer2.Compare(x.Second, y.Second);
    }
}