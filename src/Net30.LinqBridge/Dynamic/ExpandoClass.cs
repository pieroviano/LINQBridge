#nullable disable
using System.Collections.Generic;

namespace System.Dynamic;

internal class ExpandoClass
{
    private const int EmptyHashCode = 6551;
    internal static ExpandoClass Empty = new();
    private readonly int _hashCode;
    private Dictionary<int, List<WeakReference>> _transitions;

    internal ExpandoClass()
    {
        _hashCode = 6551;
        Keys = new string[0];
    }

    internal ExpandoClass(string[] keys, int hashCode)
    {
        _hashCode = hashCode;
        Keys = keys;
    }

    internal string[] Keys { get; }

    internal ExpandoClass FindNewClass(string newKey)
    {
        var hashCode = _hashCode ^ newKey.GetHashCode();
        lock (this)
        {
            var transitionList = GetTransitionList(hashCode);
            for (var index = 0; index < transitionList.Count; ++index)
            {
                if (!(transitionList[index].Target is ExpandoClass target))
                {
                    transitionList.RemoveAt(index);
                    --index;
                }
                else if (string.Equals(target.Keys[target.Keys.Length - 1], newKey, StringComparison.Ordinal))
                {
                    return target;
                }
            }

            var strArray = new string[Keys.Length + 1];
            Array.Copy(Keys, strArray, Keys.Length);
            strArray[Keys.Length] = newKey;
            var target1 = new ExpandoClass(strArray, hashCode);
            transitionList.Add(new WeakReference(target1));
            return target1;
        }
    }

    internal int GetValueIndex(string name, bool caseInsensitive, ExpandoObject obj)
    {
        return caseInsensitive ? GetValueIndexCaseInsensitive(name, obj) : GetValueIndexCaseSensitive(name);
    }

    internal int GetValueIndexCaseSensitive(string name)
    {
        for (var indexCaseSensitive = 0; indexCaseSensitive < Keys.Length; ++indexCaseSensitive)
        {
            if (string.Equals(Keys[indexCaseSensitive], name, StringComparison.Ordinal))
            {
                return indexCaseSensitive;
            }
        }

        return -1;
    }

    private List<WeakReference> GetTransitionList(int hashCode)
    {
        if (_transitions == null)
        {
            _transitions = new Dictionary<int, List<WeakReference>>();
        }

        List<WeakReference> transitionList;
        if (!_transitions.TryGetValue(hashCode, out transitionList))
        {
            _transitions[hashCode] = transitionList = new List<WeakReference>();
        }

        return transitionList;
    }

    private int GetValueIndexCaseInsensitive(string name, ExpandoObject obj)
    {
        var indexCaseInsensitive = -1;
        lock (obj.LockObject)
        {
            for (var index = Keys.Length - 1; index >= 0; --index)
            {
                if (string.Equals(Keys[index], name, StringComparison.OrdinalIgnoreCase) && !obj.IsDeletedMember(index))
                {
                    if (indexCaseInsensitive != -1)
                    {
                        return -2;
                    }

                    indexCaseInsensitive = index;
                }
            }
        }

        return indexCaseInsensitive;
    }
}