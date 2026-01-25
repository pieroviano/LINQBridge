#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

internal class ListArgumentProvider :
    IList<Expression>,
    ICollection<Expression>,
    IEnumerable<Expression>,
    IEnumerable
{
    private readonly Expression _arg0;
    private readonly IArgumentProvider _provider;

    internal ListArgumentProvider(IArgumentProvider provider, Expression arg0)
    {
        _provider = provider;
        _arg0 = arg0;
    }

    public int IndexOf(Expression item)
    {
        if (_arg0 == item)
        {
            return 0;
        }

        for (var index = 1; index < _provider.ArgumentCount; ++index)
        {
            if (_provider.GetArgument(index) == item)
            {
                return index;
            }
        }

        return -1;
    }

    public void Insert(int index, Expression item)
    {
        throw ContractUtils.Unreachable;
    }

    public void RemoveAt(int index)
    {
        throw ContractUtils.Unreachable;
    }

    public Expression this[int index]
    {
        get => index == 0 ? _arg0 : _provider.GetArgument(index);
        set => throw ContractUtils.Unreachable;
    }

    public void Add(Expression item)
    {
        throw ContractUtils.Unreachable;
    }

    public void Clear()
    {
        throw ContractUtils.Unreachable;
    }

    public bool Contains(Expression item)
    {
        return IndexOf(item) != -1;
    }

    public void CopyTo(Expression[] array, int arrayIndex)
    {
        array[arrayIndex++] = _arg0;
        for (var index = 1; index < _provider.ArgumentCount; ++index)
        {
            array[arrayIndex++] = _provider.GetArgument(index);
        }
    }

    public int Count => _provider.ArgumentCount;

    public bool IsReadOnly => true;

    public bool Remove(Expression item)
    {
        throw ContractUtils.Unreachable;
    }

    public IEnumerator<Expression> GetEnumerator()
    {
        yield return _arg0;
        for (var i = 1; i < _provider.ArgumentCount; ++i)
        {
            yield return _provider.GetArgument(i);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        yield return _arg0;
        for (var i = 1; i < _provider.ArgumentCount; ++i)
        {
            yield return _provider.GetArgument(i);
        }
    }
}