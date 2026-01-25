using System.Reflection;

namespace System.Runtime.CompilerServices;

internal static class PrivateReader
{
    public static int GetLength(object obj)
    {
        var fi = obj.GetType().GetField(
            "m_stringLength", // try this first
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (fi == null)
        {
            fi = obj.GetType().GetField(
                "m_length", // fallback name
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        if (fi == null)
        {
            throw new Exception("Field not found.");
        }

        return (int)fi.GetValue(obj);
    }
}