#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal abstract class QueryResults<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
{
    internal virtual bool IsIndexible => false;

    internal virtual int ElementsCount => throw new NotSupportedException();

    int IList<T>.IndexOf(T item)
    {
        throw new NotSupportedException();
    }

    void IList<T>.Insert(int index, T item)
    {
        throw new NotSupportedException();
    }

    void IList<T>.RemoveAt(int index)
    {
        throw new NotSupportedException();
    }

    public T this[int index]
    {
        get => GetElement(index);
        set => throw new NotSupportedException();
    }

    void ICollection<T>.Add(T item)
    {
        throw new NotSupportedException();
    }

    void ICollection<T>.Clear()
    {
        throw new NotSupportedException();
    }

    bool ICollection<T>.Contains(T item)
    {
        throw new NotSupportedException();
    }

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        throw new NotSupportedException();
    }

    public int Count => ElementsCount;

    bool ICollection<T>.IsReadOnly => true;

    bool ICollection<T>.Remove(T item)
    {
        throw new NotSupportedException();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        for (var index = 0; index < Count; ++index)
        {
            yield return this[index];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }

    internal virtual T GetElement(int index)
    {
        throw new NotSupportedException();
    }

    internal abstract void GivePartitionedStream(IPartitionedStreamRecipient<T> recipient);
}