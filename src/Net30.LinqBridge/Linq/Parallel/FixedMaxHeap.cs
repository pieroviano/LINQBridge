#nullable disable
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq.Parallel;

internal class FixedMaxHeap<TElement>
{
    private readonly IComparer<TElement> m_comparer;
    private readonly TElement[] m_elements;

    internal FixedMaxHeap(int maximumSize)
        : this(maximumSize, Util.GetDefaultComparer<TElement>())
    {
    }

    internal FixedMaxHeap(int maximumSize, IComparer<TElement> comparer)
    {
        m_elements = new TElement[maximumSize];
        m_comparer = comparer;
    }

    internal int Count { get; private set; }

    internal int Size => m_elements.Length;

    internal TElement MaxValue
    {
        get
        {
            if (Count == 0)
            {
                throw new InvalidOperationException(Strings.NoElements());
            }

            return m_elements[0];
        }
    }

    internal void Clear()
    {
        Count = 0;
    }

    internal bool Insert(TElement e)
    {
        if (Count < m_elements.Length)
        {
            m_elements[Count] = e;
            ++Count;
            HeapifyLastLeaf();
            return true;
        }

        if (m_comparer.Compare(e, m_elements[0]) >= 0)
        {
            return false;
        }

        m_elements[0] = e;
        HeapifyRoot();
        return true;
    }

    internal void RemoveMax()
    {
        --Count;
        if (Count <= 0)
        {
            return;
        }

        m_elements[0] = m_elements[Count];
        HeapifyRoot();
    }

    internal void ReplaceMax(TElement newValue)
    {
        m_elements[0] = newValue;
        HeapifyRoot();
    }

    private void HeapifyLastLeaf()
    {
        int j;
        for (var i = Count - 1; i > 0; i = j)
        {
            j = (i + 1) / 2 - 1;
            if (m_comparer.Compare(m_elements[i], m_elements[j]) <= 0)
            {
                break;
            }

            Swap(i, j);
        }
    }

    private void HeapifyRoot()
    {
        var i = 0;
        var count = Count;
        while (i < count)
        {
            var j1 = (i + 1) * 2 - 1;
            var j2 = j1 + 1;
            if (j1 < count && m_comparer.Compare(m_elements[i], m_elements[j1]) < 0)
            {
                if (j2 < count && m_comparer.Compare(m_elements[j1], m_elements[j2]) < 0)
                {
                    Swap(i, j2);
                    i = j2;
                }
                else
                {
                    Swap(i, j1);
                    i = j1;
                }
            }
            else
            {
                if (j2 >= count || m_comparer.Compare(m_elements[i], m_elements[j2]) >= 0)
                {
                    break;
                }

                Swap(i, j2);
                i = j2;
            }
        }
    }

    private void Swap(int i, int j)
    {
        var element = m_elements[i];
        m_elements[i] = m_elements[j];
        m_elements[j] = element;
    }
}