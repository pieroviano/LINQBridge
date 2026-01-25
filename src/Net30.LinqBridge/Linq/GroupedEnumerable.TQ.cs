#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

internal class GroupedEnumerable<TSource, TKey, TElement, TResult> :
    IEnumerable<TResult>,
    IEnumerable
{
    private readonly IEqualityComparer<TKey> comparer;
    private readonly Func<TSource, TElement> elementSelector;
    private readonly Func<TSource, TKey> keySelector;
    private readonly Func<TKey, IEnumerable<TElement>, TResult> resultSelector;
    private readonly IEnumerable<TSource> source;

    public GroupedEnumerable(
        IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
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

        if (resultSelector == null)
        {
            throw Error.ArgumentNull(nameof(resultSelector));
        }

        this.source = source;
        this.keySelector = keySelector;
        this.elementSelector = elementSelector;
        this.comparer = comparer;
        this.resultSelector = resultSelector;
    }

    public IEnumerator<TResult> GetEnumerator()
    {
        return Lookup<TKey, TElement>.Create(source, keySelector, elementSelector, comparer)
            .ApplyResultSelector(resultSelector).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}