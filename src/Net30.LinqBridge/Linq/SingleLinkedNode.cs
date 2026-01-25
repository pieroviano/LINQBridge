#nullable disable
using System.Collections.Generic;

namespace System.Linq;

internal sealed class SingleLinkedNode<TSource>
{
    public SingleLinkedNode(TSource item)
    {
        Item = item;
    }

    private SingleLinkedNode(SingleLinkedNode<TSource> linked, TSource item)
    {
        Linked = linked;
        Item = item;
    }

    public TSource Item { get; }

    public SingleLinkedNode<TSource> Linked { get; }

    public SingleLinkedNode<TSource> Add(TSource item)
    {
        return new SingleLinkedNode<TSource>(this, item);
    }

    public int GetCount()
    {
        var count = 0;
        for (var singleLinkedNode = this; singleLinkedNode != null; singleLinkedNode = singleLinkedNode.Linked)
        {
            ++count;
        }

        return count;
    }

    public IEnumerator<TSource> GetEnumerator(int count)
    {
        return ((IEnumerable<TSource>)ToArray(count)).GetEnumerator();
    }

    public SingleLinkedNode<TSource> GetNode(int index)
    {
        var node = this;
        for (; index > 0; --index)
        {
            node = node.Linked;
        }

        return node;
    }

    public TSource[] ToArray(int count)
    {
        var array = new TSource[count];
        var index = count;
        for (var singleLinkedNode = this; singleLinkedNode != null; singleLinkedNode = singleLinkedNode.Linked)
        {
            --index;
            array[index] = singleLinkedNode.Item;
        }

        return array;
    }
}