using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace System.Runtime.CompilerServices;

[Serializable]
public sealed class ReadOnlyCollectionBuilder<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable, IList, ICollection
{
    private const int DefaultCapacity = 4;

    private T[] _items;

    private int _size;

    private int _version;

    [NonSerialized]
    private object _syncRoot;

    private readonly static T[] _emptyArray;

    public int Capacity
    {
        get => _items.Length;
        set
        {
            ContractUtils.Requires(value >= _size, "value");
            if (value != _items.Length)
            {
                if (value > 0)
                {
                    var tArray = new T[value];
                    if (_size > 0)
                    {
                        Array.Copy(_items, 0, tArray, 0, _size);
                    }
                    _items = tArray;
                    return;
                }
                _items = _emptyArray;
            }
        }
    }

    public int Count => _size;

    public T this[int index]
    {
        get
        {
            ContractUtils.Requires(index < _size, "index");
            return _items[index];
        }
        set
        {
            ContractUtils.Requires(index < _size, "index");
            _items[index] = value;
            _version++;
        }
    }

    bool ICollection<T>.IsReadOnly => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot
    {
        get
        {
            if (_syncRoot == null)
            {
                Threading.Net20Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
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
            _size = count;
            return;
        }
        _size = 0;
        _items = new T[4];
        foreach (var t in collection)
        {
            Add(t);
        }
    }

    public void Add(T item)
    {
        if (_size == _items.Length)
        {
            EnsureCapacity(_size + 1);
        }
        var tArray = _items;
        var num = _size;
        _size = num + 1;
        tArray[num] = item;
        _version++;
    }

    public void Clear()
    {
        if (_size > 0)
        {
            Array.Clear(_items, 0, _size);
            _size = 0;
        }
        _version++;
    }

    public bool Contains(T item)
    {
        if (item == null)
        {
            for (var i = 0; i < _size; i++)
            {
                if (_items[i] == null)
                {
                    return true;
                }
            }
            return false;
        }
        var @default = EqualityComparer<T>.Default;
        for (var j = 0; j < _size; j++)
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
        Array.Copy(_items, 0, array, arrayIndex, _size);
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

    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    public int IndexOf(T item)
    {
        return Array.IndexOf<T>(_items, item, 0, _size);
    }

    public void Insert(int index, T item)
    {
        ContractUtils.Requires(index <= _size, "index");
        if (_size == _items.Length)
        {
            EnsureCapacity(_size + 1);
        }
        if (index < _size)
        {
            Array.Copy(_items, index, _items, index + 1, _size - index);
        }
        _items[index] = item;
        _size++;
        _version++;
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
        ContractUtils.Requires((index < 0 ? false : index < _size), "index");
        _size--;
        if (index < _size)
        {
            Array.Copy(_items, index + 1, _items, index, _size - index);
        }
        _items[_size] = default(T);
        _version++;
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

    void ICollection.CopyTo(Array array, int index)
    {
        ContractUtils.RequiresNotNull(array, "array");
        ContractUtils.Requires(array.Rank == 1, "array");
        Array.Copy(_items, 0, array, index, _size);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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

    public T[] ToArray()
    {
        var tArray = new T[_size];
        Array.Copy(_items, 0, tArray, 0, _size);
        return tArray;
    }

    public ReadOnlyCollection<T> ToReadOnlyCollection()
    {
        T[] tArray;
        tArray = (_size != _items.Length ? ToArray() : _items);
        _items = _emptyArray;
        _size = 0;
        _version++;
        return new TrueReadOnlyCollection<T>(tArray);
    }

    private static void ValidateNullValue(object value, string argument)
    {
        if (value == null)
        {
            if (default(T) != null)
            {
                throw new ArgumentException( argument);
            }
        }
    }

    [Serializable]
    private class Enumerator : IEnumerator<T>, IDisposable, IEnumerator
    {
        private readonly ReadOnlyCollectionBuilder<T> _builder;

        private readonly int _version;

        private int _index;

        private T _current;

        public T Current => _current;

        object IEnumerator.Current
        {
            get
            {
                if (_index == 0 || _index > _builder._size)
                {
                    throw new CompilerServicesException();
                }
                return _current;
            }
        }

        internal Enumerator(ReadOnlyCollectionBuilder<T> builder)
        {
            _builder = builder;
            _version = builder._version;
            _index = 0;
            _current = default(T);
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
            if (_index >= _builder._size)
            {
                _index = _builder._size + 1;
                _current = default(T);
                return false;
            }
            var tArray = _builder._items;
            var num = _index;
            _index = num + 1;
            _current = tArray[num];
            return true;
        }

        void IEnumerator.Reset()
        {
            if (_version != _builder._version)
            {
                throw new ArgumentException(nameof(_version));
            }
            _index = 0;
            _current = default(T);
        }
    }
}