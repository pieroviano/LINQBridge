#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

internal abstract class OrderedEnumerable<TElement> :
    IOrderedEnumerable<TElement>,
    IEnumerable<TElement>,
    IEnumerable
{
    internal IEnumerable<TElement> source;

    public IEnumerator<TElement> GetEnumerator()
    {
        var buffer = new Buffer<TElement>(source);
        if (buffer.count > 0)
        {
            var map = GetEnumerableSorter(null).Sort(buffer.items, buffer.count);
            for (var i = 0; i < buffer.count; ++i)
            {
                yield return buffer.items[map[i]];
            }

            map = null;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    IOrderedEnumerable<TElement> IOrderedEnumerable<TElement>.CreateOrderedEnumerable<TKey>(
        Func<TElement, TKey> keySelector,
        IComparer<TKey> comparer,
        bool descending)
    {
        return new OrderedEnumerable<TElement, TKey>(source, keySelector, comparer, descending)
        {
            parent = this
        };
    }

    internal abstract EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next);
}