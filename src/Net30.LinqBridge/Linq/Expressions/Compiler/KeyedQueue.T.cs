#nullable disable
using System.Collections.Generic;

namespace System.Linq.Expressions.Compiler;

internal sealed class KeyedQueue<K, V>
{
    private readonly Dictionary<K, Queue<V>> _data;

    internal KeyedQueue()
    {
        _data = new Dictionary<K, Queue<V>>();
    }

    internal void Clear()
    {
        _data.Clear();
    }

    internal V Dequeue(K key)
    {
        Queue<V> vQueue;
        if (!_data.TryGetValue(key, out vQueue))
        {
            throw Error.QueueEmpty();
        }

        var v = vQueue.Dequeue();
        if (vQueue.Count == 0)
        {
            _data.Remove(key);
        }

        return v;
    }

    internal void Enqueue(K key, V value)
    {
        Queue<V> vQueue;
        if (!_data.TryGetValue(key, out vQueue))
        {
            _data.Add(key, vQueue = new Queue<V>());
        }

        vQueue.Enqueue(value);
    }

    internal int GetCount(K key)
    {
        Queue<V> vQueue;
        return !_data.TryGetValue(key, out vQueue) ? 0 : vQueue.Count;
    }

    internal V Peek(K key)
    {
        Queue<V> vQueue;
        if (!_data.TryGetValue(key, out vQueue))
        {
            throw Error.QueueEmpty();
        }

        return vQueue.Peek();
    }

    internal bool TryDequeue(K key, out V value)
    {
        Queue<V> vQueue;
        if (_data.TryGetValue(key, out vQueue) && vQueue.Count > 0)
        {
            value = vQueue.Dequeue();
            if (vQueue.Count == 0)
            {
                _data.Remove(key);
            }

            return true;
        }

        value = default;
        return false;
    }
}