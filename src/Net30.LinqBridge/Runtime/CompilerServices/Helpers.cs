using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Dynamic.Utils;

internal static class Helpers
{
    internal static T CommonNode<T>(T first, T second, Func<T, T> parent)
        where T : class
    {
        var @default = EqualityComparer<T>.Default;
        if (@default.Equals(first, second))
        {
            return first;
        }

        var ts = new Set<T>(@default);
        for (var i = first; i != null; i = parent(i))
        {
            ts.Add(i);
        }

        for (var j = second; j != null; j = parent(j))
        {
            if (ts.Contains(j))
            {
                return j;
            }
        }

        return default;
    }

    internal static void IncrementCount<T>(T key, Dictionary<T, int> dict)
    {
        int num;
        dict.TryGetValue(key, out num);
        dict[key] = num + 1;
    }
}