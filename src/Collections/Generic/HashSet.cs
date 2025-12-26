using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;

namespace System.Collections.Generic;

/// <summary>Represents a set of values.</summary>
[DebuggerDisplay("Count = {Count}")]
[Serializable]
[HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
public class HashSet<T> :
    ICollection<T>,
    ISerializable,
    IDeserializationCallback
{
    private const int Lower31BitMask = 2147483647;
    private const int GrowthFactor = 2;
    private const int StackAllocThreshold = 100;
    private const int ShrinkThreshold = 3;
    private const string CapacityName = "Capacity";
    private const string ElementsName = "Elements";
    private const string ComparerName = "Comparer";
    private const string VersionName = "Version";
    private int[]? _buckets;
    private int _freeList;
    private int _lastIndex;
    private SerializationInfo? _serializationInfo;
    private Slot[]? _slots;
    private int _version;

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1" /> class that is empty
    ///     and uses the default equality comparer for the set type.
    /// </summary>
    public HashSet()
        : this(EqualityComparer<T>.Default)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1" /> class that is empty
    ///     and uses the specified equality comparer for the set type.
    /// </summary>
    /// <param name="comparer">
    ///     The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> implementation to use when
    ///     comparing values in the set, or null to use the default
    ///     <see cref="T:System.Collections.Generic.EqualityComparer`1" /> implementation for the set type.
    /// </param>
    public HashSet(IEqualityComparer<T> comparer)
    {
        comparer ??= EqualityComparer<T>.Default;
        Comparer = comparer;
        _lastIndex = 0;
        Count = 0;
        _freeList = -1;
        _version = 0;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1" /> class that uses the
    ///     default equality comparer for the set type, contains elements copied from the specified collection, and has
    ///     sufficient capacity to accommodate the number of elements copied.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new set.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="collection" /> is null.
    /// </exception>
    public HashSet(IEnumerable<T> collection)
        : this(collection, EqualityComparer<T>.Default)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1" /> class that uses the
    ///     specified equality comparer for the set type, contains elements copied from the specified collection, and has
    ///     sufficient capacity to accommodate the number of elements copied.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new set.</param>
    /// <param name="comparer">
    ///     The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> implementation to use when
    ///     comparing values in the set, or null to use the default
    ///     <see cref="T:System.Collections.Generic.EqualityComparer`1" /> implementation for the set type.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="collection" /> is null.
    /// </exception>
    public HashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        : this(comparer)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        var capacity = 0;
        if (collection is ICollection<T> objs)
        {
            capacity = objs.Count;
        }

        Initialize(capacity);
        UnionWith(collection);
        if (_slots != null && (Count != 0 || _slots.Length <= HashHelpers.GetMinPrime()) &&
            (Count <= 0 || _slots.Length / Count <= 3))
        {
            return;
        }

        TrimExcess();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1" /> class with serialized
    ///     data.
    /// </summary>
    /// <param name="info">
    ///     A <see cref="T:System.Runtime.Serialization.SerializationInfo" /> object that contains the
    ///     information required to serialize the <see cref="T:System.Collections.Generic.HashSet`1" /> object.
    /// </param>
    /// <param name="context">
    ///     A <see cref="T:System.Runtime.Serialization.StreamingContext" /> structure that contains the
    ///     source and destination of the serialized stream associated with the
    ///     <see cref="T:System.Collections.Generic.HashSet`1" /> object.
    /// </param>
    protected HashSet(SerializationInfo info, StreamingContext context)
    {
        _serializationInfo = info;
    }

    /// <summary>
    ///     Gets the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> object that is used to determine
    ///     equality for the values in the set.
    /// </summary>
    /// <returns>
    ///     The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> object that is used to determine equality
    ///     for the values in the set.
    /// </returns>
    public IEqualityComparer<T>? Comparer { get; private set; }

    void ICollection<T>.Add(T item)
    {
        AddIfNotPresent(item);
    }

    /// <summary>Removes all elements from a <see cref="T:System.Collections.Generic.HashSet`1" /> object.</summary>
    public void Clear()
    {
        if (_lastIndex > 0)
        {
            if (_slots != null)
            {
                Array.Clear(_slots, 0, _lastIndex);
            }

            if (_buckets != null)
            {
                Array.Clear(_buckets, 0, _buckets.Length);
            }

            _lastIndex = 0;
            Count = 0;
            _freeList = -1;
        }

        ++_version;
    }

    /// <summary>
    ///     Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object contains the specified
    ///     element.
    /// </summary>
    /// <returns>
    ///     true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object contains the specified element;
    ///     otherwise, false.
    /// </returns>
    /// <param name="item">The element to locate in the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
    public bool Contains(T item)
    {
        if (_buckets != null)
        {
            var hashCode = InternalGetHashCode(item);
            if (_slots != null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                for (var index = _buckets[hashCode % _buckets.Length] - 1; index >= 0; index = _slots[index].next)
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                {
                    if (_slots != null && _slots[index].hashCode == hashCode &&
                        Comparer != null &&
                        _slots[index].value != null &&
#pragma warning disable CS8604 // Possible null reference argument.
                        Comparer.Equals(_slots[index].value, item))
#pragma warning restore CS8604 // Possible null reference argument.
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Copies the elements of a <see cref="T:System.Collections.Generic.HashSet`1" /> object to an array, starting at
    ///     the specified array index.
    /// </summary>
    /// <param name="array">
    ///     The one-dimensional array that is the destination of the elements copied from the
    ///     <see cref="T:System.Collections.Generic.HashSet`1" /> object. The array must have zero-based indexing.
    /// </param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="array" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///     <paramref name="arrayIndex" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="arrayIndex" /> is greater than the length of the destination <paramref name="array" />.-or-
    ///     <paramref name="count" /> is larger than the size of the destination <paramref name="array" />.
    /// </exception>
    public void CopyTo(T[] array, int arrayIndex)
    {
        CopyTo(array, arrayIndex, Count);
    }

    /// <summary>Removes the specified element from a <see cref="T:System.Collections.Generic.HashSet`1" /> object.</summary>
    /// <returns>
    ///     true if the element is successfully found and removed; otherwise, false.  This method returns false if
    ///     <paramref name="item" /> is not found in the <see cref="T:System.Collections.Generic.HashSet`1" /> object.
    /// </returns>
    /// <param name="item">The element to remove.</param>
    public bool Remove(T? item)
    {
        if (_buckets != null)
        {
            var hashCode = InternalGetHashCode(item);
            var index1 = hashCode % _buckets.Length;
            var index2 = -1;
            if (_slots != null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                for (var index3 = _buckets[index1] - 1; index3 >= 0; index3 = _slots[index3].next)
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                {
                    if (_slots != null && _slots[index3].hashCode == hashCode &&
                        Comparer != null &&
                        _slots[index3].value != null &&
#pragma warning disable CS8604 // Possible null reference argument.
                        Comparer.Equals(_slots[index3].value, item))
#pragma warning restore CS8604 // Possible null reference argument.
                    {
                        if (index2 < 0)
                        {
                            _buckets[index1] = _slots[index3].next + 1;
                        }
                        else
                        {
                            _slots[index2].next = _slots[index3].next;
                        }

                        _slots[index3].hashCode = -1;
                        _slots[index3].value = default;
                        _slots[index3].next = _freeList;
                        _freeList = index3;
                        --Count;
                        ++_version;
                        return true;
                    }

                    index2 = index3;
                }
            }
        }

        return false;
    }

    /// <summary>Gets the number of elements that are contained in a set.</summary>
    /// <returns>The number of elements that are contained in the set.</returns>
    public int Count { get; private set; }

    bool ICollection<T>.IsReadOnly => false;

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>Returns an enumerator that iterates through a collection.</summary>
    /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>
    ///     Implements the <see cref="T:System.Runtime.Serialization.ISerializable" /> interface and raises the
    ///     deserialization event when the deserialization is complete.
    /// </summary>
    /// <param name="sender">The source of the deserialization event.</param>
    /// <exception cref="T:System.Runtime.Serialization.SerializationException">
    ///     The
    ///     <see cref="T:System.Runtime.Serialization.SerializationInfo" /> object associated with the current
    ///     <see cref="T:System.Collections.Generic.HashSet`1" /> object is invalid.
    /// </exception>
    /// <filterpriority>2</filterpriority>
    public virtual void OnDeserialization(object sender)
    {
        if (_serializationInfo == null)
        {
            return;
        }

        var int32 = _serializationInfo.GetInt32("Capacity");
        Comparer = (IEqualityComparer<T>)_serializationInfo.GetValue("Comparer", typeof(IEqualityComparer<T>));
        _freeList = -1;
        if (int32 != 0)
        {
            _buckets = new int[int32];
            _slots = new Slot[int32];
            var objArray = (T[])_serializationInfo.GetValue("Elements", typeof(T[]));
            if (objArray == null)
            {
                throw new SerializationException(CoreStringResources.GetString("Serialization_MissingKeys"));
            }

            for (var index = 0; index < objArray.Length; ++index)
            {
                AddIfNotPresent(objArray[index]);
            }
        }
        else
        {
            _buckets = null;
        }

        _version = _serializationInfo.GetInt32("Version");
        _serializationInfo = null;
    }

    /// <summary>
    ///     Implements the <see cref="T:System.Runtime.Serialization.ISerializable" /> interface and returns the data
    ///     needed to serialize a <see cref="T:System.Collections.Generic.HashSet`1" /> object.
    /// </summary>
    /// <param name="info">
    ///     A <see cref="T:System.Runtime.Serialization.SerializationInfo" /> object that contains the
    ///     information required to serialize the <see cref="T:System.Collections.Generic.HashSet`1" /> object.
    /// </param>
    /// <param name="context">
    ///     A <see cref="T:System.Runtime.Serialization.StreamingContext" /> structure that contains the
    ///     source and destination of the serialized stream associated with the
    ///     <see cref="T:System.Collections.Generic.HashSet`1" /> object.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="info" /> is null.
    /// </exception>
    /// <filterpriority>2</filterpriority>
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        info.AddValue("Version", _version);
        info.AddValue("Comparer", Comparer, typeof(IEqualityComparer<T>));
        info.AddValue("Capacity", _buckets == null ? 0 : _buckets.Length);
        if (_buckets == null)
        {
            return;
        }

        var array = new T[Count];
        CopyTo(array);
        info.AddValue("Elements", array, typeof(T[]));
    }

    /// <summary>Adds the specified element to a set.</summary>
    /// <returns>
    ///     true if the element is added to the <see cref="T:System.Collections.Generic.HashSet`1" /> object; false if the
    ///     element is already present.
    /// </returns>
    /// <param name="item">The element to add to the set.</param>
    public bool Add(T item)
    {
        return AddIfNotPresent(item);
    }

    /// <summary>Copies the elements of a <see cref="T:System.Collections.Generic.HashSet`1" /> object to an array.</summary>
    /// <param name="array">
    ///     The one-dimensional array that is the destination of the elements copied from the
    ///     <see cref="T:System.Collections.Generic.HashSet`1" /> object. The array must have zero-based indexing.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="array" /> is null.
    /// </exception>
    public void CopyTo(T[] array)
    {
        CopyTo(array, 0, Count);
    }

    /// <summary>
    ///     Copies the specified number of elements of a <see cref="T:System.Collections.Generic.HashSet`1" /> object to
    ///     an array, starting at the specified array index.
    /// </summary>
    /// <param name="array">
    ///     The one-dimensional array that is the destination of the elements copied from the
    ///     <see cref="T:System.Collections.Generic.HashSet`1" /> object. The array must have zero-based indexing.
    /// </param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <param name="count">The number of elements to copy to <paramref name="array" />.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="array" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///     <paramref name="arrayIndex" /> is less than 0.-or-<paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="arrayIndex" /> is greater than the length of the destination <paramref name="array" />.-or-
    ///     <paramref name="count" /> is greater than the available space from the <paramref name="index" /> to the end of the
    ///     destination <paramref name="array" />.
    /// </exception>
    public void CopyTo(T?[] array, int arrayIndex, int count)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex),
                CoreStringResources.GetString("ArgumentOutOfRange_NeedNonNegNum"));
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count),
                CoreStringResources.GetString("ArgumentOutOfRange_NeedNonNegNum"));
        }

        if (arrayIndex > array.Length || count > array.Length - arrayIndex)
        {
            throw new ArgumentException(CoreStringResources.GetString("Arg_ArrayPlusOffTooSmall"));
        }

        var num = 0;
        for (var index = 0; index < _lastIndex && num < count; ++index)
        {
            if (_slots != null && _slots[index].hashCode >= 0)
            {
                array[arrayIndex + num] = _slots[index].value;
                ++num;
            }
        }
    }

    /// <summary>
    ///     Returns an <see cref="T:System.Collections.IEqualityComparer" /> object that can be used for deep equality
    ///     testing of a <see cref="T:System.Collections.Generic.HashSet`1" /> object.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.IEqualityComparer" /> object that can be used for deep equality testing of
    ///     the <see cref="T:System.Collections.Generic.HashSet`1" /> object.
    /// </returns>
    public static IEqualityComparer<HashSet<T>> CreateSetComparer()
    {
        return new HashSetEqualityComparer<T>();
    }

    /// <summary>
    ///     Removes all elements in the specified collection from the current
    ///     <see cref="T:System.Collections.Generic.HashSet`1" /> object.
    /// </summary>
    /// <param name="other">
    ///     The collection of items to remove from the <see cref="T:System.Collections.Generic.HashSet`1" />
    ///     object.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="other" /> is null.
    /// </exception>
    public void ExceptWith(IEnumerable<T> other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (Count == 0)
        {
            return;
        }

        if (ReferenceEquals(other, this))
        {
            Clear();
        }
        else
        {
            foreach (var obj in other)
            {
                Remove(obj);
            }
        }
    }

    /// <summary>Returns an enumerator that iterates through a <see cref="T:System.Collections.Generic.HashSet`1" /> object.</summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.Generic.HashSet`1.Enumerator" /> object for the
    ///     <see cref="T:System.Collections.Generic.HashSet`1" /> object.
    /// </returns>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <summary>
    ///     Modifies the current <see cref="T:System.Collections.Generic.HashSet`1" /> object to contain only elements
    ///     that are present in that object and in the specified collection.
    /// </summary>
    /// <param name="other">
    ///     The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" />
    ///     object.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="other" /> is null.
    /// </exception>
    [SecurityCritical]
    public void IntersectWith(IEnumerable<T> other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (Count == 0)
        {
            return;
        }

        if (other is ICollection<T> objs)
        {
            if (objs.Count == 0)
            {
                Clear();
                return;
            }

            if (other is HashSet<T> objSet && AreEqualityComparersEqual(this, objSet))
            {
                IntersectWithHashSetWithSameEC(objSet);
                return;
            }
        }

        IntersectWithEnumerable(other);
    }

    /// <summary>
    ///     Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object is a proper subset of the
    ///     specified collection.
    /// </summary>
    /// <returns>
    ///     true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object is a proper subset of
    ///     <paramref name="other" />; otherwise, false.
    /// </returns>
    /// <param name="other">
    ///     The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" />
    ///     object.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="other" /> is null.
    /// </exception>
    [SecurityCritical]
    public bool IsProperSubsetOf(IEnumerable<T> other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (other is ICollection<T> objs)
        {
            if (Count == 0)
            {
                return objs.Count > 0;
            }

            if (other is HashSet<T> objSet && AreEqualityComparersEqual(this, objSet))
            {
                return Count < objSet.Count && IsSubsetOfHashSetWithSameEC(objSet);
            }
        }

        var elementCount = CheckUniqueAndUnfoundElements(other, false);
        return elementCount.uniqueCount == Count && elementCount.unfoundCount > 0;
    }

    /// <summary>
    ///     Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object is a proper superset of the
    ///     specified collection.
    /// </summary>
    /// <returns>
    ///     true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object is a proper superset of
    ///     <paramref name="other" />; otherwise, false.
    /// </returns>
    /// <param name="other">
    ///     The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" />
    ///     object.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="other" /> is null.
    /// </exception>
    [SecurityCritical]
    public bool IsProperSupersetOf(IEnumerable<T> other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (Count == 0)
        {
            return false;
        }

        if (other is ICollection<T> objs)
        {
            if (objs.Count == 0)
            {
                return true;
            }

            if (other is HashSet<T> objSet && AreEqualityComparersEqual(this, objSet))
            {
                return objSet.Count < Count && ContainsAllElements(objSet);
            }
        }

        var elementCount = CheckUniqueAndUnfoundElements(other, true);
        return elementCount.uniqueCount < Count && elementCount.unfoundCount == 0;
    }

    /// <summary>
    ///     Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object is a subset of the specified
    ///     collection.
    /// </summary>
    /// <returns>
    ///     true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object is a subset of
    ///     <paramref name="other" />; otherwise, false.
    /// </returns>
    /// <param name="other">
    ///     The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" />
    ///     object.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="other" /> is null.
    /// </exception>
    [SecurityCritical]
    public bool IsSubsetOf(IEnumerable<T> other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (Count == 0)
        {
            return true;
        }

        if (other is HashSet<T> objSet && AreEqualityComparersEqual(this, objSet))
        {
            return Count <= objSet.Count && IsSubsetOfHashSetWithSameEC(objSet);
        }

        var elementCount = CheckUniqueAndUnfoundElements(other, false);
        return elementCount.uniqueCount == Count && elementCount.unfoundCount >= 0;
    }

    /// <summary>
    ///     Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object is a superset of the
    ///     specified collection.
    /// </summary>
    /// <returns>
    ///     true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object is a superset of
    ///     <paramref name="other" />; otherwise, false.
    /// </returns>
    /// <param name="other">
    ///     The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" />
    ///     object.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="other" /> is null.
    /// </exception>
    public bool IsSupersetOf(IEnumerable<T> other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (other is ICollection<T> objs)
        {
            if (objs.Count == 0)
            {
                return true;
            }

            if (other is HashSet<T> set2 && AreEqualityComparersEqual(this, set2) && set2.Count > Count)
            {
                return false;
            }
        }

        return ContainsAllElements(other);
    }

    /// <summary>
    ///     Determines whether the current <see cref="T:System.Collections.Generic.HashSet`1" /> object overlaps the
    ///     specified collection.
    /// </summary>
    /// <returns>
    ///     true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object and <paramref name="other" /> share
    ///     at least one common element; otherwise, false.
    /// </returns>
    /// <param name="other">
    ///     The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" />
    ///     object.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="other" /> is null.
    /// </exception>
    public bool Overlaps(IEnumerable<T> other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (Count == 0)
        {
            return false;
        }

        foreach (var obj in other)
        {
            if (Contains(obj))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Removes all elements that match the conditions defined by the specified predicate from a
    ///     <see cref="T:System.Collections.Generic.HashSet`1" /> collection.
    /// </summary>
    /// <returns>
    ///     The number of elements that were removed from the <see cref="T:System.Collections.Generic.HashSet`1" />
    ///     collection.
    /// </returns>
    /// <param name="match">
    ///     The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the elements to
    ///     remove.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="match" /> is null.
    /// </exception>
    public int RemoveWhere(Predicate<T> match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }

        var num = 0;
        for (var index = 0; index < _lastIndex; ++index)
        {
            if (_slots != null && _slots[index].hashCode >= 0)
            {
                var obj = _slots[index].value;
                if (obj != null && match(obj) && Remove(obj))
                {
                    ++num;
                }
            }
        }

        return num;
    }

    /// <summary>
    ///     Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object and the specified collection
    ///     contain the same elements.
    /// </summary>
    /// <returns>
    ///     true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object is equal to <paramref name="other" />
    ///     ; otherwise, false.
    /// </returns>
    /// <param name="other">
    ///     The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" />
    ///     object.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="other" /> is null.
    /// </exception>
    [SecurityCritical]
    public bool SetEquals(IEnumerable<T> other)
    {
        switch (other)
        {
            case null:
                throw new ArgumentNullException(nameof(other));
            case HashSet<T> objSet when AreEqualityComparersEqual(this, objSet):
                return Count == objSet.Count && ContainsAllElements(objSet);
            case ICollection<T> objs when Count == 0 && objs.Count > 0:
                return false;
            default:
                var elementCount = CheckUniqueAndUnfoundElements(other, true);
                return elementCount.uniqueCount == Count && elementCount.unfoundCount == 0;
        }
    }

    /// <summary>
    ///     Modifies the current <see cref="T:System.Collections.Generic.HashSet`1" /> object to contain only elements
    ///     that are present either in that object or in the specified collection, but not both.
    /// </summary>
    /// <param name="other">
    ///     The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" />
    ///     object.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="other" /> is null.
    /// </exception>
    [SecurityCritical]
    public void SymmetricExceptWith(IEnumerable<T> other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (Count == 0)
        {
            UnionWith(other);
        }
        else if (ReferenceEquals(other, this))
        {
            Clear();
        }
        else if (other is HashSet<T> objSet && AreEqualityComparersEqual(this, objSet))
        {
            SymmetricExceptWithUniqueHashSet(objSet);
        }
        else
        {
            SymmetricExceptWithEnumerable(other);
        }
    }

    /// <summary>
    ///     Sets the capacity of a <see cref="T:System.Collections.Generic.HashSet`1" /> object to the actual number of
    ///     elements it contains, rounded up to a nearby, implementation-specific value.
    /// </summary>
    public void TrimExcess()
    {
        if (Count == 0)
        {
            _buckets = null;
            _slots = null;
            ++_version;
        }
        else
        {
            var prime = HashHelpers.GetPrime(Count);
            var slotArray = new Slot[prime];
            var numArray = new int[prime];
            var index1 = 0;
            for (var index2 = 0; index2 < _lastIndex; ++index2)
            {
                if (_slots != null && _slots[index2].hashCode >= 0)
                {
                    slotArray[index1] = _slots[index2];
                    var index3 = slotArray[index1].hashCode % prime;
                    slotArray[index1].next = numArray[index3] - 1;
                    numArray[index3] = index1 + 1;
                    ++index1;
                }
            }

            _lastIndex = index1;
            _slots = slotArray;
            _buckets = numArray;
            _freeList = -1;
        }
    }

    /// <summary>
    ///     Modifies the current <see cref="T:System.Collections.Generic.HashSet`1" /> object to contain all elements that
    ///     are present in both itself and in the specified collection.
    /// </summary>
    /// <param name="other">
    ///     The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" />
    ///     object.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="other" /> is null.
    /// </exception>
    public void UnionWith(IEnumerable<T> other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        foreach (var obj in other)
        {
            AddIfNotPresent(obj);
        }
    }

    internal static bool HashSetEquals(
        HashSet<T> set1,
        HashSet<T> set2,
        IEqualityComparer<T> comparer)
    {
        if (set1 == null)
        {
            return set2 == null;
        }

        if (set2 == null)
        {
            return false;
        }

        if (AreEqualityComparersEqual(set1, set2))
        {
            if (set1.Count != set2.Count)
            {
                return false;
            }

            foreach (var obj in set2)
            {
                if (obj != null && !set1.Contains(obj))
                {
                    return false;
                }
            }

            return true;
        }

        foreach (var x in set2)
        {
            var flag = false;
            foreach (var y in set1)
            {
#pragma warning disable CS8604 // Possible null reference argument.
                if (comparer.Equals(x, y))
#pragma warning restore CS8604 // Possible null reference argument.
                {
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                return false;
            }
        }

        return true;
    }

    internal T[] ToArray()
    {
        var array = new T[Count];
        CopyTo(array);
        return array;
    }

    private bool AddIfNotPresent(T value)
    {
        if (_buckets == null)
        {
            Initialize(0);
        }

        var hashCode = InternalGetHashCode(value);
        if (_buckets != null)
        {
            var index1 = hashCode % _buckets.Length;
            if (_slots != null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                for (var index2 = _buckets[hashCode % _buckets.Length] - 1; index2 >= 0; index2 = _slots[index2].next)
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                {
                    if (_slots[index2].hashCode == hashCode && Comparer != null && _slots[index2].value != null &&
#pragma warning disable CS8604 // Possible null reference argument.
                        Comparer.Equals(_slots[index2].value, value))
#pragma warning restore CS8604 // Possible null reference argument.
                    {
                        return false;
                    }
                }

                int index3;
                if (_freeList >= 0)
                {
                    index3 = _freeList;
                    if (_slots != null)
                    {
                        _freeList = _slots[index3].next;
                    }
                }
                else
                {
                    if (_lastIndex == _slots?.Length)
                    {
                        IncreaseCapacity();
                        index1 = hashCode % _buckets.Length;
                    }

                    index3 = _lastIndex;
                    ++_lastIndex;
                }

                if (_slots != null)
                {
                    _slots[index3].hashCode = hashCode;
                    _slots[index3].value = value;
                    _slots[index3].next = _buckets[index1] - 1;
                }

                _buckets[index1] = index3 + 1;
            }
        }

        ++Count;
        ++_version;
        return true;
    }

    private bool AddOrGetLocation(T value, out int location)
    {
        var hashCode = InternalGetHashCode(value);
        location = 0;
        if (_buckets != null)
        {
            var index1 = hashCode % _buckets.Length;
            if (_slots != null)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                for (var index2 = _buckets[hashCode % _buckets.Length] - 1; index2 >= 0; index2 = _slots[index2].next)
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                {
                    if (_slots[index2].hashCode == hashCode && Comparer != null &&
                        _slots[index2].value != null &&
#pragma warning disable CS8604 // Possible null reference argument.
                        Comparer.Equals(_slots[index2].value, value))
#pragma warning restore CS8604 // Possible null reference argument.
                    {
                        location = index2;
                        return false;
                    }
                }

                int index3;
                if (_freeList >= 0)
                {
                    index3 = _freeList;
                    _freeList = _slots[index3].next;
                }
                else
                {
                    if (_lastIndex == _slots.Length)
                    {
                        IncreaseCapacity();
                        index1 = hashCode % _buckets.Length;
                    }

                    index3 = _lastIndex;
                    ++_lastIndex;
                }

                _slots[index3].hashCode = hashCode;
                _slots[index3].value = value;
                _slots[index3].next = _buckets[index1] - 1;
                _buckets[index1] = index3 + 1;
                ++Count;
                ++_version;
                location = index3;
            }
        }

        return true;
    }

    private static bool AreEqualityComparersEqual(HashSet<T> set1, HashSet<T> set2)
    {
        return set1.Comparer != null && set1.Comparer.Equals(set2.Comparer);
    }

    [SecurityCritical]
    private ElementCount CheckUniqueAndUnfoundElements(
        IEnumerable<T> other,
        bool returnIfUnfound)
    {
        if (Count == 0)
        {
            var num = 0;
            using (var enumerator = other.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    ++num;
                }
            }

            ElementCount elementCount;
            elementCount.uniqueCount = 0;
            elementCount.unfoundCount = num;
            return elementCount;
        }

        var intArrayLength = BitHelper.ToIntArrayLength(_lastIndex);
        // ISSUE: untyped stack allocation
        var bitHelper = intArrayLength > 100
            ? new BitHelper(new int[intArrayLength], intArrayLength)
            : new BitHelper(new int[4 * intArrayLength], intArrayLength);
        var num1 = 0;
        var num2 = 0;
        foreach (var obj in other)
        {
            var bitPosition = InternalIndexOf(obj);
            if (bitPosition >= 0)
            {
                if (!bitHelper.IsMarked(bitPosition))
                {
                    bitHelper.MarkBit(bitPosition);
                    ++num2;
                }
            }
            else
            {
                ++num1;
                if (returnIfUnfound)
                {
                    break;
                }
            }
        }

        ElementCount elementCount1;
        elementCount1.uniqueCount = num2;
        elementCount1.unfoundCount = num1;
        return elementCount1;
    }

    private bool ContainsAllElements(IEnumerable<T> other)
    {
        foreach (var obj in other)
        {
            if (!Contains(obj))
            {
                return false;
            }
        }

        return true;
    }

    private void IncreaseCapacity()
    {
        var min = Count * 2;
        if (min < 0)
        {
            min = Count;
        }

        var prime = HashHelpers.GetPrime(min);
        var destinationArray = prime > Count
            ? new Slot[prime]
            : throw new ArgumentException(CoreStringResources.GetString("Arg_HSCapacityOverflow"));
        if (_slots != null)
        {
            Array.Copy(_slots, 0, destinationArray, 0, _lastIndex);
        }

        var numArray = new int[prime];
        for (var index1 = 0; index1 < _lastIndex; ++index1)
        {
            var index2 = destinationArray[index1].hashCode % prime;
            destinationArray[index1].next = numArray[index2] - 1;
            numArray[index2] = index1 + 1;
        }

        _slots = destinationArray;
        _buckets = numArray;
    }

    private void Initialize(int capacity)
    {
        var prime = HashHelpers.GetPrime(capacity);
        _buckets = new int[prime];
        _slots = new Slot[prime];
    }

    private int InternalGetHashCode(T? item)
    {
        if (Comparer != null)
        {
            return item == null ? 0 : Comparer.GetHashCode(item) & int.MaxValue;
        }

        return 0;
    }

    private int InternalIndexOf(T item)
    {
        var hashCode = InternalGetHashCode(item);
        if (_buckets != null && _slots != null)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            for (var index = _buckets[hashCode % _buckets.Length] - 1; index >= 0; index = _slots[index].next)
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            {
                if (_slots[index].hashCode == hashCode && Comparer != null &&
#pragma warning disable CS8604 // Possible null reference argument.
                    _slots[index].value != null && Comparer.Equals(_slots[index].value, item))
#pragma warning restore CS8604 // Possible null reference argument.
                {
                    return index;
                }
            }
        }

        return -1;
    }

    [SecurityCritical]
    private void IntersectWithEnumerable(IEnumerable<T> other)
    {
        var lastIndex = _lastIndex;
        var intArrayLength = BitHelper.ToIntArrayLength(lastIndex);
        // ISSUE: untyped stack allocation
        var bitHelper = intArrayLength > 100
            ? new BitHelper(new int[intArrayLength], intArrayLength)
            : new BitHelper(new int[4 * intArrayLength], intArrayLength);
        foreach (var obj in other)
        {
            var bitPosition = InternalIndexOf(obj);
            if (bitPosition >= 0)
            {
                bitHelper.MarkBit(bitPosition);
            }
        }

        for (var bitPosition = 0; bitPosition < lastIndex; ++bitPosition)
        {
            if (_slots != null && _slots[bitPosition].hashCode >= 0 && !bitHelper.IsMarked(bitPosition))
            {
                Remove(_slots[bitPosition].value);
            }
        }
    }

    private void IntersectWithHashSetWithSameEC(HashSet<T> other)
    {
        for (var index = 0; index < _lastIndex; ++index)
        {
            if (_slots != null && _slots[index].hashCode >= 0)
            {
                var obj = _slots[index].value;
                if (obj != null && !other.Contains(obj))
                {
                    Remove(obj);
                }
            }
        }
    }

    private bool IsSubsetOfHashSetWithSameEC(HashSet<T> other)
    {
        foreach (var obj in this)
        {
            if (obj != null && !other.Contains(obj))
            {
                return false;
            }
        }

        return true;
    }

    [SecurityCritical]
    private void SymmetricExceptWithEnumerable(IEnumerable<T> other)
    {
        var lastIndex = _lastIndex;
        var intArrayLength = BitHelper.ToIntArrayLength(lastIndex);
        BitHelper bitHelper1;
        BitHelper bitHelper2;
        if (intArrayLength <= 50)
        {
            // ISSUE: untyped stack allocation
            bitHelper1 = new BitHelper(new int[4 * intArrayLength], intArrayLength);
            // ISSUE: untyped stack allocation
            bitHelper2 = new BitHelper(new int[4 * intArrayLength], intArrayLength);
        }
        else
        {
            bitHelper1 = new BitHelper(new int[intArrayLength], intArrayLength);
            bitHelper2 = new BitHelper(new int[intArrayLength], intArrayLength);
        }

        foreach (var obj in other)
        {
            var location = 0;
            if (AddOrGetLocation(obj, out location))
            {
                bitHelper2.MarkBit(location);
            }
            else if (location < lastIndex && !bitHelper2.IsMarked(location))
            {
                bitHelper1.MarkBit(location);
            }
        }

        for (var bitPosition = 0; bitPosition < lastIndex; ++bitPosition)
        {
            if (bitHelper1.IsMarked(bitPosition))
            {
                if (_slots != null)
                {
                    Remove(_slots[bitPosition].value);
                }
            }
        }
    }

    private void SymmetricExceptWithUniqueHashSet(HashSet<T> other)
    {
        foreach (var obj in other)
        {
            if (!Remove(obj))
            {
                if (obj != null)
                {
                    AddIfNotPresent(obj);
                }
            }
        }
    }

    internal struct ElementCount
    {
        internal int uniqueCount;
        internal int unfoundCount;
    }

    internal struct Slot
    {
        internal int hashCode;
        internal T? value;
        internal int next;
    }

    /// <summary>Enumerates the elements of a <see cref="T:System.Collections.Generic.HashSet`1" /> object.</summary>
    /// <filterpriority>2</filterpriority>
    [Serializable]
    [HostProtection(SecurityAction.LinkDemand, MayLeakOnAbort = true)]
    public struct Enumerator : IEnumerator<T>
    {
        private HashSet<T> set;
        private int index;
        private int version;

        internal Enumerator(HashSet<T> set)
        {
            this.set = set;
            index = 0;
            version = set._version;
            Current = default;
        }

        /// <summary>Releases all resources used by a <see cref="T:System.Collections.Generic.HashSet`1.Enumerator" /> object.</summary>
        public void Dispose()
        {
        }

        /// <summary>
        ///     Advances the enumerator to the next element of the <see cref="T:System.Collections.Generic.HashSet`1" />
        ///     collection.
        /// </summary>
        /// <returns>
        ///     true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the
        ///     end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
        public bool MoveNext()
        {
            if (version != set._version)
            {
                throw new InvalidOperationException(
                    CoreStringResources.GetString("InvalidOperation_EnumFailedVersion"));
            }

            for (; index < set._lastIndex; ++index)
            {
                if (set._slots != null && set._slots[index].hashCode >= 0)
                {
                    Current = set._slots[index].value;
                    ++index;
                    return true;
                }
            }

            index = set._lastIndex + 1;
            Current = default;
            return false;
        }

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        /// <returns>
        ///     The element in the <see cref="T:System.Collections.Generic.HashSet`1" /> collection at the current position of
        ///     the enumerator.
        /// </returns>
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
        public T? Current { get; private set; }
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        /// <returns>The element in the collection at the current position of the enumerator, as an <see cref="T:System.Object" />.</returns>
        /// <exception cref="T:System.InvalidOperationException">
        ///     The enumerator is positioned before the first element of the
        ///     collection or after the last element.
        /// </exception>
        object? IEnumerator.Current
        {
            get
            {
                if (index == 0 || index == set._lastIndex + 1)
                {
                    throw new InvalidOperationException(
                        CoreStringResources.GetString("InvalidOperation_EnumOpCantHappen"));
                }

                return Current;
            }
        }

        /// <summary>Sets the enumerator to its initial position, which is before the first element in the collection.</summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
        void IEnumerator.Reset()
        {
            if (version != set._version)
            {
                throw new InvalidOperationException(
                    CoreStringResources.GetString("InvalidOperation_EnumFailedVersion"));
            }

            index = 0;
            Current = default;
        }
    }
}