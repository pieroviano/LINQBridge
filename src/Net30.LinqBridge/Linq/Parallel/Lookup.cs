#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class Lookup<TKey, TElement> :
    ILookup<TKey, TElement>,
    IEnumerable<IGrouping<TKey, TElement>>,
    IEnumerable
{
    private readonly IEqualityComparer<TKey> m_comparer;
    private readonly IDictionary<TKey, IGrouping<TKey, TElement>> m_dict;
    private IGrouping<TKey, TElement> m_defaultKeyGrouping;

    internal Lookup(IEqualityComparer<TKey> comparer)
    {
        m_comparer = comparer;
        m_dict = new Dictionary<TKey, IGrouping<TKey, TElement>>(m_comparer);
    }

    public int Count
    {
        get
        {
            var count = m_dict.Count;
            if (m_defaultKeyGrouping != null)
            {
                ++count;
            }

            return count;
        }
    }

    public IEnumerable<TElement> this[TKey key]
    {
        get
        {
            IGrouping<TKey, TElement> grouping;
            return m_comparer.Equals(key, default)
                ?
                m_defaultKeyGrouping != null ? m_defaultKeyGrouping : Enumerable.Empty<TElement>()
                : m_dict.TryGetValue(key, out grouping)
                    ? grouping
                    : Enumerable.Empty<TElement>();
        }
    }

    public bool Contains(TKey key)
    {
        return m_comparer.Equals(key, default) ? m_defaultKeyGrouping != null : m_dict.ContainsKey(key);
    }

    public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
    {
        foreach (var grouping in m_dict.Values)
        {
            yield return grouping;
        }

        if (m_defaultKeyGrouping != null)
        {
            yield return m_defaultKeyGrouping;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal void Add(IGrouping<TKey, TElement> grouping)
    {
        if (m_comparer.Equals(grouping.Key, default))
        {
            m_defaultKeyGrouping = grouping;
        }
        else
        {
            m_dict.Add(grouping.Key, grouping);
        }
    }
}