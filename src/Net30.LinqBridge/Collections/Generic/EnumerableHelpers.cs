using System.Linq;

namespace System.Collections.Generic;

internal static class EnumerableHelpers
{
    internal static void Copy<T>(IEnumerable<T> source, T[] array, int arrayIndex, int count)
    {
        var ts = source as ICollection<T>;
        if (ts != null)
        {
            ts.CopyTo(array, arrayIndex);
            return;
        }

        IterativeCopy(source, array, arrayIndex, count);
    }

    internal static void IterativeCopy<T>(IEnumerable<T> source, T[] array, int arrayIndex, int count)
    {
        foreach (var t in source)
        {
            var num = arrayIndex;
            arrayIndex = num + 1;
            array[num] = t;
        }
    }

    internal static bool TryGetCount<T>(IEnumerable<T> source, out int count)
    {
        var ts = source as ICollection<T>;
        if (ts != null)
        {
            count = ts.Count;
            return true;
        }

        var ts1 = source as IIListProvider<T>;
        if (ts1 == null)
        {
            count = -1;
            return false;
        }

        count = ts1.GetCount(true);
        return count >= 0;
    }
}