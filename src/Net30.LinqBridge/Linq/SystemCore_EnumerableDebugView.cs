#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq;

internal sealed class SystemCore_EnumerableDebugView
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly IEnumerable enumerable;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private object[] cachedCollection;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int count;

    public SystemCore_EnumerableDebugView(IEnumerable enumerable)
    {
        this.enumerable = enumerable != null ? enumerable : throw new ArgumentNullException(nameof(enumerable));
        count = 0;
        cachedCollection = null;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public object[] Items
    {
        get
        {
            var objectList = new List<object>();
            var enumerator = enumerable.GetEnumerator();
            if (enumerator != null)
            {
                count = 0;
                while (enumerator.MoveNext())
                {
                    objectList.Add(enumerator.Current);
                    ++count;
                }
            }

            cachedCollection =
                count != 0 ? new object[count] : throw new SystemCore_EnumerableDebugViewEmptyException();
            objectList.CopyTo(cachedCollection, 0);
            return cachedCollection;
        }
    }
}