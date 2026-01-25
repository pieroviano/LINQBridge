using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace System.Runtime.CompilerServices;

[Serializable]
public sealed class ReadOnlyCollectionBuilder<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList,
    ICollection
{
    private const int DefaultCapacity = 4;

    private static readonly T[] _emptyArray;

    private T[] _items;

    [NonSerialized] private object _syncRoot;

    private int _version;

    static ReadOnlyCollectionBuilder()
    {
        _emptyArray = new T[0];
    }

    public ReadOnlyCollectionBuilder()
    {
        _items = _emptyArray;
    }

    public ReadOnlyCollectionBuilder(int capacity)
    {
        ContractUtils.Requires(capacity >= 0, "capacity");
        _items = new T[capacity];
    }

    public ReadOnlyCollectionBuilder(IEnumerable<T> collection)
    {
        ContractUtils.Requires(collection != null, "collection");
        var ts = collection as ICollection<T>;
        if (ts != null)
        {
            var count = ts.Count;
            _items = new T[count];
            ts.CopyTo(_items, 0);
            Count = count;
            return;
        }

        Count = 0;
        _items = new T[4];
        foreach (var t in collection)
        {
            Add(t);
        }
    }

    public int Capacity
    {
        get => _items.Length;
        set
        {
            ContractUtils.Requires(value >= Count, "value");
            if (value != _items.Length)
            {
                if (value > 0)
                {
                    var tArray = new T[value];
                    if (Count > 0)
                    {
                        Array.Copy(_items, 0, tArray, 0, Count);
                    }

                    _items = tArray;
                    return;
                }

                _items = _emptyArray;
            }
        }
    }

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot
    {
        get
        {
            if (_syncRoot == null)
            {
                Net20Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
            }

            return _syncRoot;
        }
    }

    bool IList.IsFixedSize => false;

    bool IList.IsReadOnly => false;

    object IList.this[int index]
    {
        get => this[index];
        set
        {
            ValidateNullValue(value, "value");
            try
            {
                this[index] = (T)value;
            }
            catch (InvalidCastException invalidCastException)
            {
                ThrowInvalidTypeException(value, "value");
            }
        }
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ContractUtils.RequiresNotNull(array, "array");
        ContractUtils.Requires(array.Rank == 1, "array");
        Array.Copy(_items, 0, array, index, Count);
    }

    int IList.Add(object value)
    {
        ValidateNullValue(value, "value");
        try
        {
            Add((T)value);
        }
        catch (InvalidCastException invalidCastException)
        {
            ThrowInvalidTypeException(value, "value");
        }

        return Count - 1;
    }

    bool IList.Contains(object value)
    {
        if (!IsCompatibleObject(value))
        {
            return false;
        }

        return Contains((T)value);
    }

    int IList.IndexOf(object value)
    {
        if (!IsCompatibleObject(value))
        {
            return -1;
        }

        return IndexOf((T)value);
    }

    void IList.Insert(int index, object value)
    {
        ValidateNullValue(value, "value");
        try
        {
            Insert(index, (T)value);
        }
        catch (InvalidCastException invalidCastException)
        {
            ThrowInvalidTypeException(value, "value");
        }
    }

    void IList.Remove(object value)
    {
        if (IsCompatibleObject(value))
        {
            Remove((T)value);
        }
    }

    public int Count { get; private set; }

    public T this[int index]
    {
        get
        {
            ContractUtils.Requires(index < Count, "index");
            return _items[index];
        }
        set
        {
            ContractUtils.Requires(index < Count, "index");
            _items[index] = value;
            _version++;
        }
    }

    bool ICollection<T>.IsReadOnly => false;

    public void Add(T item)
    {
        if (Count == _items.Length)
        {
            EnsureCapacity(Count + 1);
        }

        var tArray = _items;
        var num = Count;
        Count = num + 1;
        tArray[num] = item;
        _version++;
    }

    public void Clear()
    {
        if (Count > 0)
        {
            Array.Clear(_items, 0, Count);
            Count = 0;
        }

        _version++;
    }

    public bool Contains(T item)
    {
        if (item == null)
        {
            for (var i = 0; i < Count; i++)
            {
                if (_items[i] == null)
                {
                    return true;
                }
            }

            return false;
        }

        var @default = EqualityComparer<T>.Default;
        for (var j = 0; j < Count; j++)
        {
            if (@default.Equals(_items[j], item))
            {
                return true;
            }
        }

        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(_items, 0, array, arrayIndex, Count);
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    public int IndexOf(T item)
    {
        return Array.IndexOf<T>(_items, item, 0, Count);
    }

    public void Insert(int index, T item)
    {
        ContractUtils.Requires(index <= Count, "index");
        if (Count == _items.Length)
        {
            EnsureCapacity(Count + 1);
        }

        if (index < Count)
        {
            Array.Copy(_items, index, _items, index + 1, Count - index);
        }

        _items[index] = item;
        Count++;
        _version++;
    }

    public bool Remove(T item)
    {
        var num = IndexOf(item);
        if (num < 0)
        {
            return false;
        }

        RemoveAt(num);
        return true;
    }

    public void RemoveAt(int index)
    {
        ContractUtils.Requires(index < 0 ? false : index < Count, "index");
        Count--;
        if (index < Count)
        {
            Array.Copy(_items, index + 1, _items, index, Count - index);
        }

        _items[Count] = default;
        _version++;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Reverse()
    {
        Reverse(0, Count);
    }

    public void Reverse(int index, int count)
    {
        ContractUtils.Requires(index >= 0, "index");
        ContractUtils.Requires(count >= 0, "count");
        Array.Reverse(_items, index, count);
        _version++;
    }

    public T[] ToArray()
    {
        var tArray = new T[Count];
        Array.Copy(_items, 0, tArray, 0, Count);
        return tArray;
    }

    public ReadOnlyCollection<T> ToReadOnlyCollection()
    {
        T[] tArray;
        tArray = Count != _items.Length ? ToArray() : _items;
        _items = _emptyArray;
        Count = 0;
        _version++;
        return new TrueReadOnlyCollection<T>(tArray);
    }

    private void EnsureCapacity(int min)
    {
        if (_items.Length < min)
        {
            var length = 4;
            if (_items.Length != 0)
            {
                length = _items.Length * 2;
            }

            if (length < min)
            {
                length = min;
            }

            Capacity = length;
        }
    }

    private static bool IsCompatibleObject(object value)
    {
        if (value is T)
        {
            return true;
        }

        if (value != null)
        {
            return false;
        }

        return default(T) == null;
    }

    private static void ThrowInvalidTypeException(object value, string argument)
    {
        object type;
        if (value != null)
        {
            type = value.GetType();
        }
        else
        {
            type = "null";
        }

        throw new ArgumentException(argument);
    }

    private static void ValidateNullValue(object value, string argument)
    {
        if (value == null)
        {
            if (default(T) != null)
            {
                throw new ArgumentException(argument);
            }
        }
    }

    [Serializable]
    private class Enumerator : IEnumerator<T>, IDisposable, IEnumerator
    {
        private readonly ReadOnlyCollectionBuilder<T> _builder;

        private readonly int _version;

        private int _index;

        internal Enumerator(ReadOnlyCollectionBuilder<T> builder)
        {
            _builder = builder;
            _version = builder._version;
            _index = 0;
            Current = default;
        }

        public T Current { get; private set; }

        object IEnumerator.Current
        {
            get
            {
                if (_index == 0 || _index > _builder.Count)
                {
                    throw new CompilerServicesException();
                }

                return Current;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public bool MoveNext()
        {
            if (_version != _builder._version)
            {
                throw new ArgumentException(nameof(_version));
            }

            if (_index >= _builder.Count)
            {
                _index = _builder.Count + 1;
                Current = default;
                return false;
            }

            var tArray = _builder._items;
            var num = _index;
            _index = num + 1;
            Current = tArray[num];
            return true;
        }

        void IEnumerator.Reset()
        {
            if (_version != _builder._version)
            {
                throw new ArgumentException(nameof(_version));
            }

            _index = 0;
            Current = default;
        }
    }
}