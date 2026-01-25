#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class HashLookup<TKey, TValue>
{
    private readonly IEqualityComparer<TKey> comparer;
    private int[] buckets;
    private int freeList;
    private Slot[] slots;

    internal HashLookup()
        : this(null)
    {
    }

    internal HashLookup(IEqualityComparer<TKey> comparer)
    {
        this.comparer = comparer;
        buckets = new int[7];
        slots = new Slot[7];
        freeList = -1;
    }

    internal TValue this[TKey key]
    {
        set
        {
            var obj = value;
            Find(key, false, true, ref obj);
        }
    }

    internal int Count { get; private set; }

    internal KeyValuePair<TKey, TValue> this[int index] => new(slots[index].key, slots[index].value);

    internal bool Add(TKey key, TValue value)
    {
        return !Find(key, true, false, ref value);
    }

    internal bool Remove(TKey key)
    {
        var keyHashCode = GetKeyHashCode(key);
        var index1 = keyHashCode % buckets.Length;
        var index2 = -1;
        for (var index3 = buckets[index1] - 1; index3 >= 0; index3 = slots[index3].next)
        {
            if (slots[index3].hashCode == keyHashCode && AreKeysEqual(slots[index3].key, key))
            {
                if (index2 < 0)
                {
                    buckets[index1] = slots[index3].next + 1;
                }
                else
                {
                    slots[index2].next = slots[index3].next;
                }

                slots[index3].hashCode = -1;
                slots[index3].key = default;
                slots[index3].value = default;
                slots[index3].next = freeList;
                freeList = index3;
                return true;
            }

            index2 = index3;
        }

        return false;
    }

    internal bool TryGetValue(TKey key, ref TValue value)
    {
        return Find(key, false, false, ref value);
    }

    private bool AreKeysEqual(TKey key1, TKey key2)
    {
        if (comparer != null)
        {
            return comparer.Equals(key1, key2);
        }

        if (key1 == null && key2 == null)
        {
            return true;
        }

        return key1 != null && key1.Equals(key2);
    }

    private bool Find(TKey key, bool add, bool set, ref TValue value)
    {
        var keyHashCode = GetKeyHashCode(key);
        for (var index = buckets[keyHashCode % buckets.Length] - 1; index >= 0; index = slots[index].next)
        {
            if (slots[index].hashCode == keyHashCode && AreKeysEqual(slots[index].key, key))
            {
                if (set)
                {
                    slots[index].value = value;
                    return true;
                }

                value = slots[index].value;
                return true;
            }
        }

        if (add)
        {
            int index1;
            if (freeList >= 0)
            {
                index1 = freeList;
                freeList = slots[index1].next;
            }
            else
            {
                if (Count == slots.Length)
                {
                    Resize();
                }

                index1 = Count;
                ++Count;
            }

            var index2 = keyHashCode % buckets.Length;
            slots[index1].hashCode = keyHashCode;
            slots[index1].key = key;
            slots[index1].value = value;
            slots[index1].next = buckets[index2] - 1;
            buckets[index2] = index1 + 1;
        }

        return false;
    }

    private int GetKeyHashCode(TKey key)
    {
        return int.MaxValue & (comparer == null ? key == null ? 0 : key.GetHashCode() : comparer.GetHashCode(key));
    }

    private void Resize()
    {
        var length = checked(Count * 2 + 1);
        var numArray = new int[length];
        var destinationArray = new Slot[length];
        Array.Copy(slots, 0, destinationArray, 0, Count);
        for (var index1 = 0; index1 < Count; ++index1)
        {
            var index2 = destinationArray[index1].hashCode % length;
            destinationArray[index1].next = numArray[index2] - 1;
            numArray[index2] = index1 + 1;
        }

        buckets = numArray;
        slots = destinationArray;
    }

    internal struct Slot
    {
        internal int hashCode;
        internal TKey key;
        internal TValue value;
        internal int next;
    }
}