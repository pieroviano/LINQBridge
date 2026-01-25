#nullable disable
using System.Collections.Generic;

namespace System.Linq;

internal class OrderedEnumerable<TElement, TKey> : OrderedEnumerable<TElement>
{
    internal IComparer<TKey> comparer;
    internal bool descending;
    internal Func<TElement, TKey> keySelector;
    internal OrderedEnumerable<TElement> parent;

    internal OrderedEnumerable(
        IEnumerable<TElement> source,
        Func<TElement, TKey> keySelector,
        IComparer<TKey> comparer,
        bool descending)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (keySelector == null)
        {
            throw Error.ArgumentNull(nameof(keySelector));
        }

        this.source = source;
        parent = null;
        this.keySelector = keySelector;
        this.comparer = comparer != null ? comparer : Comparer<TKey>.Default;
        this.descending = descending;
    }

    internal override EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next)
    {
        var next1 = (EnumerableSorter<TElement>)new EnumerableSorter<TElement, TKey>(keySelector, comparer, descending,
            next);
        if (parent != null)
        {
            next1 = parent.GetEnumerableSorter(next1);
        }

        return next1;
    }
}