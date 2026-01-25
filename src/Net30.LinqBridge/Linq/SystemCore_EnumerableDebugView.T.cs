#nullable disable
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq;

internal sealed class SystemCore_EnumerableDebugView<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly IEnumerable<T> enumerable;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private T[] cachedCollection;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int count;

    public SystemCore_EnumerableDebugView(IEnumerable<T> enumerable)
    {
        this.enumerable = enumerable != null ? enumerable : throw new ArgumentNullException(nameof(enumerable));
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            var objList = new List<T>();
            var enumerator = enumerable.GetEnumerator();
            if (enumerator != null)
            {
                count = 0;
                while (enumerator.MoveNext())
                {
                    objList.Add(enumerator.Current);
                    ++count;
                }
            }

            cachedCollection = count != 0 ? new T[count] : throw new SystemCore_EnumerableDebugViewEmptyException();
            objList.CopyTo(cachedCollection, 0);
            return cachedCollection;
        }
    }
}