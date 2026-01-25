#nullable disable
namespace System.Linq.Parallel;

internal class GrowingArray<T>
{
    private const int DEFAULT_ARRAY_SIZE = 1024 /*0x0400*/;

    internal GrowingArray()
    {
        InternalArray = new T[1024 /*0x0400*/];
        Count = 0;
    }

    internal T[] InternalArray { get; private set; }

    internal int Count { get; private set; }

    internal void Add(T element)
    {
        if (Count >= InternalArray.Length)
        {
            GrowArray(2 * InternalArray.Length);
        }

        InternalArray[Count++] = element;
    }

    internal void CopyFrom(T[] otherArray, int otherCount)
    {
        if (Count + otherCount > InternalArray.Length)
        {
            GrowArray(Count + otherCount);
        }

        Array.Copy(otherArray, 0, InternalArray, Count, otherCount);
        Count += otherCount;
    }

    private void GrowArray(int newSize)
    {
        var objArray = new T[newSize];
        InternalArray.CopyTo(objArray, 0);
        InternalArray = objArray;
    }
}