using System.Collections.ObjectModel;

namespace System.Collections;

public class EmptyReadOnlyCollection<T>
{
    public static ReadOnlyCollection<T> Instance { get; } = new([]);
}