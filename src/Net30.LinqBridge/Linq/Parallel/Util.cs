#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal static class Util
{
    private static readonly FastIntComparer s_fastIntComparer = new();
    private static readonly FastLongComparer s_fastLongComparer = new();
    private static readonly FastFloatComparer s_fastFloatComparer = new();
    private static readonly FastDoubleComparer s_fastDoubleComparer = new();
    private static readonly FastDateTimeComparer s_fastDateTimeComparer = new();

    internal static Comparer<TKey> GetDefaultComparer<TKey>()
    {
        if (typeof(TKey) == typeof(int))
        {
            return (Comparer<TKey>)(object)s_fastIntComparer;
        }

        if (typeof(TKey) == typeof(long))
        {
            return (Comparer<TKey>)(object)s_fastLongComparer;
        }

        if (typeof(TKey) == typeof(float))
        {
            return (Comparer<TKey>)(object)s_fastFloatComparer;
        }

        if (typeof(TKey) == typeof(double))
        {
            return (Comparer<TKey>)(object)s_fastDoubleComparer;
        }

        return typeof(TKey) == typeof(DateTime)
            ? (Comparer<TKey>)(object)s_fastDateTimeComparer
            : Comparer<TKey>.Default;
    }

    internal static int Sign(int x)
    {
        if (x < 0)
        {
            return -1;
        }

        return x != 0 ? 1 : 0;
    }

    private class FastIntComparer : Comparer<int>
    {
        public override int Compare(int x, int y)
        {
            return x.CompareTo(y);
        }
    }

    private class FastLongComparer : Comparer<long>
    {
        public override int Compare(long x, long y)
        {
            return x.CompareTo(y);
        }
    }

    private class FastFloatComparer : Comparer<float>
    {
        public override int Compare(float x, float y)
        {
            return x.CompareTo(y);
        }
    }

    private class FastDoubleComparer : Comparer<double>
    {
        public override int Compare(double x, double y)
        {
            return x.CompareTo(y);
        }
    }

    private class FastDateTimeComparer : Comparer<DateTime>
    {
        public override int Compare(DateTime x, DateTime y)
        {
            return x.CompareTo(y);
        }
    }
}