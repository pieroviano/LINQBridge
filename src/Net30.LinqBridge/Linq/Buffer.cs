#nullable disable
using System.Collections.Generic;

namespace System.Linq;

internal struct Buffer<TElement>
{
    internal TElement[] items;
    internal int count;

    internal Buffer(IEnumerable<TElement> source)
    {
        var elementArray = (TElement[])null;
        var length = 0;
        if (source is ICollection<TElement> elements)
        {
            length = elements.Count;
            if (length > 0)
            {
                elementArray = new TElement[length];
                elements.CopyTo(elementArray, 0);
            }
        }
        else
        {
            foreach (var element in source)
            {
                if (elementArray == null)
                {
                    elementArray = new TElement[4];
                }
                else if (elementArray.Length == length)
                {
                    var destinationArray = new TElement[checked(length * 2)];
                    Array.Copy(elementArray, 0, destinationArray, 0, length);
                    elementArray = destinationArray;
                }

                elementArray[length] = element;
                ++length;
            }
        }

        items = elementArray;
        count = length;
    }

    internal TElement[] ToArray()
    {
        if (count == 0)
        {
            return new TElement[0];
        }

        if (items.Length == count)
        {
            return items;
        }

        var destinationArray = new TElement[count];
        Array.Copy(items, 0, destinationArray, 0, count);
        return destinationArray;
    }
}