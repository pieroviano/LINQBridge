#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

internal class GroupedEnumerable<TSource, TKey, TElement> :
    IEnumerable<IGrouping<TKey, TElement>>,
    IEnumerable
{
    private readonly IEqualityComparer<TKey> comparer;
    private readonly Func<TSource, TElement> elementSelector;
    private readonly Func<TSource, TKey> keySelector;
    private readonly IEnumerable<TSource> source;

    public GroupedEnumerable(
        IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        IEqualityComparer<TKey> comparer)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (keySelector == null)
        {
            throw Error.ArgumentNull(nameof(keySelector));
        }

        if (elementSelector == null)
        {
            throw Error.ArgumentNull(nameof(elementSelector));
        }

        this.source = source;
        this.keySelector = keySelector;
        this.elementSelector = elementSelector;
        this.comparer = comparer;
    }

    public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
    {
        return Lookup<TKey, TElement>.Create(source, keySelector, elementSelector, comparer).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}