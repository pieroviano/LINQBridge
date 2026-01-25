namespace System.Collections.Generic;

internal struct SparseArrayBuilder<T>
{
    private LargeArrayBuilder<T> _builder;

    private ArrayBuilder<Marker> _markers;

    private int _reservedCount;

    public int Count => checked(_builder.Count + _reservedCount);

    public ArrayBuilder<Marker> Markers => _markers;

    public SparseArrayBuilder(bool initialize)
    {
        this = new SparseArrayBuilder<T>
        {
            _builder = new LargeArrayBuilder<T>(true)
        };
    }

    public void Add(T item)
    {
        _builder.Add(item);
    }

    public void AddRange(IEnumerable<T> items)
    {
        _builder.AddRange(items);
    }

    public void CopyTo(T[] array, int arrayIndex, int count)
    {
        var num = 0;
        var start = CopyPosition.Start;
        for (var i = 0; i < _markers.Count; i++)
        {
            var item = _markers[i];
            var num1 = Math.Min(item.Index - num, count);
            if (num1 > 0)
            {
                start = _builder.CopyTo(start, array, arrayIndex, num1);
                arrayIndex += num1;
                num += num1;
                count -= num1;
            }

            if (count == 0)
            {
                return;
            }

            var num2 = Math.Min(item.Count, count);
            arrayIndex += num2;
            num += num2;
            count -= num2;
        }

        _builder.CopyTo(start, array, arrayIndex, count);
    }

    public void Reserve(int count)
    {
        _markers.Add(new Marker(count, Count));
        _reservedCount = checked(_reservedCount + count);
    }

    public bool ReserveOrAdd(IEnumerable<T> items)
    {
        int num;
        if (!EnumerableHelpers.TryGetCount<T>(items, out num))
        {
            AddRange(items);
        }
        else if (num > 0)
        {
            Reserve(num);
            return true;
        }

        return false;
    }

    public T[] ToArray()
    {
        if (_markers.Count == 0)
        {
            return _builder.ToArray();
        }

        var tArray = new T[Count];
        CopyTo(tArray, 0, tArray.Length);
        return tArray;
    }
}