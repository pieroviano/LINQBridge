#nullable disable
namespace System.Linq.Parallel;

internal struct Pair<T, U>(T first, U second)
{
    internal T m_first = first;
    internal U m_second = second;

    public T First
    {
        get => m_first;
        set => m_first = value;
    }

    public U Second
    {
        get => m_second;
        set => m_second = value;
    }
}