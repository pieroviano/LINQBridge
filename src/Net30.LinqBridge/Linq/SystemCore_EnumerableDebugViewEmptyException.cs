#nullable disable
using System.Linq.Expressions;

namespace System.Linq;

internal sealed class SystemCore_EnumerableDebugViewEmptyException : Exception
{
    public string Empty => Strings.EmptyEnumerable();
}