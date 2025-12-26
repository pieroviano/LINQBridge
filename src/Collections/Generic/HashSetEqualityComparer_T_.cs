using System;

namespace System.Collections.Generic;

[Serializable]
internal class HashSetEqualityComparer<T> : IEqualityComparer<HashSet<T>>
{
    private IEqualityComparer<T> _comparer;

    public HashSetEqualityComparer()
    {
        _comparer = EqualityComparer<T>.Default;
    }

    public HashSetEqualityComparer(IEqualityComparer<T> comparer)
    {
        if (_comparer == null)
        {
            _comparer = EqualityComparer<T>.Default;
            return;
        }
        _comparer = comparer;
    }

    public bool Equals(HashSet<T> x, HashSet<T> y)
    {
        return HashSet<T>.HashSetEquals(x, y, _comparer);
    }

    public override bool Equals(object? obj)
    {
        var hashSetEqualityComparer = obj as HashSetEqualityComparer<T>;
        if (hashSetEqualityComparer == null)
        {
            return false;
        }
        return _comparer == hashSetEqualityComparer._comparer;
    }

    public int GetHashCode(HashSet<T> obj)
    {
        var hashCode = 0;
        if (obj != null)
        {
            foreach (var t in obj)
            {
                if (t != null)
                {
                    hashCode = hashCode ^ _comparer.GetHashCode(t) & 2147483647;
                }
            }
        }
        return hashCode;
    }

    public override int GetHashCode()
    {
        return _comparer.GetHashCode();
    }
}