#nullable disable
using System.Collections.Generic;
using System.Linq.Parallel;

namespace System.Linq;

internal static class AggregationMinMaxHelpers<T>
{
    internal static T ReduceMax(IEnumerable<T> source)
    {
        return Reduce(source, 1);
    }

    internal static T ReduceMin(IEnumerable<T> source)
    {
        return Reduce(source, -1);
    }

    private static Func<Pair<bool, T>, Pair<bool, T>, Pair<bool, T>> MakeFinalReduceFunction(int sign)
    {
        var comparer = Util.GetDefaultComparer<T>();
        return (accumulator, element) =>
            element.First && (!accumulator.First ||
                              Util.Sign(comparer.Compare(element.Second, accumulator.Second)) == sign)
                ? new Pair<bool, T>(true, element.Second)
                : accumulator;
    }

    private static Func<Pair<bool, T>, T, Pair<bool, T>> MakeIntermediateReduceFunction(int sign)
    {
        var comparer = Util.GetDefaultComparer<T>();
        return (accumulator, element) =>
            (default(T) != null || element != null) &&
            (!accumulator.First || Util.Sign(comparer.Compare(element, accumulator.Second)) == sign)
                ? new Pair<bool, T>(true, element)
                : accumulator;
    }

    private static Func<Pair<bool, T>, T> MakeResultSelectorFunction()
    {
        return accumulator => accumulator.Second;
    }

    private static T Reduce(IEnumerable<T> source, int sign)
    {
        var intermediateReduce = MakeIntermediateReduceFunction(sign);
        var finalReduce = MakeFinalReduceFunction(sign);
        var resultSelector = MakeResultSelectorFunction();
        return new AssociativeAggregationOperator<T, Pair<bool, T>, T>(source, new Pair<bool, T>(false, default), null,
            true, intermediateReduce, finalReduce, resultSelector, default(T) != null,
            QueryAggregationOptions.AssociativeCommutative).Aggregate();
    }
}