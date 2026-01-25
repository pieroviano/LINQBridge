#nullable disable
using System.Collections.Generic;

namespace System.Linq;

internal class EnumerableSorter<TElement, TKey> : EnumerableSorter<TElement>
{
    internal IComparer<TKey> comparer;
    internal bool descending;
    internal TKey[] keys;
    internal Func<TElement, TKey> keySelector;
    internal EnumerableSorter<TElement> next;

    internal EnumerableSorter(
        Func<TElement, TKey> keySelector,
        IComparer<TKey> comparer,
        bool descending,
        EnumerableSorter<TElement> next)
    {
        this.keySelector = keySelector;
        this.comparer = comparer;
        this.descending = descending;
        this.next = next;
    }

    internal override int CompareKeys(int index1, int index2)
    {
        var num = comparer.Compare(keys[index1], keys[index2]);
        return num == 0 ? next == null ? index1 - index2 : next.CompareKeys(index1, index2) : !descending ? num : -num;
    }

    internal override void ComputeKeys(TElement[] elements, int count)
    {
        keys = new TKey[count];
        for (var index = 0; index < count; ++index)
        {
            keys[index] = keySelector(elements[index]);
        }

        if (next == null)
        {
            return;
        }

        next.ComputeKeys(elements, count);
    }
}