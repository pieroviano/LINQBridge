using System.Collections.Generic;
using System.Threading;

namespace System.Runtime.CompilerServices;

internal class CacheDict<TKey, TValue>
{
    protected readonly Entry[] entries;
    protected readonly int mask;

    internal CacheDict(int size)
    {
        var num = AlignSize(size);
        mask = num - 1;
        entries = new Entry[num];
    }

    internal TValue this[TKey key]
    {
        get
        {
            TValue tValue;
            if (!TryGetValue(key, out tValue))
            {
                throw new KeyNotFoundException();
            }

            return tValue;
        }
        set => Add(key, value);
    }

    internal void Add(TKey key, TValue value)
    {
        var hashCode = key.GetHashCode();
        var num = hashCode & mask;
#pragma warning disable CS0436
        var entry = Volatile.Read(ref entries[num]);
        if (entry == null || entry.hash != hashCode || !entry.key.Equals(key))
        {
            Volatile.Write<Entry>(ref entries[num], new Entry(hashCode, key, value));
#pragma warning restore CS0436
        }
    }

    internal bool TryGetValue(TKey key, out TValue value)
    {
        var hashCode = key.GetHashCode();
        var num = hashCode & mask;
#pragma warning disable CS0436
        var entry = Volatile.Read(ref entries[num]);
#pragma warning restore CS0436
        if (entry == null || entry.hash != hashCode || !entry.key.Equals(key))
        {
            value = default;
            return false;
        }

        value = entry.value;
        return true;
    }

    private static int AlignSize(int size)
    {
        size--;
        size = size | (size >> 1);
        size = size | (size >> 2);
        size = size | (size >> 4);
        size = size | (size >> 8);
        size = size | (size >> 16);
        return size + 1;
    }

    internal class Entry
    {
        internal readonly int hash;

        internal readonly TKey key;

        internal readonly TValue value;

        internal Entry(int hash, TKey key, TValue value)
        {
            this.hash = hash;
            this.key = key;
            this.value = value;
        }
    }
}