namespace System.Collections.Generic;

internal struct ArrayBuilder<T>
{
    private const int DefaultCapacity = 4;

    private const int MaxCoreClrArrayLength = 2146435071;

    private T[] _array;

    public int Capacity
    {
        get
        {
            var tArray = _array;
            if (tArray == null)
            {
                return 0;
            }

            return tArray.Length;
        }
    }

    public int Count { get; private set; }

    public T this[int index]
    {
        get => _array[index];
        set => _array[index] = value;
    }

    public ArrayBuilder(int capacity)
    {
        this = new ArrayBuilder<T>();
        if (capacity > 0)
        {
            _array = new T[capacity];
        }
    }

    public void Add(T item)
    {
        if (Count == Capacity)
        {
            EnsureCapacity(Count + 1);
        }

        UncheckedAdd(item);
    }

    private void EnsureCapacity(int minimum)
    {
        var capacity = Capacity;
        var num = capacity == 0 ? 4 : 2 * capacity;
        if (num > 2146435071)
        {
            num = Math.Max(capacity + 1, 2146435071);
        }

        num = Math.Max(num, minimum);
        var tArray = new T[num];
        if (Count > 0)
        {
            Array.Copy(_array, 0, tArray, 0, Count);
        }

        _array = tArray;
    }

    public T First()
    {
        return _array[0];
    }

    public T Last()
    {
        return _array[Count - 1];
    }

    public T[] ToArray()
    {
        if (Count == 0)
        {
            return new T[0];
        }

        var tArray = _array;
        if (Count < tArray.Length)
        {
            tArray = new T[Count];
            Array.Copy(_array, 0, tArray, 0, Count);
        }

        return tArray;
    }

    public void UncheckedAdd(T item)
    {
        var tArray = _array;
        var num = Count;
        Count = num + 1;
        tArray[num] = item;
    }
}