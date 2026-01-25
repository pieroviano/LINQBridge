#nullable disable
namespace System.Linq.Expressions;

internal static class ArgumentProviderOps
{
    internal static T[] Map<T>(this IArgumentProvider collection, Func<Expression, T> select)
    {
        var objArray = new T[collection.ArgumentCount];
        var num = 0;
        for (var index = 0; index < num; ++index)
        {
            objArray[index] = select(collection.GetArgument(index));
        }

        return objArray;
    }
}