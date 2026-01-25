#nullable disable
using System.Collections.Generic;

namespace System.Linq;

internal class Set<TElement>
{
    private readonly IEqualityComparer<TElement> comparer;
    private int[] buckets;
    private int count;
    private int freeList;
    private Slot[] slots;

    public Set()
        : this(null)
    {
    }

    public Set(IEqualityComparer<TElement> comparer)
    {
        if (comparer == null)
        {
            comparer = EqualityComparer<TElement>.Default;
        }

        this.comparer = comparer;
        buckets = new int[7];
        slots = new Slot[7];
        freeList = -1;
    }

    public bool Add(TElement value)
    {
        return !Find(value, true);
    }

    public bool Contains(TElement value)
    {
        return Find(value, false);
    }

    public bool Remove(TElement value)
    {
        var hashCode = InternalGetHashCode(value);
        var index1 = hashCode % buckets.Length;
        var index2 = -1;
        for (var index3 = buckets[index1] - 1; index3 >= 0; index3 = slots[index3].next)
        {
            if (slots[index3].hashCode == hashCode && comparer.Equals(slots[index3].value, value))
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
                slots[index3].value = default;
                slots[index3].next = freeList;
                freeList = index3;
                return true;
            }

            index2 = index3;
        }

        return false;
    }

    internal int InternalGetHashCode(TElement value)
    {
        return value != null ? comparer.GetHashCode(value) & int.MaxValue : 0;
    }

    private bool Find(TElement value, bool add)
    {
        var hashCode = InternalGetHashCode(value);
        for (var index = buckets[hashCode % buckets.Length] - 1; index >= 0; index = slots[index].next)
        {
            if (slots[index].hashCode == hashCode && comparer.Equals(slots[index].value, value))
            {
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
                if (count == slots.Length)
                {
                    Resize();
                }

                index1 = count;
                ++count;
            }

            var index2 = hashCode % buckets.Length;
            slots[index1].hashCode = hashCode;
            slots[index1].value = value;
            slots[index1].next = buckets[index2] - 1;
            buckets[index2] = index1 + 1;
        }

        return false;
    }

    private void Resize()
    {
        var length = checked(count * 2 + 1);
        var numArray = new int[length];
        var destinationArray = new Slot[length];
        Array.Copy(slots, 0, destinationArray, 0, count);
        for (var index1 = 0; index1 < count; ++index1)
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
        internal TElement value;
        internal int next;
    }
}