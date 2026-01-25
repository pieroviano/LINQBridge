using System.ComponentModel;
using System.Diagnostics;

namespace System.Runtime.CompilerServices;

/// <summary>Represents a cache of runtime binding rules.</summary>
/// <typeparam name="T">The delegate type.</typeparam>
[DebuggerStepThrough]
[EditorBrowsable(EditorBrowsableState.Never)]
public class RuleCache<T>
    where T : class
{
    private const int MaxRules = 128;

    private const int InsertPosition = 64;

    private readonly object cacheLock;
    private T[] _rules;

    internal RuleCache()
    {
    }

    internal void AddRule(T newRule)
    {
        lock (cacheLock)
        {
            _rules = AddOrInsert(_rules, newRule);
        }
    }

    internal T[] GetRules()
    {
        return _rules;
    }

    internal void MoveRule(T rule, int i)
    {
        lock (cacheLock)
        {
            var length = _rules.Length - i;
            if (length > 8)
            {
                length = 8;
            }

            var num = -1;
            var num1 = Math.Min(_rules.Length, i + length);
            var num2 = i;
            while (num2 < num1)
            {
                if (_rules[num2] != rule)
                {
                    num2++;
                }
                else
                {
                    num = num2;
                    break;
                }
            }

            if (num >= 0)
            {
                var t = _rules[num];
                _rules[num] = _rules[num - 1];
                _rules[num - 1] = _rules[num - 2];
                _rules[num - 2] = t;
            }
        }
    }

    internal void ReplaceRule(T oldRule, T newRule)
    {
        lock (cacheLock)
        {
            var num = Array.IndexOf(_rules, oldRule);
            if (num < 0)
            {
                _rules = AddOrInsert(_rules, newRule);
            }
            else
            {
                _rules[num] = newRule;
            }
        }
    }

    private static T[] AddOrInsert(T[] rules, T item)
    {
        T[] tArray;
        if (rules.Length < 64)
        {
            return rules.AddLast<T>(item);
        }

        var length = rules.Length + 1;
        if (length <= 128)
        {
            tArray = new T[length];
        }
        else
        {
            length = 128;
            tArray = rules;
        }

        Array.Copy(rules, 0, tArray, 0, 64);
        tArray[64] = item;
        Array.Copy(rules, 64, tArray, 65, length - 64 - 1);
        return tArray;
    }
}