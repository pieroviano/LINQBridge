using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System;

internal static class CollectionExtensions
{
    internal static T[] AddFirst<T>(this IList<T> list, T item)
    {
        var tArray = new T[list.Count + 1];
        tArray[0] = item;
        list.CopyTo(tArray, 1);
        return tArray;
    }

    internal static T[] AddLast<T>(this IList<T> list, T item)
    {
        var tArray = new T[list.Count + 1];
        list.CopyTo(tArray, 0);
        tArray[list.Count] = item;
        return tArray;
    }

    internal static bool All<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        bool flag;
        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current))
                {
                    continue;
                }

                flag = false;
                return flag;
            }

            return true;
        }

        return flag;
    }

    internal static bool Any<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        bool flag;
        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                if (!predicate(enumerator.Current))
                {
                    continue;
                }

                flag = true;
                return flag;
            }

            return false;
        }

        return flag;
    }

    internal static T[] Copy<T>(this T[] array)
    {
        var tArray = new T[array.Length];
        Array.Copy(array, tArray, array.Length);
        return tArray;
    }

    internal static T First<T>(this IEnumerable<T> source)
    {
        T current;
        var ts = source as IList<T>;
        if (ts != null)
        {
            return ts[0];
        }

        using (var enumerator = source.GetEnumerator())
        {
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException();
            }

            current = enumerator.Current;
        }

        return current;
    }

    internal static T Last<T>(this IList<T> list)
    {
        return list[list.Count - 1];
    }

    internal static bool ListEquals<T>(this ICollection<T> first, ICollection<T> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        var @default = EqualityComparer<T>.Default;
        var enumerator = first.GetEnumerator();
        var enumerator1 = second.GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerator1.MoveNext();
            if (@default.Equals(enumerator.Current, enumerator1.Current))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    internal static int ListHashCode<T>(this IEnumerable<T> list)
    {
        var @default = EqualityComparer<T>.Default;
        var hashCode = 6551;
        foreach (var t in list)
        {
            hashCode = hashCode ^ (hashCode << 5) ^ @default.GetHashCode(t);
        }

        return hashCode;
    }

    internal static U[] Map<T, U>(this ICollection<T> collection, Func<T, U> select)
    {
        var count = collection.Count;
        var uArray = new U[count];
        count = 0;
        foreach (var t in collection)
        {
            var num = count;
            count = num + 1;
            uArray[num] = select(t);
        }

        return uArray;
    }

    internal static T[] RemoveFirst<T>(this T[] array)
    {
        var tArray = new T[array.Length - 1];
        Array.Copy(array, 1, tArray, 0, tArray.Length);
        return tArray;
    }

    internal static T[] RemoveLast<T>(this T[] array)
    {
        var tArray = new T[array.Length - 1];
        Array.Copy(array, 0, tArray, 0, tArray.Length);
        return tArray;
    }

    internal static IEnumerable<U> Select<T, U>(IEnumerable<T> enumerable, Func<T, U> select)
    {
        foreach (var t in enumerable)
        {
            yield return select(t);
        }
    }

    internal static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> enumerable)
    {
        if (enumerable == null)
        {
            return EmptyReadOnlyCollection<T>.Instance;
        }

        var trueReadOnlyCollection = enumerable as TrueReadOnlyCollection<T>;
        if (trueReadOnlyCollection != null)
        {
            return trueReadOnlyCollection;
        }

        var ts = enumerable as ReadOnlyCollectionBuilder<T>;
        if (ts != null)
        {
            return ts.ToReadOnlyCollection();
        }

        var ts1 = enumerable as ICollection<T>;
        if (ts1 == null)
        {
            return new TrueReadOnlyCollection<T>(new List<T>(enumerable).ToArray());
        }

        var count = ts1.Count;
        if (count == 0)
        {
            return EmptyReadOnlyCollection<T>.Instance;
        }

        var tArray = new T[count];
        ts1.CopyTo(tArray, 0);
        return new TrueReadOnlyCollection<T>(tArray);
    }

    internal static IEnumerable<T> Where<T>(IEnumerable<T> enumerable, Func<T, bool> where)
    {
        foreach (var t in enumerable)
        {
            if (!where(t))
            {
                continue;
            }

            yield return t;
        }
    }
}