#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class EmptyEnumerator<T> :
    QueryOperatorEnumerator<T, int>,
    IEnumerator<T>,
    IDisposable,
    IEnumerator
{
    public T Current => default;

    object IEnumerator.Current => null;

    public bool MoveNext()
    {
        return false;
    }

    void IEnumerator.Reset()
    {
    }

    internal override bool MoveNext(ref T currentElement, ref int currentKey)
    {
        return false;
    }
}