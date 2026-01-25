#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

internal interface IIListProvider<TElement> : IEnumerable<TElement>, IEnumerable
{
    int GetCount(bool onlyIfCheap);
    TElement[] ToArray();

    List<TElement> ToList();
}