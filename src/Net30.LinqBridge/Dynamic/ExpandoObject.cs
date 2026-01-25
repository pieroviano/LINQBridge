#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace System.Dynamic;

/// <summary>Represents an object whose members can be dynamically added and removed at run time.</summary>
public sealed class ExpandoObject :
    IDynamicMetaObjectProvider,
    IDictionary<string, object>,
    ICollection<KeyValuePair<string, object>>,
    IEnumerable<KeyValuePair<string, object>>,
    IEnumerable,
    INotifyPropertyChanged
{
    internal const int AmbiguousMatchFound = -2;
    internal const int NoMatch = -1;
    internal static readonly object Uninitialized = new();
    internal readonly object LockObject;
    private int _count;
    private ExpandoData _data;
    private PropertyChangedEventHandler _propertyChanged;

    /// <summary>Initializes a new ExpandoObject that does not have members.</summary>
    public ExpandoObject()
    {
        _data = ExpandoData.Empty;
        LockObject = new object();
    }

    internal ExpandoClass Class => _data.Class;

    ICollection<string> IDictionary<string, object>.Keys => new KeyCollection(this);

    ICollection<object> IDictionary<string, object>.Values => new ValueCollection(this);

    object IDictionary<string, object>.this[string key]
    {
        get
        {
            object obj;
            if (!TryGetValueForKey(key, out obj))
            {
                throw Error.KeyDoesNotExistInExpando(key);
            }

            return obj;
        }
        set
        {
            ContractUtils.RequiresNotNull(key, nameof(key));
            TrySetValue(null, -1, value, key, false, false);
        }
    }

    void IDictionary<string, object>.Add(string key, object value)
    {
        TryAddMember(key, value);
    }

    bool IDictionary<string, object>.ContainsKey(string key)
    {
        ContractUtils.RequiresNotNull(key, nameof(key));
        var data = _data;
        var indexCaseSensitive = data.Class.GetValueIndexCaseSensitive(key);
        return indexCaseSensitive >= 0 && data[indexCaseSensitive] != Uninitialized;
    }

    bool IDictionary<string, object>.Remove(string key)
    {
        ContractUtils.RequiresNotNull(key, nameof(key));
        return TryDeleteValue(null, -1, key, false, Uninitialized);
    }

    bool IDictionary<string, object>.TryGetValue(string key, out object value)
    {
        return TryGetValueForKey(key, out value);
    }

    int ICollection<KeyValuePair<string, object>>.Count => _count;

    bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

    void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
    {
        TryAddMember(item.Key, item.Value);
    }

    void ICollection<KeyValuePair<string, object>>.Clear()
    {
        ExpandoData data;
        lock (LockObject)
        {
            data = _data;
            _data = ExpandoData.Empty;
            _count = 0;
        }

        var propertyChanged = _propertyChanged;
        if (propertyChanged == null)
        {
            return;
        }

        var index = 0;
        for (var length = data.Class.Keys.Length; index < length; ++index)
        {
            if (data[index] != Uninitialized)
            {
                propertyChanged(this, new PropertyChangedEventArgs(data.Class.Keys[index]));
            }
        }
    }

    bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
    {
        object objA;
        return TryGetValueForKey(item.Key, out objA) && Equals(objA, item.Value);
    }

    void ICollection<KeyValuePair<string, object>>.CopyTo(
        KeyValuePair<string, object>[] array,
        int arrayIndex)
    {
        ContractUtils.RequiresNotNull(array, nameof(array));
        ContractUtils.RequiresArrayRange(array, arrayIndex, _count, nameof(arrayIndex), "Count");
        lock (LockObject)
        {
            foreach (var keyValuePair in this)
            {
                array[arrayIndex++] = keyValuePair;
            }
        }
    }

    bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
    {
        return TryDeleteValue(null, -1, item.Key, false, item.Value);
    }

    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
    {
        var data = _data;
        return GetExpandoEnumerator(data, data.Version);
    }

    /// <summary>Returns an enumerator that iterates through the collection.</summary>
    /// <returns>An <see cref="T:System.Collections.IEnumerator" /> that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        var data = _data;
        return GetExpandoEnumerator(data, data.Version);
    }

    /// <summary>
    ///     The provided MetaObject will dispatch to the dynamic virtual methods. The object can be encapsulated inside
    ///     another MetaObject to provide custom behavior for individual actions.
    /// </summary>
    /// <returns>The object of the <see cref="T:System.Dynamic.DynamicMetaObject" /> type.</returns>
    /// <param name="parameter">The expression that represents the MetaObject to dispatch to the Dynamic virtual methods.</param>
    DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
    {
        return new MetaExpando(parameter, this);
    }

    /// <summary>Occurs when a property value changes.</summary>
    event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
    {
        add => _propertyChanged += value;
        remove => _propertyChanged -= value;
    }

    internal bool IsDeletedMember(int index)
    {
        return index != _data.Length && _data[index] == Uninitialized;
    }

    internal void PromoteClass(object oldClass, object newClass)
    {
        PromoteClassCore((ExpandoClass)oldClass, (ExpandoClass)newClass);
    }

    internal bool TryDeleteValue(
        object indexClass,
        int index,
        string name,
        bool ignoreCase,
        object deleteValue)
    {
        ExpandoData data;
        lock (LockObject)
        {
            data = _data;
            if ((data.Class != indexClass) | ignoreCase)
            {
                index = data.Class.GetValueIndex(name, ignoreCase, this);
                if (index == -2)
                {
                    throw Error.AmbiguousMatchInExpandoObject(name);
                }
            }

            if (index == -1)
            {
                return false;
            }

            var objA = data[index];
            if (objA == Uninitialized || (deleteValue != Uninitialized && !Equals(objA, deleteValue)))
            {
                return false;
            }

            data[index] = Uninitialized;
            --_count;
        }

        var propertyChanged = _propertyChanged;
        if (propertyChanged != null)
        {
            propertyChanged(this, new PropertyChangedEventArgs(data.Class.Keys[index]));
        }

        return true;
    }

    internal bool TryGetValue(
        object indexClass,
        int index,
        string name,
        bool ignoreCase,
        out object value)
    {
        var data = _data;
        if ((data.Class != indexClass) | ignoreCase)
        {
            index = data.Class.GetValueIndex(name, ignoreCase, this);
            if (index == -2)
            {
                throw Error.AmbiguousMatchInExpandoObject(name);
            }
        }

        if (index == -1)
        {
            value = null;
            return false;
        }

        var obj = data[index];
        if (obj == Uninitialized)
        {
            value = null;
            return false;
        }

        value = obj;
        return true;
    }

    internal void TrySetValue(
        object indexClass,
        int index,
        object value,
        string name,
        bool ignoreCase,
        bool add)
    {
        ExpandoData expandoData;
        object obj;
        lock (LockObject)
        {
            expandoData = _data;
            if ((expandoData.Class != indexClass) | ignoreCase)
            {
                index = expandoData.Class.GetValueIndex(name, ignoreCase, this);
                if (index == -2)
                {
                    throw Error.AmbiguousMatchInExpandoObject(name);
                }

                if (index == -1)
                {
                    var num = ignoreCase ? expandoData.Class.GetValueIndexCaseSensitive(name) : index;
                    if (num != -1)
                    {
                        index = num;
                    }
                    else
                    {
                        var newClass = expandoData.Class.FindNewClass(name);
                        expandoData = PromoteClassCore(expandoData.Class, newClass);
                        index = expandoData.Class.GetValueIndexCaseSensitive(name);
                    }
                }
            }

            obj = expandoData[index];
            if (obj == Uninitialized)
            {
                ++_count;
            }
            else if (add)
            {
                throw Error.SameKeyExistsInExpando(name);
            }

            expandoData[index] = value;
        }

        var propertyChanged = _propertyChanged;
        if (propertyChanged == null || value == obj)
        {
            return;
        }

        propertyChanged(this, new PropertyChangedEventArgs(expandoData.Class.Keys[index]));
    }

    private bool ExpandoContainsKey(string key)
    {
        return _data.Class.GetValueIndexCaseSensitive(key) >= 0;
    }

    private IEnumerator<KeyValuePair<string, object>> GetExpandoEnumerator(
        ExpandoData data,
        int version)
    {
        for (var i = 0; i < data.Class.Keys.Length; ++i)
        {
            if (_data.Version != version || data != _data)
            {
                throw Error.CollectionModifiedWhileEnumerating();
            }

            var obj = data[i];
            if (obj != Uninitialized)
            {
                yield return new KeyValuePair<string, object>(data.Class.Keys[i], obj);
            }
        }
    }

    private ExpandoData PromoteClassCore(ExpandoClass oldClass, ExpandoClass newClass)
    {
        lock (LockObject)
        {
            if (_data.Class == oldClass)
            {
                _data = _data.UpdateClass(newClass);
            }

            return _data;
        }
    }

    private void TryAddMember(string key, object value)
    {
        ContractUtils.RequiresNotNull(key, nameof(key));
        TrySetValue(null, -1, value, key, false, true);
    }

    private bool TryGetValueForKey(string key, out object value)
    {
        return TryGetValue(null, -1, key, false, out value);
    }

    private sealed class KeyCollectionDebugView
    {
        private readonly ICollection<string> collection;

        public KeyCollectionDebugView(ICollection<string> collection)
        {
            this.collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public string[] Items
        {
            get
            {
                var array = new string[collection.Count];
                collection.CopyTo(array, 0);
                return array;
            }
        }
    }

    [DebuggerTypeProxy(typeof(KeyCollectionDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    private class KeyCollection : ICollection<string>, IEnumerable<string>, IEnumerable
    {
        private readonly ExpandoObject _expando;
        private readonly int _expandoCount;
        private readonly ExpandoData _expandoData;
        private readonly int _expandoVersion;

        internal KeyCollection(ExpandoObject expando)
        {
            lock (expando.LockObject)
            {
                _expando = expando;
                _expandoVersion = expando._data.Version;
                _expandoCount = expando._count;
                _expandoData = expando._data;
            }
        }

        public void Add(string item)
        {
            throw Error.CollectionReadOnly();
        }

        public void Clear()
        {
            throw Error.CollectionReadOnly();
        }

        public bool Contains(string item)
        {
            lock (_expando.LockObject)
            {
                CheckVersion();
                return _expando.ExpandoContainsKey(item);
            }
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            ContractUtils.RequiresNotNull(array, nameof(array));
            ContractUtils.RequiresArrayRange(array, arrayIndex, _expandoCount, nameof(arrayIndex), "Count");
            lock (_expando.LockObject)
            {
                CheckVersion();
                var data = _expando._data;
                for (var index = 0; index < data.Class.Keys.Length; ++index)
                {
                    if (data[index] != Uninitialized)
                    {
                        array[arrayIndex++] = data.Class.Keys[index];
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                CheckVersion();
                return _expandoCount;
            }
        }

        public bool IsReadOnly => true;

        public bool Remove(string item)
        {
            throw Error.CollectionReadOnly();
        }

        public IEnumerator<string> GetEnumerator()
        {
            var i = 0;
            for (var n = _expandoData.Class.Keys.Length; i < n; ++i)
            {
                CheckVersion();
                if (_expandoData[i] != Uninitialized)
                {
                    yield return _expandoData.Class.Keys[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void CheckVersion()
        {
            if (_expando._data.Version != _expandoVersion || _expandoData != _expando._data)
            {
                throw Error.CollectionModifiedWhileEnumerating();
            }
        }
    }

    private sealed class ValueCollectionDebugView
    {
        private readonly ICollection<object> collection;

        public ValueCollectionDebugView(ICollection<object> collection)
        {
            this.collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object[] Items
        {
            get
            {
                var array = new object[collection.Count];
                collection.CopyTo(array, 0);
                return array;
            }
        }
    }

    [DebuggerTypeProxy(typeof(ValueCollectionDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    private class ValueCollection : ICollection<object>, IEnumerable<object>, IEnumerable
    {
        private readonly ExpandoObject _expando;
        private readonly int _expandoCount;
        private readonly ExpandoData _expandoData;
        private readonly int _expandoVersion;

        internal ValueCollection(ExpandoObject expando)
        {
            lock (expando.LockObject)
            {
                _expando = expando;
                _expandoVersion = expando._data.Version;
                _expandoCount = expando._count;
                _expandoData = expando._data;
            }
        }

        public void Add(object item)
        {
            throw Error.CollectionReadOnly();
        }

        public void Clear()
        {
            throw Error.CollectionReadOnly();
        }

        public bool Contains(object item)
        {
            lock (_expando.LockObject)
            {
                CheckVersion();
                var data = _expando._data;
                for (var index = 0; index < data.Class.Keys.Length; ++index)
                {
                    if (Equals(data[index], item))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            ContractUtils.RequiresNotNull(array, nameof(array));
            ContractUtils.RequiresArrayRange(array, arrayIndex, _expandoCount, nameof(arrayIndex), "Count");
            lock (_expando.LockObject)
            {
                CheckVersion();
                var data = _expando._data;
                for (var index = 0; index < data.Class.Keys.Length; ++index)
                {
                    if (data[index] != Uninitialized)
                    {
                        array[arrayIndex++] = data[index];
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                CheckVersion();
                return _expandoCount;
            }
        }

        public bool IsReadOnly => true;

        public bool Remove(object item)
        {
            throw Error.CollectionReadOnly();
        }

        public IEnumerator<object> GetEnumerator()
        {
            var data = _expando._data;
            for (var i = 0; i < data.Class.Keys.Length; ++i)
            {
                CheckVersion();
                var obj = data[i];
                if (obj != Uninitialized)
                {
                    yield return obj;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void CheckVersion()
        {
            if (_expando._data.Version != _expandoVersion || _expandoData != _expando._data)
            {
                throw Error.CollectionModifiedWhileEnumerating();
            }
        }
    }

    private class MetaExpando(Expression expression, ExpandoObject value)
        : DynamicMetaObject(expression, BindingRestrictions.Empty, value)
    {
        public new ExpandoObject Value => (ExpandoObject)base.Value;

        public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            var valueIndex = Value.Class.GetValueIndex(binder.Name, binder.IgnoreCase, Value);
            var expression = (Expression)Expression.Call(typeof(RuntimeOps).GetMethod("ExpandoTryDeleteValue"),
                GetLimitedSelf(), Expression.Constant(Value.Class, typeof(object)), Expression.Constant(valueIndex),
                Expression.Constant(binder.Name), Expression.Constant(binder.IgnoreCase));
            var dynamicMetaObject = binder.FallbackDeleteMember(this);
            var succeeds =
                new DynamicMetaObject(Expression.IfThen(Expression.Not(expression), dynamicMetaObject.Expression),
                    dynamicMetaObject.Restrictions);
            return AddDynamicTestAndDefer(binder, Value.Class, null, succeeds);
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return BindGetOrInvokeMember(binder, binder.Name, binder.IgnoreCase, binder.FallbackGetMember(this), null);
        }

        public override DynamicMetaObject BindInvokeMember(
            InvokeMemberBinder binder,
            DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            return BindGetOrInvokeMember(binder, binder.Name, binder.IgnoreCase,
                binder.FallbackInvokeMember(this, args), value => binder.FallbackInvoke(value, args, null));
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ContractUtils.RequiresNotNull(value, nameof(value));
            ExpandoClass klass;
            int index;
            var classEnsureIndex = GetClassEnsureIndex(binder.Name, binder.IgnoreCase, Value, out klass, out index);
            return AddDynamicTestAndDefer(binder, klass, classEnsureIndex,
                new DynamicMetaObject(
                    Expression.Call(typeof(RuntimeOps).GetMethod("ExpandoTrySetValue"), GetLimitedSelf(),
                        Expression.Constant(klass, typeof(object)), Expression.Constant(index),
                        Expression.Convert(value.Expression, typeof(object)), Expression.Constant(binder.Name),
                        Expression.Constant(binder.IgnoreCase)), BindingRestrictions.Empty));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            var expandoData = Value._data;
            var klass = expandoData.Class;
            for (var i = 0; i < klass.Keys.Length; ++i)
            {
                if (expandoData[i] != Uninitialized)
                {
                    yield return klass.Keys[i];
                }
            }
        }

        private DynamicMetaObject AddDynamicTestAndDefer(
            DynamicMetaObjectBinder binder,
            ExpandoClass klass,
            ExpandoClass originalClass,
            DynamicMetaObject succeeds)
        {
            var ifTrue = succeeds.Expression;
            if (originalClass != null)
            {
                ifTrue = Expression.Block(
                    Expression.Call(null, typeof(RuntimeOps).GetMethod("ExpandoPromoteClass"), GetLimitedSelf(),
                        Expression.Constant(originalClass, typeof(object)), Expression.Constant(klass, typeof(object))),
                    succeeds.Expression);
            }

            return new DynamicMetaObject(
                Expression.Condition(
                    Expression.Call(null, typeof(RuntimeOps).GetMethod("ExpandoCheckVersion"), GetLimitedSelf(),
                        Expression.Constant(originalClass ?? klass, typeof(object))), ifTrue,
                    binder.GetUpdateExpression(ifTrue.Type)), GetRestrictions().Merge(succeeds.Restrictions));
        }

        private DynamicMetaObject BindGetOrInvokeMember(
            DynamicMetaObjectBinder binder,
            string name,
            bool ignoreCase,
            DynamicMetaObject fallback,
            Func<DynamicMetaObject, DynamicMetaObject> fallbackInvoke)
        {
            var expandoClass = Value.Class;
            var valueIndex = expandoClass.GetValueIndex(name, ignoreCase, Value);
            var parameterExpression = Expression.Parameter(typeof(object), "value");
            var test = (Expression)Expression.Call(typeof(RuntimeOps).GetMethod("ExpandoTryGetValue"), GetLimitedSelf(),
                Expression.Constant(expandoClass, typeof(object)), Expression.Constant(valueIndex),
                Expression.Constant(name), Expression.Constant(ignoreCase), parameterExpression);
            var dynamicMetaObject = new DynamicMetaObject(parameterExpression, BindingRestrictions.Empty);
            if (fallbackInvoke != null)
            {
                dynamicMetaObject = fallbackInvoke(dynamicMetaObject);
            }

            var succeeds = new DynamicMetaObject(Expression.Block(new ParameterExpression[1]
                {
                    parameterExpression
                }, Expression.Condition(test, dynamicMetaObject.Expression, fallback.Expression, typeof(object))),
                dynamicMetaObject.Restrictions.Merge(fallback.Restrictions));
            return AddDynamicTestAndDefer(binder, Value.Class, null, succeeds);
        }

        private ExpandoClass GetClassEnsureIndex(
            string name,
            bool caseInsensitive,
            ExpandoObject obj,
            out ExpandoClass klass,
            out int index)
        {
            var classEnsureIndex = Value.Class;
            index = classEnsureIndex.GetValueIndex(name, caseInsensitive, obj);
            if (index == -2)
            {
                klass = classEnsureIndex;
                return null;
            }

            if (index == -1)
            {
                var newClass = classEnsureIndex.FindNewClass(name);
                klass = newClass;
                index = newClass.GetValueIndexCaseSensitive(name);
                return classEnsureIndex;
            }

            klass = classEnsureIndex;
            return null;
        }

        private Expression GetLimitedSelf()
        {
            return TypeUtils.AreEquivalent(Expression.Type, LimitType)
                ? Expression
                : Expression.Convert(Expression, LimitType);
        }

        private BindingRestrictions GetRestrictions()
        {
            return BindingRestrictions.GetTypeRestriction(this);
        }
    }

    private class ExpandoData
    {
        internal static readonly ExpandoData Empty = new();
        private readonly object[] _dataArray;
        internal readonly ExpandoClass Class;

        private ExpandoData()
        {
            Class = ExpandoClass.Empty;
            _dataArray = new object[0];
        }

        internal ExpandoData(ExpandoClass klass, object[] data, int version)
        {
            Class = klass;
            _dataArray = data;
            Version = version;
        }

        internal object this[int index]
        {
            get => _dataArray[index];
            set
            {
                ++Version;
                _dataArray[index] = value;
            }
        }

        internal int Version { get; private set; }

        internal int Length => _dataArray.Length;

        internal ExpandoData UpdateClass(ExpandoClass newClass)
        {
            if (_dataArray.Length >= newClass.Keys.Length)
            {
                this[newClass.Keys.Length - 1] = Uninitialized;
                return new ExpandoData(newClass, _dataArray, Version);
            }

            var length = _dataArray.Length;
            var objArray = new object[GetAlignedSize(newClass.Keys.Length)];
            Array.Copy(_dataArray, objArray, _dataArray.Length);
            return new ExpandoData(newClass, objArray, Version)
            {
                [length] = Uninitialized
            };
        }

        private static int GetAlignedSize(int len)
        {
            return (len + 7) & -8;
        }
    }
}