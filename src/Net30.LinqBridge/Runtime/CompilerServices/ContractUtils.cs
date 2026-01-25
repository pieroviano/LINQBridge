using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq.Expressions;

namespace System.Runtime.CompilerServices;

internal class ContractUtils
{
    public static Exception Unreachable => throw new CompilerServicesException("Unreachable");

    public static void Requires(bool b, string s)
    {
        if (!b)
        {
            throw new ArgumentException(s);
        }
    }

    public static void RequiresNotEmpty<T>(ReadOnlyCollection<T> readOnly, string expressions)
    {
        if (readOnly.Count == 0)
        {
            throw new ArgumentException(expressions);
        }
    }

    public static void RequiresNotEmpty<T>(T[] readOnly, string expressions)
    {
        if (readOnly.Length == 0)
        {
            throw new ArgumentException(expressions);
        }
    }

    public static void RequiresNotNull(object array, string s)
    {
        if (array is null)
        {
            throw new ArgumentException(s);
        }
    }

    internal static void Requires(bool condition, string paramName, string message)
    {
        if (!condition)
        {
            throw new ArgumentException(message, paramName);
        }
    }

    internal static void Requires(bool precondition)
    {
        if (!precondition)
        {
            throw new ArgumentException(Strings.MethodPreconditionViolated);
        }
    }

    internal static void RequiresArrayRange<T>(IList<T> array, int offset, int count, string offsetName,
        string countName)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(countName);
        }

        if (offset < 0 || array.Count - offset < count)
        {
            throw new ArgumentOutOfRangeException(offsetName);
        }
    }

    internal static void RequiresNotNullItems<T>(IList<T> array, string arrayName)
    {
        RequiresNotNull(array, arrayName);
        for (var i = 0; i < array.Count; i++)
        {
            if (array[i] == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, "{0}[{1}]", arrayName, i));
            }
        }
    }
}