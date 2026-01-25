// Type: System.Dynamic.CollectionExtensions
using System.Collections.Generic;

#nullable disable
namespace System.Linq.Expressions;

internal static class CollectionExtensions
{
  internal static T[] RemoveFirst<T>(this T[] array)
  {
    T[] destinationArray = new T[array.Length - 1];
    Array.Copy((Array) array, 1, (Array) destinationArray, 0, destinationArray.Length);
    return destinationArray;
  }

  internal static T[] AddFirst<T>(this IList<T> list, T item)
  {
    T[] array = new T[list.Count + 1];
    array[0] = item;
    list.CopyTo(array, 1);
    return array;
  }

  internal static T[] ToArray<T>(this IList<T> list)
  {
    T[] array = new T[list.Count];
    list.CopyTo(array, 0);
    return array;
  }

  internal static T[] AddLast<T>(this IList<T> list, T item)
  {
    T[] array = new T[list.Count + 1];
    list.CopyTo(array, 0);
    array[list.Count] = item;
    return array;
  }
}
