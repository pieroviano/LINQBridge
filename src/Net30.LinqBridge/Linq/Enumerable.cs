#pragma warning disable CS3002

#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace System.Linq;

/// <summary>
///     Provides a set of static (Shared in Visual Basic) methods for querying objects that implement
///     <see cref="T:System.Collections.Generic.IEnumerable`1" />.
/// </summary>
public static class Enumerable
{
    /// <summary>Applies an accumulator function over a sequence.</summary>
    /// <returns>The final accumulator value.</returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to aggregate over.</param>
    /// <param name="func">An accumulator function to be invoked on each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="func" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static TSource Aggregate<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, TSource, TSource> func)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (func == null)
        {
            throw Error.ArgumentNull(nameof(func));
        }

        using (var enumerator = source.GetEnumerator())
        {
            var source1 = enumerator.MoveNext() ? enumerator.Current : throw Error.NoElements();
            while (enumerator.MoveNext())
            {
                source1 = func(source1, enumerator.Current);
            }

            return source1;
        }
    }

    /// <summary>
    ///     Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator
    ///     value.
    /// </summary>
    /// <returns>The final accumulator value.</returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to aggregate over.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="func">An accumulator function to be invoked on each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="func" /> is null.
    /// </exception>
    public static TAccumulate Aggregate<TSource, TAccumulate>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> func)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (func == null)
        {
            throw Error.ArgumentNull(nameof(func));
        }

        var accumulate = seed;
        foreach (var source1 in source)
        {
            accumulate = func(accumulate, source1);
        }

        return accumulate;
    }

    /// <summary>
    ///     Applies an accumulator function over a sequence. The specified seed value is used as the initial accumulator
    ///     value, and the specified function is used to select the result value.
    /// </summary>
    /// <returns>The transformed final accumulator value.</returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to aggregate over.</param>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="func">An accumulator function to be invoked on each element.</param>
    /// <param name="resultSelector">A function to transform the final accumulator value into the result value.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TAccumulate">The type of the accumulator value.</typeparam>
    /// <typeparam name="TResult">The type of the resulting value.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="func" /> or <paramref name="resultSelector" /> is null.
    /// </exception>
    public static TResult Aggregate<TSource, TAccumulate, TResult>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> func,
        Func<TAccumulate, TResult> resultSelector)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (func == null)
        {
            throw Error.ArgumentNull(nameof(func));
        }

        if (resultSelector == null)
        {
            throw Error.ArgumentNull(nameof(resultSelector));
        }

        var accumulate = seed;
        foreach (var source1 in source)
        {
            accumulate = func(accumulate, source1);
        }

        return resultSelector(accumulate);
    }

    /// <summary>Determines whether all elements of a sequence satisfy a condition.</summary>
    /// <returns>
    ///     true if every element of the source sequence passes the test in the specified predicate, or if the sequence is
    ///     empty; otherwise, false.
    /// </returns>
    /// <param name="source">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the elements to apply
    ///     the predicate to.
    /// </param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    public static bool All<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (predicate == null)
        {
            throw Error.ArgumentNull(nameof(predicate));
        }

        foreach (var source1 in source)
        {
            if (!predicate(source1))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Determines whether a sequence contains any elements.</summary>
    /// <returns>true if the source sequence contains any elements; otherwise, false.</returns>
    /// <param name="source">The <see cref="T:System.Collections.Generic.IEnumerable`1" /> to check for emptiness.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static bool Any<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        using (var enumerator = source.GetEnumerator())
        {
            if (enumerator.MoveNext())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether any element of a sequence satisfies a condition.</summary>
    /// <returns>true if any elements in the source sequence pass the test in the specified predicate; otherwise, false.</returns>
    /// <param name="source">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements to apply the predicate
    ///     to.
    /// </param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (predicate == null)
        {
            throw Error.ArgumentNull(nameof(predicate));
        }

        foreach (var source1 in source)
        {
            if (predicate(source1))
            {
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<TSource> Append<TSource>(
        this IEnumerable<TSource> source,
        TSource element)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return source is AppendPrependIterator<TSource> appendPrependIterator
            ? appendPrependIterator.Append(element)
            : (IEnumerable<TSource>)new AppendPrepend1Iterator<TSource>(source, element, true);
    }

    /// <summary>Returns the input typed as <see cref="T:System.Collections.Generic.IEnumerable`1" />.</summary>
    /// <returns>The input sequence typed as <see cref="T:System.Collections.Generic.IEnumerable`1" />.</returns>
    /// <param name="source">The sequence to type as <see cref="T:System.Collections.Generic.IEnumerable`1" />.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    public static IEnumerable<TSource> AsEnumerable<TSource>(this IEnumerable<TSource> source)
    {
        return source;
    }

    /// <summary>Computes the average of a sequence of <see cref="T:System.Int32" /> values.</summary>
    /// <returns>The average of the sequence of values.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Int32" /> values to calculate the average of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static double Average(this IEnumerable<int> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        long num1 = 0;
        long num2 = 0;
        foreach (var num3 in source)
        {
            checked
            {
                num1 += num3;
            }

            checked
            {
                ++num2;
            }
        }

        if (num2 > 0L)
        {
            return num1 / (double)num2;
        }

        throw Error.NoElements();
    }

    /// <summary>Computes the average of a sequence of nullable <see cref="T:System.Int32" /> values.</summary>
    /// <returns>
    ///     The average of the sequence of values, or null if the source sequence is empty or contains only values that
    ///     are null.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Int32" /> values to calculate the average of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The sum of the elements in the sequence is larger than
    ///     <see cref="F:System.Int64.MaxValue" />.
    /// </exception>
    public static double? Average(this IEnumerable<int?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        long num1 = 0;
        long num2 = 0;
        foreach (var nullable in source)
        {
            if (nullable.HasValue)
            {
                checked
                {
                    num1 += nullable.GetValueOrDefault();
                }

                checked
                {
                    ++num2;
                }
            }
        }

        return num2 > 0L ? num1 / (double)num2 : new double?();
    }

    /// <summary>Computes the average of a sequence of <see cref="T:System.Int64" /> values.</summary>
    /// <returns>The average of the sequence of values.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Int64" /> values to calculate the average of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static double Average(this IEnumerable<long> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        long num1 = 0;
        long num2 = 0;
        foreach (var num3 in source)
        {
            checked
            {
                num1 += num3;
            }

            checked
            {
                ++num2;
            }
        }

        if (num2 > 0L)
        {
            return num1 / (double)num2;
        }

        throw Error.NoElements();
    }

    /// <summary>Computes the average of a sequence of nullable <see cref="T:System.Int64" /> values.</summary>
    /// <returns>
    ///     The average of the sequence of values, or null if the source sequence is empty or contains only values that
    ///     are null.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Int64" /> values to calculate the average of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The sum of the elements in the sequence is larger than
    ///     <see cref="F:System.Int64.MaxValue" />.
    /// </exception>
    public static double? Average(this IEnumerable<long?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        long num1 = 0;
        long num2 = 0;
        foreach (var nullable in source)
        {
            if (nullable.HasValue)
            {
                checked
                {
                    num1 += nullable.GetValueOrDefault();
                }

                checked
                {
                    ++num2;
                }
            }
        }

        return num2 > 0L ? num1 / (double)num2 : new double?();
    }

    /// <summary>Computes the average of a sequence of <see cref="T:System.Single" /> values.</summary>
    /// <returns>The average of the sequence of values.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Single" /> values to calculate the average of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static float Average(this IEnumerable<float> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0.0;
        long num2 = 0;
        foreach (var num3 in source)
        {
            num1 += num3;
            checked
            {
                ++num2;
            }
        }

        if (num2 > 0L)
        {
            return (float)num1 / num2;
        }

        throw Error.NoElements();
    }

    /// <summary>Computes the average of a sequence of nullable <see cref="T:System.Single" /> values.</summary>
    /// <returns>
    ///     The average of the sequence of values, or null if the source sequence is empty or contains only values that
    ///     are null.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Single" /> values to calculate the average of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static float? Average(this IEnumerable<float?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0.0;
        long num2 = 0;
        foreach (var nullable in source)
        {
            if (nullable.HasValue)
            {
                num1 += nullable.GetValueOrDefault();
                checked
                {
                    ++num2;
                }
            }
        }

        return num2 > 0L ? (float)num1 / num2 : new float?();
    }

    /// <summary>Computes the average of a sequence of <see cref="T:System.Double" /> values.</summary>
    /// <returns>The average of the sequence of values.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Double" /> values to calculate the average of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static double Average(this IEnumerable<double> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0.0;
        long num2 = 0;
        foreach (var num3 in source)
        {
            num1 += num3;
            checked
            {
                ++num2;
            }
        }

        if (num2 > 0L)
        {
            return num1 / num2;
        }

        throw Error.NoElements();
    }

    /// <summary>Computes the average of a sequence of nullable <see cref="T:System.Double" /> values.</summary>
    /// <returns>
    ///     The average of the sequence of values, or null if the source sequence is empty or contains only values that
    ///     are null.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Double" /> values to calculate the average of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static double? Average(this IEnumerable<double?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0.0;
        long num2 = 0;
        foreach (var nullable in source)
        {
            if (nullable.HasValue)
            {
                num1 += nullable.GetValueOrDefault();
                checked
                {
                    ++num2;
                }
            }
        }

        return num2 > 0L ? num1 / num2 : new double?();
    }

    /// <summary>Computes the average of a sequence of <see cref="T:System.Decimal" /> values.</summary>
    /// <returns>The average of the sequence of values.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Decimal" /> values to calculate the average of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static decimal Average(this IEnumerable<decimal> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0M;
        long num2 = 0;
        foreach (var num3 in source)
        {
            num1 += num3;
            checked
            {
                ++num2;
            }
        }

        if (num2 > 0L)
        {
            return num1 / num2;
        }

        throw Error.NoElements();
    }

    /// <summary>Computes the average of a sequence of nullable <see cref="T:System.Decimal" /> values.</summary>
    /// <returns>
    ///     The average of the sequence of values, or null if the source sequence is empty or contains only values that
    ///     are null.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Decimal" /> values to calculate the average of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The sum of the elements in the sequence is larger than
    ///     <see cref="F:System.Decimal.MaxValue" />.
    /// </exception>
    public static decimal? Average(this IEnumerable<decimal?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0M;
        long num2 = 0;
        foreach (var nullable in source)
        {
            if (nullable.HasValue)
            {
                num1 += nullable.GetValueOrDefault();
                checked
                {
                    ++num2;
                }
            }
        }

        return num2 > 0L ? num1 / num2 : new decimal?();
    }

    /// <summary>
    ///     Computes the average of a sequence of <see cref="T:System.Int32" /> values that are obtained by invoking a
    ///     transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The average of the sequence of values.</returns>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The sum of the elements in the sequence is larger than
    ///     <see cref="F:System.Int64.MaxValue" />.
    /// </exception>
    public static double Average<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, int> selector)
    {
        return source.Select(selector).Average();
    }

    /// <summary>
    ///     Computes the average of a sequence of nullable <see cref="T:System.Int32" /> values that are obtained by
    ///     invoking a transform function on each element of the input sequence.
    /// </summary>
    /// <returns>
    ///     The average of the sequence of values, or null if the source sequence is empty or contains only values that
    ///     are null.
    /// </returns>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The sum of the elements in the sequence is larger than
    ///     <see cref="F:System.Int64.MaxValue" />.
    /// </exception>
    public static double? Average<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, int?> selector)
    {
        return source.Select(selector).Average();
    }

    /// <summary>
    ///     Computes the average of a sequence of <see cref="T:System.Int64" /> values that are obtained by invoking a
    ///     transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The average of the sequence of values.</returns>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of source.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The sum of the elements in the sequence is larger than
    ///     <see cref="F:System.Int64.MaxValue" />.
    /// </exception>
    public static double Average<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, long> selector)
    {
        return source.Select(selector).Average();
    }

    /// <summary>
    ///     Computes the average of a sequence of nullable <see cref="T:System.Int64" /> values that are obtained by
    ///     invoking a transform function on each element of the input sequence.
    /// </summary>
    /// <returns>
    ///     The average of the sequence of values, or null if the source sequence is empty or contains only values that
    ///     are null.
    /// </returns>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    public static double? Average<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, long?> selector)
    {
        return source.Select(selector).Average();
    }

    /// <summary>
    ///     Computes the average of a sequence of <see cref="T:System.Single" /> values that are obtained by invoking a
    ///     transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The average of the sequence of values.</returns>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static float Average<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, float> selector)
    {
        return source.Select(selector).Average();
    }

    /// <summary>
    ///     Computes the average of a sequence of nullable <see cref="T:System.Single" /> values that are obtained by
    ///     invoking a transform function on each element of the input sequence.
    /// </summary>
    /// <returns>
    ///     The average of the sequence of values, or null if the source sequence is empty or contains only values that
    ///     are null.
    /// </returns>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static float? Average<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, float?> selector)
    {
        return source.Select(selector).Average();
    }

    /// <summary>
    ///     Computes the average of a sequence of <see cref="T:System.Double" /> values that are obtained by invoking a
    ///     transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The average of the sequence of values.</returns>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static double Average<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, double> selector)
    {
        return source.Select(selector).Average();
    }

    /// <summary>
    ///     Computes the average of a sequence of nullable <see cref="T:System.Double" /> values that are obtained by
    ///     invoking a transform function on each element of the input sequence.
    /// </summary>
    /// <returns>
    ///     The average of the sequence of values, or null if the source sequence is empty or contains only values that
    ///     are null.
    /// </returns>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static double? Average<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, double?> selector)
    {
        return source.Select(selector).Average();
    }

    /// <summary>
    ///     Computes the average of a sequence of <see cref="T:System.Decimal" /> values that are obtained by invoking a
    ///     transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The average of the sequence of values.</returns>
    /// <param name="source">A sequence of values that are used to calculate an average.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The sum of the elements in the sequence is larger than
    ///     <see cref="F:System.Decimal.MaxValue" />.
    /// </exception>
    public static decimal Average<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, decimal> selector)
    {
        return source.Select(selector).Average();
    }

    /// <summary>
    ///     Computes the average of a sequence of nullable <see cref="T:System.Decimal" /> values that are obtained by
    ///     invoking a transform function on each element of the input sequence.
    /// </summary>
    /// <returns>
    ///     The average of the sequence of values, or null if the source sequence is empty or contains only values that
    ///     are null.
    /// </returns>
    /// <param name="source">A sequence of values to calculate the average of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The sum of the elements in the sequence is larger than
    ///     <see cref="F:System.Decimal.MaxValue" />.
    /// </exception>
    public static decimal? Average<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, decimal?> selector)
    {
        return source.Select(selector).Average();
    }

    /// <summary>Casts the elements of an <see cref="T:System.Collections.IEnumerable" /> to the specified type.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains each element of the source sequence
    ///     cast to the specified type.
    /// </returns>
    /// <param name="source">
    ///     The <see cref="T:System.Collections.IEnumerable" /> that contains the elements to be cast to type
    ///     <paramref name="TResult" />.
    /// </param>
    /// <typeparam name="TResult">The type to cast the elements of <paramref name="source" /> to.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidCastException">
    ///     An element in the sequence cannot be cast to type
    ///     <paramref name="TResult" />.
    /// </exception>
    public static IEnumerable<TResult> Cast<TResult>(this IEnumerable source)
    {
        if (source is IEnumerable<TResult> results)
        {
            return results;
        }

        return source != null ? CastIterator<TResult>(source) : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>Concatenates two sequences.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the concatenated elements of the
    ///     two input sequences.
    /// </returns>
    /// <param name="first">The first sequence to concatenate.</param>
    /// <param name="second">The sequence to concatenate to the first sequence.</param>
    /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="first" /> or <paramref name="second" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Concat<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second)
    {
        if (first == null)
        {
            throw Error.ArgumentNull(nameof(first));
        }

        return second != null ? ConcatIterator(first, second) : throw Error.ArgumentNull(nameof(second));
    }

    /// <summary>Determines whether a sequence contains a specified element by using the default equality comparer.</summary>
    /// <returns>true if the source sequence contains an element that has the specified value; otherwise, false.</returns>
    /// <param name="source">A sequence in which to locate a value.</param>
    /// <param name="value">The value to locate in the sequence.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value)
    {
        return source is ICollection<TSource> sources ? sources.Contains(value) : source.Contains(value, null);
    }

    /// <summary>
    ///     Determines whether a sequence contains a specified element by using a specified
    ///     <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.
    /// </summary>
    /// <returns>true if the source sequence contains an element that has the specified value; otherwise, false.</returns>
    /// <param name="source">A sequence in which to locate a value.</param>
    /// <param name="value">The value to locate in the sequence.</param>
    /// <param name="comparer">An equality comparer to compare values.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static bool Contains<TSource>(
        this IEnumerable<TSource> source,
        TSource value,
        IEqualityComparer<TSource> comparer)
    {
        if (comparer == null)
        {
            comparer = EqualityComparer<TSource>.Default;
        }

        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        foreach (var x in source)
        {
            if (comparer.Equals(x, value))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the number of elements in a sequence.</summary>
    /// <returns>The number of elements in the input sequence.</returns>
    /// <param name="source">A sequence that contains elements to be counted.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The number of elements in <paramref name="source" /> is larger than
    ///     <see cref="F:System.Int32.MaxValue" />.
    /// </exception>
    public static int Count<TSource>(this IEnumerable<TSource> source)
    {
        switch (source)
        {
            case null:
                throw Error.ArgumentNull(nameof(source));
            case ICollection<TSource> sources:
                return sources.Count;
            case ICollection collection:
                return collection.Count;
            default:
                var num = 0;
                using (var enumerator = source.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        checked
                        {
                            ++num;
                        }
                    }
                }

                return num;
        }
    }

    /// <summary>Returns a number that represents how many elements in the specified sequence satisfy a condition.</summary>
    /// <returns>A number that represents how many elements in the sequence satisfy the condition in the predicate function.</returns>
    /// <param name="source">A sequence that contains elements to be tested and counted.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The number of elements in <paramref name="source" /> is larger than
    ///     <see cref="F:System.Int32.MaxValue" />.
    /// </exception>
    public static int Count<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (predicate == null)
        {
            throw Error.ArgumentNull(nameof(predicate));
        }

        var num = 0;
        foreach (var source1 in source)
        {
            if (predicate(source1))
            {
                checked
                {
                    ++num;
                }
            }
        }

        return num;
    }

    /// <summary>
    ///     Returns the elements of the specified sequence or the type parameter's default value in a singleton collection
    ///     if the sequence is empty.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> object that contains the default value for the
    ///     <paramref name="TSource" /> type if <paramref name="source" /> is empty; otherwise, <paramref name="source" />.
    /// </returns>
    /// <param name="source">The sequence to return a default value for if it is empty.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static IEnumerable<TSource> DefaultIfEmpty<TSource>(this IEnumerable<TSource> source)
    {
        return source.DefaultIfEmpty(default);
    }

    /// <summary>
    ///     Returns the elements of the specified sequence or the specified value in a singleton collection if the
    ///     sequence is empty.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <paramref name="defaultValue" /> if
    ///     <paramref name="source" /> is empty; otherwise, <paramref name="source" />.
    /// </returns>
    /// <param name="source">The sequence to return the specified value for if it is empty.</param>
    /// <param name="defaultValue">The value to return if the sequence is empty.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    public static IEnumerable<TSource> DefaultIfEmpty<TSource>(
        this IEnumerable<TSource> source,
        TSource defaultValue)
    {
        return source != null ? DefaultIfEmptyIterator(source, defaultValue) : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>Returns distinct elements from a sequence by using the default equality comparer to compare values.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains distinct elements from the source
    ///     sequence.
    /// </returns>
    /// <param name="source">The sequence to remove duplicate elements from.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source)
    {
        return source != null ? DistinctIterator(source, null) : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>
    ///     Returns distinct elements from a sequence by using a specified
    ///     <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare values.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains distinct elements from the source
    ///     sequence.
    /// </returns>
    /// <param name="source">The sequence to remove duplicate elements from.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare values.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Distinct<TSource>(
        this IEnumerable<TSource> source,
        IEqualityComparer<TSource> comparer)
    {
        return source != null ? DistinctIterator(source, comparer) : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>Returns the element at a specified index in a sequence.</summary>
    /// <returns>The element at the specified position in the source sequence.</returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return an element from.</param>
    /// <param name="index">The zero-based index of the element to retrieve.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///     <paramref name="index" /> is less than 0 or greater than or equal to the number of elements in
    ///     <paramref name="source" />.
    /// </exception>
    public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (source is IList<TSource> sourceList)
        {
            return sourceList[index];
        }

        if (index < 0)
        {
            throw Error.ArgumentOutOfRange(nameof(index));
        }

        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                if (index == 0)
                {
                    return enumerator.Current;
                }

                --index;
            }

            throw Error.ArgumentOutOfRange(nameof(index));
        }
    }

    /// <summary>Returns the element at a specified index in a sequence or a default value if the index is out of range.</summary>
    /// <returns>
    ///     default(<paramref name="TSource" />) if the index is outside the bounds of the source sequence; otherwise, the
    ///     element at the specified position in the source sequence.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return an element from.</param>
    /// <param name="index">The zero-based index of the element to retrieve.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static TSource ElementAtOrDefault<TSource>(this IEnumerable<TSource> source, int index)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (index >= 0)
        {
            if (source is IList<TSource> sourceList)
            {
                if (index < sourceList.Count)
                {
                    return sourceList[index];
                }
            }
            else
            {
                foreach (var source1 in source)
                {
                    if (index == 0)
                    {
                        return source1;
                    }

                    --index;
                }
            }
        }

        return default;
    }

    /// <summary>
    ///     Returns an empty <see cref="T:System.Collections.Generic.IEnumerable`1" /> that has the specified type
    ///     argument.
    /// </summary>
    /// <returns>
    ///     An empty <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose type argument is
    ///     <paramref name="TResult" />.
    /// </returns>
    /// <typeparam name="TResult">
    ///     The type to assign to the type parameter of the returned generic
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" />.
    /// </typeparam>
    public static IEnumerable<TResult> Empty<TResult>()
    {
        return EmptyEnumerable<TResult>.Instance;
    }

    /// <summary>Produces the set difference of two sequences by using the default equality comparer to compare values.</summary>
    /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
    /// <param name="first">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements that are not also in
    ///     <paramref name="second" /> will be returned.
    /// </param>
    /// <param name="second">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements that also occur in the
    ///     first sequence will cause those elements to be removed from the returned sequence.
    /// </param>
    /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="first" /> or <paramref name="second" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Except<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second)
    {
        if (first == null)
        {
            throw Error.ArgumentNull(nameof(first));
        }

        return second != null ? ExceptIterator(first, second, null) : throw Error.ArgumentNull(nameof(second));
    }

    /// <summary>
    ///     Produces the set difference of two sequences by using the specified
    ///     <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare values.
    /// </summary>
    /// <returns>A sequence that contains the set difference of the elements of two sequences.</returns>
    /// <param name="first">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements that are not also in
    ///     <paramref name="second" /> will be returned.
    /// </param>
    /// <param name="second">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements that also occur in the
    ///     first sequence will cause those elements to be removed from the returned sequence.
    /// </param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare values.</param>
    /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="first" /> or <paramref name="second" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Except<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second,
        IEqualityComparer<TSource> comparer)
    {
        if (first == null)
        {
            throw Error.ArgumentNull(nameof(first));
        }

        if (second == null)
        {
            throw Error.ArgumentNull(nameof(second));
        }

        return ExceptIterator(first, second, comparer);
    }

    /// <summary>Returns the first element of a sequence.</summary>
    /// <returns>The first element in the specified sequence.</returns>
    /// <param name="source">The <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return the first element of.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">The source sequence is empty.</exception>
    public static TSource First<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (source is IList<TSource> sourceList)
        {
            if (sourceList.Count > 0)
            {
                return sourceList[0];
            }
        }
        else
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    return enumerator.Current;
                }
            }
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the first element in a sequence that satisfies a specified condition.</summary>
    /// <returns>The first element in the sequence that passes the test in the specified predicate function.</returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return an element from.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     No element satisfies the condition in
    ///     <paramref name="predicate" />.-or-The source sequence is empty.
    /// </exception>
    public static TSource First<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (predicate == null)
        {
            throw Error.ArgumentNull(nameof(predicate));
        }

        foreach (var source1 in source)
        {
            if (predicate(source1))
            {
                return source1;
            }
        }

        throw Error.NoMatch();
    }

    /// <summary>Returns the first element of a sequence, or a default value if the sequence contains no elements.</summary>
    /// <returns>
    ///     default(<paramref name="TSource" />) if <paramref name="source" /> is empty; otherwise, the first element in
    ///     <paramref name="source" />.
    /// </returns>
    /// <param name="source">The <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return the first element of.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (source is IList<TSource> sourceList)
        {
            if (sourceList.Count > 0)
            {
                return sourceList[0];
            }
        }
        else
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    return enumerator.Current;
                }
            }
        }

        return default;
    }

    /// <summary>
    ///     Returns the first element of the sequence that satisfies a condition or a default value if no such element is
    ///     found.
    /// </summary>
    /// <returns>
    ///     default(<paramref name="TSource" />) if <paramref name="source" /> is empty or if no element passes the test
    ///     specified by <paramref name="predicate" />; otherwise, the first element in <paramref name="source" /> that passes
    ///     the test specified by <paramref name="predicate" />.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return an element from.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    public static TSource FirstOrDefault<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (predicate == null)
        {
            throw Error.ArgumentNull(nameof(predicate));
        }

        foreach (var source1 in source)
        {
            if (predicate(source1))
            {
                return source1;
            }
        }

        return default;
    }

    /// <summary>Groups the elements of a sequence according to a specified key selector function.</summary>
    /// <returns>
    ///     An IEnumerable&lt;IGrouping&lt;TKey, TSource&gt;&gt; in C# or IEnumerable(Of IGrouping(Of TKey, TSource)) in
    ///     Visual Basic where each <see cref="T:System.Linq.IGrouping`2" /> object contains a sequence of objects and a key.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements to group.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return new GroupedEnumerable<TSource, TKey, TSource>(source, keySelector, IdentityFunction<TSource>.Instance,
            null);
    }

    /// <summary>
    ///     Groups the elements of a sequence according to a specified key selector function and compares the keys by
    ///     using a specified comparer.
    /// </summary>
    /// <returns>
    ///     An IEnumerable&lt;IGrouping&lt;TKey, TSource&gt;&gt; in C# or IEnumerable(Of IGrouping(Of TKey, TSource)) in
    ///     Visual Basic where each <see cref="T:System.Linq.IGrouping`2" /> object contains a collection of objects and a key.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements to group.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare keys.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey> comparer)
    {
        return new GroupedEnumerable<TSource, TKey, TSource>(source, keySelector, IdentityFunction<TSource>.Instance,
            comparer);
    }

    /// <summary>
    ///     Groups the elements of a sequence according to a specified key selector function and projects the elements for
    ///     each group by using a specified function.
    /// </summary>
    /// <returns>
    ///     An IEnumerable&lt;IGrouping&lt;TKey, TElement&gt;&gt; in C# or IEnumerable(Of IGrouping(Of TKey, TElement)) in
    ///     Visual Basic where each <see cref="T:System.Linq.IGrouping`2" /> object contains a collection of objects of type
    ///     <paramref name="TElement" /> and a key.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements to group.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="elementSelector">
    ///     A function to map each source element to an element in the
    ///     <see cref="T:System.Linq.IGrouping`2" />.
    /// </param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the <see cref="T:System.Linq.IGrouping`2" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is null.
    /// </exception>
    public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector)
    {
        return new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, null);
    }

    /// <summary>
    ///     Groups the elements of a sequence according to a key selector function. The keys are compared by using a
    ///     comparer and each group's elements are projected by using a specified function.
    /// </summary>
    /// <returns>
    ///     An IEnumerable&lt;IGrouping&lt;TKey, TElement&gt;&gt; in C# or IEnumerable(Of IGrouping(Of TKey, TElement)) in
    ///     Visual Basic where each <see cref="T:System.Linq.IGrouping`2" /> object contains a collection of objects of type
    ///     <paramref name="TElement" /> and a key.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements to group.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="elementSelector">
    ///     A function to map each source element to an element in an
    ///     <see cref="T:System.Linq.IGrouping`2" />.
    /// </param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare keys.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <typeparam name="TElement">The type of the elements in the <see cref="T:System.Linq.IGrouping`2" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is null.
    /// </exception>
    public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        IEqualityComparer<TKey> comparer)
    {
        return new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
    }

    /// <summary>
    ///     Groups the elements of a sequence according to a specified key selector function and creates a result value
    ///     from each group and its key.
    /// </summary>
    /// <returns>
    ///     A collection of elements of type <paramref name="TResult" /> where each element represents a projection over a
    ///     group and its key.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements to group.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="resultSelector">A function to create a result value from each group.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
    public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
    {
        return new GroupedEnumerable<TSource, TKey, TSource, TResult>(source, keySelector,
            IdentityFunction<TSource>.Instance, resultSelector, null);
    }

    /// <summary>
    ///     Groups the elements of a sequence according to a specified key selector function and creates a result value
    ///     from each group and its key. The elements of each group are projected by using a specified function.
    /// </summary>
    /// <returns>
    ///     A collection of elements of type <paramref name="TResult" /> where each element represents a projection over a
    ///     group and its key.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements to group.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="elementSelector">
    ///     A function to map each source element to an element in an
    ///     <see cref="T:System.Linq.IGrouping`2" />.
    /// </param>
    /// <param name="resultSelector">A function to create a result value from each group.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <typeparam name="TElement">The type of the elements in each <see cref="T:System.Linq.IGrouping`2" />.</typeparam>
    /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
    public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
    {
        return new GroupedEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector,
            resultSelector, null);
    }

    /// <summary>
    ///     Groups the elements of a sequence according to a specified key selector function and creates a result value
    ///     from each group and its key. The keys are compared by using a specified comparer.
    /// </summary>
    /// <returns>
    ///     A collection of elements of type <paramref name="TResult" /> where each element represents a projection over a
    ///     group and its key.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements to group.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="resultSelector">A function to create a result value from each group.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare keys with.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
    public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TKey, IEnumerable<TSource>, TResult> resultSelector,
        IEqualityComparer<TKey> comparer)
    {
        return new GroupedEnumerable<TSource, TKey, TSource, TResult>(source, keySelector,
            IdentityFunction<TSource>.Instance, resultSelector, comparer);
    }

    /// <summary>
    ///     Groups the elements of a sequence according to a specified key selector function and creates a result value
    ///     from each group and its key. Key values are compared by using a specified comparer, and the elements of each group
    ///     are projected by using a specified function.
    /// </summary>
    /// <returns>
    ///     A collection of elements of type <paramref name="TResult" /> where each element represents a projection over a
    ///     group and its key.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements to group.</param>
    /// <param name="keySelector">A function to extract the key for each element.</param>
    /// <param name="elementSelector">
    ///     A function to map each source element to an element in an
    ///     <see cref="T:System.Linq.IGrouping`2" />.
    /// </param>
    /// <param name="resultSelector">A function to create a result value from each group.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare keys with.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <typeparam name="TElement">The type of the elements in each <see cref="T:System.Linq.IGrouping`2" />.</typeparam>
    /// <typeparam name="TResult">The type of the result value returned by <paramref name="resultSelector" />.</typeparam>
    public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        Func<TKey, IEnumerable<TElement>, TResult> resultSelector,
        IEqualityComparer<TKey> comparer)
    {
        return new GroupedEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector,
            resultSelector, comparer);
    }

    /// <summary>
    ///     Correlates the elements of two sequences based on equality of keys and groups the results. The default
    ///     equality comparer is used to compare keys.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains elements of type
    ///     <paramref name="TResult" /> that are obtained by performing a grouped join on two sequences.
    /// </returns>
    /// <param name="outer">The first sequence to join.</param>
    /// <param name="inner">The sequence to join to the first sequence.</param>
    /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
    /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
    /// <param name="resultSelector">
    ///     A function to create a result element from an element from the first sequence and a
    ///     collection of matching elements from the second sequence.
    /// </param>
    /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
    /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
    /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or
    ///     <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is null.
    /// </exception>
    public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
        this IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        Func<TOuter, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        Func<TOuter, IEnumerable<TInner>, TResult> resultSelector)
    {
        if (outer == null)
        {
            throw Error.ArgumentNull(nameof(outer));
        }

        if (inner == null)
        {
            throw Error.ArgumentNull(nameof(inner));
        }

        if (outerKeySelector == null)
        {
            throw Error.ArgumentNull(nameof(outerKeySelector));
        }

        if (innerKeySelector == null)
        {
            throw Error.ArgumentNull(nameof(innerKeySelector));
        }

        if (resultSelector == null)
        {
            throw Error.ArgumentNull(nameof(resultSelector));
        }

        return GroupJoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
    }

    /// <summary>
    ///     Correlates the elements of two sequences based on key equality and groups the results. A specified
    ///     <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> is used to compare keys.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains elements of type
    ///     <paramref name="TResult" /> that are obtained by performing a grouped join on two sequences.
    /// </returns>
    /// <param name="outer">The first sequence to join.</param>
    /// <param name="inner">The sequence to join to the first sequence.</param>
    /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
    /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
    /// <param name="resultSelector">
    ///     A function to create a result element from an element from the first sequence and a
    ///     collection of matching elements from the second sequence.
    /// </param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to hash and compare keys.</param>
    /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
    /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
    /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or
    ///     <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is null.
    /// </exception>
    public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
        this IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        Func<TOuter, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
        IEqualityComparer<TKey> comparer)
    {
        if (outer == null)
        {
            throw Error.ArgumentNull(nameof(outer));
        }

        if (inner == null)
        {
            throw Error.ArgumentNull(nameof(inner));
        }

        if (outerKeySelector == null)
        {
            throw Error.ArgumentNull(nameof(outerKeySelector));
        }

        if (innerKeySelector == null)
        {
            throw Error.ArgumentNull(nameof(innerKeySelector));
        }

        if (resultSelector == null)
        {
            throw Error.ArgumentNull(nameof(resultSelector));
        }

        return GroupJoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
    }

    /// <summary>Produces the set intersection of two sequences by using the default equality comparer to compare values.</summary>
    /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
    /// <param name="first">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose distinct elements that also
    ///     appear in <paramref name="second" /> will be returned.
    /// </param>
    /// <param name="second">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose distinct elements that also
    ///     appear in the first sequence will be returned.
    /// </param>
    /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="first" /> or <paramref name="second" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Intersect<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second)
    {
        if (first == null)
        {
            throw Error.ArgumentNull(nameof(first));
        }

        return second != null ? IntersectIterator(first, second, null) : throw Error.ArgumentNull(nameof(second));
    }

    /// <summary>
    ///     Produces the set intersection of two sequences by using the specified
    ///     <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare values.
    /// </summary>
    /// <returns>A sequence that contains the elements that form the set intersection of two sequences.</returns>
    /// <param name="first">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose distinct elements that also
    ///     appear in <paramref name="second" /> will be returned.
    /// </param>
    /// <param name="second">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose distinct elements that also
    ///     appear in the first sequence will be returned.
    /// </param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare values.</param>
    /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="first" /> or <paramref name="second" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Intersect<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second,
        IEqualityComparer<TSource> comparer)
    {
        if (first == null)
        {
            throw Error.ArgumentNull(nameof(first));
        }

        if (second == null)
        {
            throw Error.ArgumentNull(nameof(second));
        }

        return IntersectIterator(first, second, comparer);
    }

    /// <summary>
    ///     Correlates the elements of two sequences based on matching keys. The default equality comparer is used to
    ///     compare keys.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that has elements of type
    ///     <paramref name="TResult" /> that are obtained by performing an inner join on two sequences.
    /// </returns>
    /// <param name="outer">The first sequence to join.</param>
    /// <param name="inner">The sequence to join to the first sequence.</param>
    /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
    /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
    /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
    /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
    /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
    /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or
    ///     <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is null.
    /// </exception>
    public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(
        this IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        Func<TOuter, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        Func<TOuter, TInner, TResult> resultSelector)
    {
        if (outer == null)
        {
            throw Error.ArgumentNull(nameof(outer));
        }

        if (inner == null)
        {
            throw Error.ArgumentNull(nameof(inner));
        }

        if (outerKeySelector == null)
        {
            throw Error.ArgumentNull(nameof(outerKeySelector));
        }

        if (innerKeySelector == null)
        {
            throw Error.ArgumentNull(nameof(innerKeySelector));
        }

        if (resultSelector == null)
        {
            throw Error.ArgumentNull(nameof(resultSelector));
        }

        return JoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);
    }

    /// <summary>
    ///     Correlates the elements of two sequences based on matching keys. A specified
    ///     <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> is used to compare keys.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that has elements of type
    ///     <paramref name="TResult" /> that are obtained by performing an inner join on two sequences.
    /// </returns>
    /// <param name="outer">The first sequence to join.</param>
    /// <param name="inner">The sequence to join to the first sequence.</param>
    /// <param name="outerKeySelector">A function to extract the join key from each element of the first sequence.</param>
    /// <param name="innerKeySelector">A function to extract the join key from each element of the second sequence.</param>
    /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to hash and compare keys.</param>
    /// <typeparam name="TOuter">The type of the elements of the first sequence.</typeparam>
    /// <typeparam name="TInner">The type of the elements of the second sequence.</typeparam>
    /// <typeparam name="TKey">The type of the keys returned by the key selector functions.</typeparam>
    /// <typeparam name="TResult">The type of the result elements.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="outer" /> or <paramref name="inner" /> or <paramref name="outerKeySelector" /> or
    ///     <paramref name="innerKeySelector" /> or <paramref name="resultSelector" /> is null.
    /// </exception>
    public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult>(
        this IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        Func<TOuter, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        Func<TOuter, TInner, TResult> resultSelector,
        IEqualityComparer<TKey> comparer)
    {
        if (outer == null)
        {
            throw Error.ArgumentNull(nameof(outer));
        }

        if (inner == null)
        {
            throw Error.ArgumentNull(nameof(inner));
        }

        if (outerKeySelector == null)
        {
            throw Error.ArgumentNull(nameof(outerKeySelector));
        }

        if (innerKeySelector == null)
        {
            throw Error.ArgumentNull(nameof(innerKeySelector));
        }

        if (resultSelector == null)
        {
            throw Error.ArgumentNull(nameof(resultSelector));
        }

        return JoinIterator(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
    }

    /// <summary>Returns the last element of a sequence.</summary>
    /// <returns>The value at the last position in the source sequence.</returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return the last element of.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">The source sequence is empty.</exception>
    public static TSource Last<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (source is IList<TSource> sourceList)
        {
            var count = sourceList.Count;
            if (count > 0)
            {
                return sourceList[count - 1];
            }
        }
        else
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    TSource current;
                    do
                    {
                        current = enumerator.Current;
                    } while (enumerator.MoveNext());

                    return current;
                }
            }
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the last element of a sequence that satisfies a specified condition.</summary>
    /// <returns>The last element in the sequence that passes the test in the specified predicate function.</returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return an element from.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     No element satisfies the condition in
    ///     <paramref name="predicate" />.-or-The source sequence is empty.
    /// </exception>
    public static TSource Last<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (predicate == null)
        {
            throw Error.ArgumentNull(nameof(predicate));
        }

        var source1 = default(TSource);
        var flag = false;
        foreach (var source2 in source)
        {
            if (predicate(source2))
            {
                source1 = source2;
                flag = true;
            }
        }

        if (flag)
        {
            return source1;
        }

        throw Error.NoMatch();
    }

    /// <summary>Returns the last element of a sequence, or a default value if the sequence contains no elements.</summary>
    /// <returns>
    ///     default(<paramref name="TSource" />) if the source sequence is empty; otherwise, the last element in the
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" />.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return the last element of.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static TSource LastOrDefault<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (source is IList<TSource> sourceList)
        {
            var count = sourceList.Count;
            if (count > 0)
            {
                return sourceList[count - 1];
            }
        }
        else
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    TSource current;
                    do
                    {
                        current = enumerator.Current;
                    } while (enumerator.MoveNext());

                    return current;
                }
            }
        }

        return default;
    }

    /// <summary>
    ///     Returns the last element of a sequence that satisfies a condition or a default value if no such element is
    ///     found.
    /// </summary>
    /// <returns>
    ///     default(<paramref name="TSource" />) if the sequence is empty or if no elements pass the test in the predicate
    ///     function; otherwise, the last element that passes the test in the predicate function.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return an element from.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    public static TSource LastOrDefault<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (predicate == null)
        {
            throw Error.ArgumentNull(nameof(predicate));
        }

        var source1 = default(TSource);
        foreach (var source2 in source)
        {
            if (predicate(source2))
            {
                source1 = source2;
            }
        }

        return source1;
    }

    /// <summary>Returns an <see cref="T:System.Int64" /> that represents the total number of elements in a sequence.</summary>
    /// <returns>The number of elements in the source sequence.</returns>
    /// <param name="source">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the elements to be
    ///     counted.
    /// </param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The number of elements exceeds <see cref="F:System.Int64.MaxValue" />.</exception>
    public static long LongCount<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        long num = 0;
        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                checked
                {
                    ++num;
                }
            }
        }

        return num;
    }

    /// <summary>Returns an <see cref="T:System.Int64" /> that represents how many elements in a sequence satisfy a condition.</summary>
    /// <returns>A number that represents how many elements in the sequence satisfy the condition in the predicate function.</returns>
    /// <param name="source">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the elements to be
    ///     counted.
    /// </param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">
    ///     The number of matching elements exceeds
    ///     <see cref="F:System.Int64.MaxValue" />.
    /// </exception>
    public static long LongCount<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (predicate == null)
        {
            throw Error.ArgumentNull(nameof(predicate));
        }

        long num = 0;
        foreach (var source1 in source)
        {
            if (predicate(source1))
            {
                checked
                {
                    ++num;
                }
            }
        }

        return num;
    }

    /// <summary>Returns the maximum value in a sequence of <see cref="T:System.Int32" /> values.</summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Int32" /> values to determine the maximum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static int Max(this IEnumerable<int> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0;
        var flag = false;
        foreach (var num2 in source)
        {
            if (flag)
            {
                if (num2 > num1)
                {
                    num1 = num2;
                }
            }
            else
            {
                num1 = num2;
                flag = true;
            }
        }

        if (flag)
        {
            return num1;
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the maximum value in a sequence of nullable <see cref="T:System.Int32" /> values.</summary>
    /// <returns>
    ///     A value of type Nullable&lt;Int32&gt; in C# or Nullable(Of Int32) in Visual Basic that corresponds to the
    ///     maximum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Int32" /> values to determine the maximum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static int? Max(this IEnumerable<int?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var nullable1 = new int?();
        foreach (var nullable2 in source)
        {
            if (nullable1.HasValue)
            {
                var nullable3 = nullable2;
                var nullable4 = nullable1;
                if (!((nullable3.GetValueOrDefault() > nullable4.GetValueOrDefault()) & nullable3.HasValue &
                      nullable4.HasValue))
                {
                    continue;
                }
            }

            nullable1 = nullable2;
        }

        return nullable1;
    }

    /// <summary>Returns the maximum value in a sequence of <see cref="T:System.Int64" /> values.</summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Int64" /> values to determine the maximum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static long Max(this IEnumerable<long> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        long num1 = 0;
        var flag = false;
        foreach (var num2 in source)
        {
            if (flag)
            {
                if (num2 > num1)
                {
                    num1 = num2;
                }
            }
            else
            {
                num1 = num2;
                flag = true;
            }
        }

        if (flag)
        {
            return num1;
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the maximum value in a sequence of nullable <see cref="T:System.Int64" /> values.</summary>
    /// <returns>
    ///     A value of type Nullable&lt;Int64&gt; in C# or Nullable(Of Int64) in Visual Basic that corresponds to the
    ///     maximum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Int64" /> values to determine the maximum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static long? Max(this IEnumerable<long?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var nullable1 = new long?();
        foreach (var nullable2 in source)
        {
            if (nullable1.HasValue)
            {
                var nullable3 = nullable2;
                var nullable4 = nullable1;
                if (!((nullable3.GetValueOrDefault() > nullable4.GetValueOrDefault()) & nullable3.HasValue &
                      nullable4.HasValue))
                {
                    continue;
                }
            }

            nullable1 = nullable2;
        }

        return nullable1;
    }

    /// <summary>Returns the maximum value in a sequence of <see cref="T:System.Double" /> values.</summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Double" /> values to determine the maximum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static double Max(this IEnumerable<double> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var d = 0.0;
        var flag = false;
        foreach (var num in source)
        {
            if (flag)
            {
                if (num > d || double.IsNaN(d))
                {
                    d = num;
                }
            }
            else
            {
                d = num;
                flag = true;
            }
        }

        if (flag)
        {
            return d;
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the maximum value in a sequence of nullable <see cref="T:System.Double" /> values.</summary>
    /// <returns>
    ///     A value of type Nullable&lt;Double&gt; in C# or Nullable(Of Double) in Visual Basic that corresponds to the
    ///     maximum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Double" /> values to determine the maximum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static double? Max(this IEnumerable<double?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var nullable1 = new double?();
        foreach (var nullable2 in source)
        {
            if (nullable2.HasValue)
            {
                if (nullable1.HasValue)
                {
                    var nullable3 = nullable2;
                    var nullable4 = nullable1;
                    if (!((nullable3.GetValueOrDefault() > nullable4.GetValueOrDefault()) & nullable3.HasValue &
                          nullable4.HasValue) && !double.IsNaN(nullable1.Value))
                    {
                        continue;
                    }
                }

                nullable1 = nullable2;
            }
        }

        return nullable1;
    }

    /// <summary>Returns the maximum value in a sequence of <see cref="T:System.Single" /> values.</summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Single" /> values to determine the maximum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static float Max(this IEnumerable<float> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var d = 0.0f;
        var flag = false;
        foreach (var num in source)
        {
            if (flag)
            {
                if (num > (double)d || double.IsNaN(d))
                {
                    d = num;
                }
            }
            else
            {
                d = num;
                flag = true;
            }
        }

        if (flag)
        {
            return d;
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the maximum value in a sequence of nullable <see cref="T:System.Single" /> values.</summary>
    /// <returns>
    ///     A value of type Nullable&lt;Single&gt; in C# or Nullable(Of Single) in Visual Basic that corresponds to the
    ///     maximum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Single" /> values to determine the maximum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static float? Max(this IEnumerable<float?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var nullable1 = new float?();
        foreach (var nullable2 in source)
        {
            if (nullable2.HasValue)
            {
                if (nullable1.HasValue)
                {
                    var nullable3 = nullable2;
                    var nullable4 = nullable1;
                    if (!((nullable3.GetValueOrDefault() > (double)nullable4.GetValueOrDefault()) & nullable3.HasValue &
                          nullable4.HasValue) && !float.IsNaN(nullable1.Value))
                    {
                        continue;
                    }
                }

                nullable1 = nullable2;
            }
        }

        return nullable1;
    }

    /// <summary>Returns the maximum value in a sequence of <see cref="T:System.Decimal" /> values.</summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Decimal" /> values to determine the maximum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static decimal Max(this IEnumerable<decimal> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0M;
        var flag = false;
        foreach (var num2 in source)
        {
            if (flag)
            {
                if (num2 > num1)
                {
                    num1 = num2;
                }
            }
            else
            {
                num1 = num2;
                flag = true;
            }
        }

        if (flag)
        {
            return num1;
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the maximum value in a sequence of nullable <see cref="T:System.Decimal" /> values.</summary>
    /// <returns>
    ///     A value of type Nullable&lt;Decimal&gt; in C# or Nullable(Of Decimal) in Visual Basic that corresponds to the
    ///     maximum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Decimal" /> values to determine the maximum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static decimal? Max(this IEnumerable<decimal?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var nullable1 = new decimal?();
        foreach (var nullable2 in source)
        {
            if (nullable1.HasValue)
            {
                var nullable3 = nullable2;
                var nullable4 = nullable1;
                if (!((nullable3.GetValueOrDefault() > nullable4.GetValueOrDefault()) & nullable3.HasValue &
                      nullable4.HasValue))
                {
                    continue;
                }
            }

            nullable1 = nullable2;
        }

        return nullable1;
    }

    /// <summary>Returns the maximum value in a generic sequence.</summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static TSource Max<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var comparer = Comparer<TSource>.Default;
        var y = default(TSource);
        if (y == null)
        {
            foreach (var x in source)
            {
                if (x != null && (y == null || comparer.Compare(x, y) > 0))
                {
                    y = x;
                }
            }

            return y;
        }

        var flag = false;
        foreach (var x in source)
        {
            if (flag)
            {
                if (comparer.Compare(x, y) > 0)
                {
                    y = x;
                }
            }
            else
            {
                y = x;
                flag = true;
            }
        }

        if (flag)
        {
            return y;
        }

        throw Error.NoElements();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the maximum
    ///     <see cref="T:System.Int32" /> value.
    /// </summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static int Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
    {
        return source.Select(selector).Max();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the maximum nullable
    ///     <see cref="T:System.Int32" /> value.
    /// </summary>
    /// <returns>
    ///     The value of type Nullable&lt;Int32&gt; in C# or Nullable(Of Int32) in Visual Basic that corresponds to the
    ///     maximum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static int? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
    {
        return source.Select(selector).Max();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the maximum
    ///     <see cref="T:System.Int64" /> value.
    /// </summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static long Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
    {
        return source.Select(selector).Max();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the maximum nullable
    ///     <see cref="T:System.Int64" /> value.
    /// </summary>
    /// <returns>
    ///     The value of type Nullable&lt;Int64&gt; in C# or Nullable(Of Int64) in Visual Basic that corresponds to the
    ///     maximum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static long? Max<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
    {
        return source.Select(selector).Max();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the maximum
    ///     <see cref="T:System.Single" /> value.
    /// </summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static float Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
    {
        return source.Select(selector).Max();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the maximum nullable
    ///     <see cref="T:System.Single" /> value.
    /// </summary>
    /// <returns>
    ///     The value of type Nullable&lt;Single&gt; in C# or Nullable(Of Single) in Visual Basic that corresponds to the
    ///     maximum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static float? Max<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, float?> selector)
    {
        return source.Select(selector).Max();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the maximum
    ///     <see cref="T:System.Double" /> value.
    /// </summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static double Max<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, double> selector)
    {
        return source.Select(selector).Max();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the maximum nullable
    ///     <see cref="T:System.Double" /> value.
    /// </summary>
    /// <returns>
    ///     The value of type Nullable&lt;Double&gt; in C# or Nullable(Of Double) in Visual Basic that corresponds to the
    ///     maximum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static double? Max<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, double?> selector)
    {
        return source.Select(selector).Max();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the maximum
    ///     <see cref="T:System.Decimal" /> value.
    /// </summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static decimal Max<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, decimal> selector)
    {
        return source.Select(selector).Max();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the maximum nullable
    ///     <see cref="T:System.Decimal" /> value.
    /// </summary>
    /// <returns>
    ///     The value of type Nullable&lt;Decimal&gt; in C# or Nullable(Of Decimal) in Visual Basic that corresponds to
    ///     the maximum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static decimal? Max<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, decimal?> selector)
    {
        return source.Select(selector).Max();
    }

    /// <summary>Invokes a transform function on each element of a generic sequence and returns the maximum resulting value.</summary>
    /// <returns>The maximum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the maximum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static TResult Max<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TResult> selector)
    {
        return source.Select(selector).Max();
    }

    /// <summary>Returns the minimum value in a sequence of <see cref="T:System.Int32" /> values.</summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Int32" /> values to determine the minimum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static int Min(this IEnumerable<int> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0;
        var flag = false;
        foreach (var num2 in source)
        {
            if (flag)
            {
                if (num2 < num1)
                {
                    num1 = num2;
                }
            }
            else
            {
                num1 = num2;
                flag = true;
            }
        }

        if (flag)
        {
            return num1;
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the minimum value in a sequence of nullable <see cref="T:System.Int32" /> values.</summary>
    /// <returns>
    ///     A value of type Nullable&lt;Int32&gt; in C# or Nullable(Of Int32) in Visual Basic that corresponds to the
    ///     minimum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Int32" /> values to determine the minimum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static int? Min(this IEnumerable<int?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var nullable1 = new int?();
        foreach (var nullable2 in source)
        {
            if (nullable1.HasValue)
            {
                var nullable3 = nullable2;
                var nullable4 = nullable1;
                if (!((nullable3.GetValueOrDefault() < nullable4.GetValueOrDefault()) & nullable3.HasValue &
                      nullable4.HasValue))
                {
                    continue;
                }
            }

            nullable1 = nullable2;
        }

        return nullable1;
    }

    /// <summary>Returns the minimum value in a sequence of <see cref="T:System.Int64" /> values.</summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Int64" /> values to determine the minimum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static long Min(this IEnumerable<long> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        long num1 = 0;
        var flag = false;
        foreach (var num2 in source)
        {
            if (flag)
            {
                if (num2 < num1)
                {
                    num1 = num2;
                }
            }
            else
            {
                num1 = num2;
                flag = true;
            }
        }

        if (flag)
        {
            return num1;
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the minimum value in a sequence of nullable <see cref="T:System.Int64" /> values.</summary>
    /// <returns>
    ///     A value of type Nullable&lt;Int64&gt; in C# or Nullable(Of Int64) in Visual Basic that corresponds to the
    ///     minimum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Int64" /> values to determine the minimum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static long? Min(this IEnumerable<long?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var nullable1 = new long?();
        foreach (var nullable2 in source)
        {
            if (nullable1.HasValue)
            {
                var nullable3 = nullable2;
                var nullable4 = nullable1;
                if (!((nullable3.GetValueOrDefault() < nullable4.GetValueOrDefault()) & nullable3.HasValue &
                      nullable4.HasValue))
                {
                    continue;
                }
            }

            nullable1 = nullable2;
        }

        return nullable1;
    }

    /// <summary>Returns the minimum value in a sequence of <see cref="T:System.Single" /> values.</summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Single" /> values to determine the minimum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static float Min(this IEnumerable<float> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num = 0.0f;
        var flag = false;
        foreach (var f in source)
        {
            if (flag)
            {
                if (f < (double)num || float.IsNaN(f))
                {
                    num = f;
                }
            }
            else
            {
                num = f;
                flag = true;
            }
        }

        if (flag)
        {
            return num;
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the minimum value in a sequence of nullable <see cref="T:System.Single" /> values.</summary>
    /// <returns>
    ///     A value of type Nullable&lt;Single&gt; in C# or Nullable(Of Single) in Visual Basic that corresponds to the
    ///     minimum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Single" /> values to determine the minimum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static float? Min(this IEnumerable<float?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var nullable1 = new float?();
        foreach (var nullable2 in source)
        {
            if (nullable2.HasValue)
            {
                if (nullable1.HasValue)
                {
                    var nullable3 = nullable2;
                    var nullable4 = nullable1;
                    if (!((nullable3.GetValueOrDefault() < (double)nullable4.GetValueOrDefault()) & nullable3.HasValue &
                          nullable4.HasValue) && !float.IsNaN(nullable2.Value))
                    {
                        continue;
                    }
                }

                nullable1 = nullable2;
            }
        }

        return nullable1;
    }

    /// <summary>Returns the minimum value in a sequence of <see cref="T:System.Double" /> values.</summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Double" /> values to determine the minimum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static double Min(this IEnumerable<double> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num = 0.0;
        var flag = false;
        foreach (var d in source)
        {
            if (flag)
            {
                if (d < num || double.IsNaN(d))
                {
                    num = d;
                }
            }
            else
            {
                num = d;
                flag = true;
            }
        }

        if (flag)
        {
            return num;
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the minimum value in a sequence of nullable <see cref="T:System.Double" /> values.</summary>
    /// <returns>
    ///     A value of type Nullable&lt;Double&gt; in C# or Nullable(Of Double) in Visual Basic that corresponds to the
    ///     minimum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Double" /> values to determine the minimum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static double? Min(this IEnumerable<double?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var nullable1 = new double?();
        foreach (var nullable2 in source)
        {
            if (nullable2.HasValue)
            {
                if (nullable1.HasValue)
                {
                    var nullable3 = nullable2;
                    var nullable4 = nullable1;
                    if (!((nullable3.GetValueOrDefault() < nullable4.GetValueOrDefault()) & nullable3.HasValue &
                          nullable4.HasValue) && !double.IsNaN(nullable2.Value))
                    {
                        continue;
                    }
                }

                nullable1 = nullable2;
            }
        }

        return nullable1;
    }

    /// <summary>Returns the minimum value in a sequence of <see cref="T:System.Decimal" /> values.</summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Decimal" /> values to determine the minimum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static decimal Min(this IEnumerable<decimal> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0M;
        var flag = false;
        foreach (var num2 in source)
        {
            if (flag)
            {
                if (num2 < num1)
                {
                    num1 = num2;
                }
            }
            else
            {
                num1 = num2;
                flag = true;
            }
        }

        if (flag)
        {
            return num1;
        }

        throw Error.NoElements();
    }

    /// <summary>Returns the minimum value in a sequence of nullable <see cref="T:System.Decimal" /> values.</summary>
    /// <returns>
    ///     A value of type Nullable&lt;Decimal&gt; in C# or Nullable(Of Decimal) in Visual Basic that corresponds to the
    ///     minimum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Decimal" /> values to determine the minimum value of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static decimal? Min(this IEnumerable<decimal?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var nullable1 = new decimal?();
        foreach (var nullable2 in source)
        {
            if (nullable1.HasValue)
            {
                var nullable3 = nullable2;
                var nullable4 = nullable1;
                if (!((nullable3.GetValueOrDefault() < nullable4.GetValueOrDefault()) & nullable3.HasValue &
                      nullable4.HasValue))
                {
                    continue;
                }
            }

            nullable1 = nullable2;
        }

        return nullable1;
    }

    /// <summary>Returns the minimum value in a generic sequence.</summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static TSource Min<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var comparer = Comparer<TSource>.Default;
        var y = default(TSource);
        if (y == null)
        {
            foreach (var x in source)
            {
                if (x != null && (y == null || comparer.Compare(x, y) < 0))
                {
                    y = x;
                }
            }

            return y;
        }

        var flag = false;
        foreach (var x in source)
        {
            if (flag)
            {
                if (comparer.Compare(x, y) < 0)
                {
                    y = x;
                }
            }
            else
            {
                y = x;
                flag = true;
            }
        }

        if (flag)
        {
            return y;
        }

        throw Error.NoElements();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the minimum
    ///     <see cref="T:System.Int32" /> value.
    /// </summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static int Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
    {
        return source.Select(selector).Min();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the minimum nullable
    ///     <see cref="T:System.Int32" /> value.
    /// </summary>
    /// <returns>
    ///     The value of type Nullable&lt;Int32&gt; in C# or Nullable(Of Int32) in Visual Basic that corresponds to the
    ///     minimum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static int? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
    {
        return source.Select(selector).Min();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the minimum
    ///     <see cref="T:System.Int64" /> value.
    /// </summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static long Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
    {
        return source.Select(selector).Min();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the minimum nullable
    ///     <see cref="T:System.Int64" /> value.
    /// </summary>
    /// <returns>
    ///     The value of type Nullable&lt;Int64&gt; in C# or Nullable(Of Int64) in Visual Basic that corresponds to the
    ///     minimum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static long? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
    {
        return source.Select(selector).Min();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the minimum
    ///     <see cref="T:System.Single" /> value.
    /// </summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static float Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
    {
        return source.Select(selector).Min();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the minimum nullable
    ///     <see cref="T:System.Single" /> value.
    /// </summary>
    /// <returns>
    ///     The value of type Nullable&lt;Single&gt; in C# or Nullable(Of Single) in Visual Basic that corresponds to the
    ///     minimum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static float? Min<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, float?> selector)
    {
        return source.Select(selector).Min();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the minimum
    ///     <see cref="T:System.Double" /> value.
    /// </summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static double Min<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, double> selector)
    {
        return source.Select(selector).Min();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the minimum nullable
    ///     <see cref="T:System.Double" /> value.
    /// </summary>
    /// <returns>
    ///     The value of type Nullable&lt;Double&gt; in C# or Nullable(Of Double) in Visual Basic that corresponds to the
    ///     minimum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static double? Min<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, double?> selector)
    {
        return source.Select(selector).Min();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the minimum
    ///     <see cref="T:System.Decimal" /> value.
    /// </summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="source" /> contains no elements.
    /// </exception>
    public static decimal Min<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, decimal> selector)
    {
        return source.Select(selector).Min();
    }

    /// <summary>
    ///     Invokes a transform function on each element of a sequence and returns the minimum nullable
    ///     <see cref="T:System.Decimal" /> value.
    /// </summary>
    /// <returns>
    ///     The value of type Nullable&lt;Decimal&gt; in C# or Nullable(Of Decimal) in Visual Basic that corresponds to
    ///     the minimum value in the sequence.
    /// </returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static decimal? Min<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, decimal?> selector)
    {
        return source.Select(selector).Min();
    }

    /// <summary>Invokes a transform function on each element of a generic sequence and returns the minimum resulting value.</summary>
    /// <returns>The minimum value in the sequence.</returns>
    /// <param name="source">A sequence of values to determine the minimum value of.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static TResult Min<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TResult> selector)
    {
        return source.Select(selector).Min();
    }

    /// <summary>Filters the elements of an <see cref="T:System.Collections.IEnumerable" /> based on a specified type.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains elements from the input sequence of
    ///     type <paramref name="TResult" />.
    /// </returns>
    /// <param name="source">The <see cref="T:System.Collections.IEnumerable" /> whose elements to filter.</param>
    /// <typeparam name="TResult">The type to filter the elements of the sequence on.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
    {
        return source != null ? OfTypeIterator<TResult>(source) : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>Sorts the elements of a sequence in ascending order according to a key.</summary>
    /// <returns>An <see cref="T:System.Linq.IOrderedEnumerable`1" /> whose elements are sorted according to a key.</returns>
    /// <param name="source">A sequence of values to order.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return new OrderedEnumerable<TSource, TKey>(source, keySelector, null, false);
    }

    /// <summary>Sorts the elements of a sequence in ascending order by using a specified comparer.</summary>
    /// <returns>An <see cref="T:System.Linq.IOrderedEnumerable`1" /> whose elements are sorted according to a key.</returns>
    /// <param name="source">A sequence of values to order.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IComparer`1" /> to compare keys.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey> comparer)
    {
        return new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, false);
    }

    /// <summary>Sorts the elements of a sequence in descending order according to a key.</summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.IOrderedEnumerable`1" /> whose elements are sorted in descending order according
    ///     to a key.
    /// </returns>
    /// <param name="source">A sequence of values to order.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return new OrderedEnumerable<TSource, TKey>(source, keySelector, null, true);
    }

    /// <summary>Sorts the elements of a sequence in descending order by using a specified comparer.</summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.IOrderedEnumerable`1" /> whose elements are sorted in descending order according
    ///     to a key.
    /// </returns>
    /// <param name="source">A sequence of values to order.</param>
    /// <param name="keySelector">A function to extract a key from an element.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IComparer`1" /> to compare keys.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey> comparer)
    {
        return new OrderedEnumerable<TSource, TKey>(source, keySelector, comparer, true);
    }

    public static IEnumerable<TSource> Prepend<TSource>(
        this IEnumerable<TSource> source,
        TSource element)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return source is AppendPrependIterator<TSource> appendPrependIterator
            ? appendPrependIterator.Prepend(element)
            : (IEnumerable<TSource>)new AppendPrepend1Iterator<TSource>(source, element, false);
    }

    /// <summary>Generates a sequence of integral numbers within a specified range.</summary>
    /// <returns>
    ///     An IEnumerable&lt;Int32&gt; in C# or IEnumerable(Of Int32) in Visual Basic that contains a range of sequential
    ///     integral numbers.
    /// </returns>
    /// <param name="start">The value of the first integer in the sequence.</param>
    /// <param name="count">The number of sequential integers to generate.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///     <paramref name="count" /> is less than 0.-or-<paramref name="start" /> + <paramref name="count" /> -1 is larger
    ///     than <see cref="F:System.Int32.MaxValue" />.
    /// </exception>
    public static IEnumerable<int> Range(int start, int count)
    {
        var num = start + (long)count - 1L;
        return count >= 0 && num <= int.MaxValue
            ? RangeIterator(start, count)
            : throw Error.ArgumentOutOfRange(nameof(count));
    }

    /// <summary>Generates a sequence that contains one repeated value.</summary>
    /// <returns>An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains a repeated value.</returns>
    /// <param name="element">The value to be repeated.</param>
    /// <param name="count">The number of times to repeat the value in the generated sequence.</param>
    /// <typeparam name="TResult">The type of the value to be repeated in the result sequence.</typeparam>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    ///     <paramref name="count" /> is less than 0.
    /// </exception>
    public static IEnumerable<TResult> Repeat<TResult>(TResult element, int count)
    {
        return count >= 0 ? RepeatIterator(element, count) : throw Error.ArgumentOutOfRange(nameof(count));
    }

    /// <summary>Inverts the order of the elements in a sequence.</summary>
    /// <returns>A sequence whose elements correspond to those of the input sequence in reverse order.</returns>
    /// <param name="source">A sequence of values to reverse.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
    {
        return source != null ? ReverseIterator(source) : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>Projects each element of a sequence into a new form.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements are the result of invoking the
    ///     transform function on each element of <paramref name="source" />.
    /// </returns>
    /// <param name="source">A sequence of values to invoke a transform function on.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static IEnumerable<TResult> Select<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TResult> selector)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (selector == null)
        {
            throw Error.ArgumentNull(nameof(selector));
        }

        switch (source)
        {
            case Iterator<TSource> _:
                return ((Iterator<TSource>)source).Select(selector);
            case TSource[] _:
                return new WhereSelectArrayIterator<TSource, TResult>((TSource[])source, null, selector);
            case List<TSource> _:
                return new WhereSelectListIterator<TSource, TResult>((List<TSource>)source, null, selector);
            default:
                return new WhereSelectEnumerableIterator<TSource, TResult>(source, null, selector);
        }
    }

    /// <summary>Projects each element of a sequence into a new form by incorporating the element's index.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements are the result of invoking the
    ///     transform function on each element of <paramref name="source" />.
    /// </returns>
    /// <param name="source">A sequence of values to invoke a transform function on.</param>
    /// <param name="selector">
    ///     A transform function to apply to each source element; the second parameter of the function
    ///     represents the index of the source element.
    /// </param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by <paramref name="selector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static IEnumerable<TResult> Select<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, int, TResult> selector)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return selector != null ? SelectIterator(source, selector) : throw Error.ArgumentNull(nameof(selector));
    }

    /// <summary>
    ///     Projects each element of a sequence to an <see cref="T:System.Collections.Generic.IEnumerable`1" /> and
    ///     flattens the resulting sequences into one sequence.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements are the result of invoking the
    ///     one-to-many transform function on each element of the input sequence.
    /// </returns>
    /// <param name="source">A sequence of values to project.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TResult">The type of the elements of the sequence returned by <paramref name="selector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static IEnumerable<TResult> SelectMany<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, IEnumerable<TResult>> selector)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return selector != null ? SelectManyIterator(source, selector) : throw Error.ArgumentNull(nameof(selector));
    }

    /// <summary>
    ///     Projects each element of a sequence to an <see cref="T:System.Collections.Generic.IEnumerable`1" />, and
    ///     flattens the resulting sequences into one sequence. The index of each source element is used in the projected form
    ///     of that element.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements are the result of invoking the
    ///     one-to-many transform function on each element of an input sequence.
    /// </returns>
    /// <param name="source">A sequence of values to project.</param>
    /// <param name="selector">
    ///     A transform function to apply to each source element; the second parameter of the function
    ///     represents the index of the source element.
    /// </param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TResult">The type of the elements of the sequence returned by <paramref name="selector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static IEnumerable<TResult> SelectMany<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, int, IEnumerable<TResult>> selector)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return selector != null ? SelectManyIterator(source, selector) : throw Error.ArgumentNull(nameof(selector));
    }

    /// <summary>
    ///     Projects each element of a sequence to an <see cref="T:System.Collections.Generic.IEnumerable`1" />, flattens
    ///     the resulting sequences into one sequence, and invokes a result selector function on each element therein. The
    ///     index of each source element is used in the intermediate projected form of that element.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements are the result of invoking the
    ///     one-to-many transform function <paramref name="collectionSelector" /> on each element of <paramref name="source" />
    ///     and then mapping each of those sequence elements and their corresponding source element to a result element.
    /// </returns>
    /// <param name="source">A sequence of values to project.</param>
    /// <param name="collectionSelector">
    ///     A transform function to apply to each source element; the second parameter of the
    ///     function represents the index of the source element.
    /// </param>
    /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TCollection">
    ///     The type of the intermediate elements collected by <paramref name="collectionSelector" />
    ///     .
    /// </typeparam>
    /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="collectionSelector" /> or <paramref name="resultSelector" /> is null.
    /// </exception>
    public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, int, IEnumerable<TCollection>> collectionSelector,
        Func<TSource, TCollection, TResult> resultSelector)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (collectionSelector == null)
        {
            throw Error.ArgumentNull(nameof(collectionSelector));
        }

        if (resultSelector == null)
        {
            throw Error.ArgumentNull(nameof(resultSelector));
        }

        return SelectManyIterator(source, collectionSelector, resultSelector);
    }

    /// <summary>
    ///     Projects each element of a sequence to an <see cref="T:System.Collections.Generic.IEnumerable`1" />, flattens
    ///     the resulting sequences into one sequence, and invokes a result selector function on each element therein.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose elements are the result of invoking the
    ///     one-to-many transform function <paramref name="collectionSelector" /> on each element of <paramref name="source" />
    ///     and then mapping each of those sequence elements and their corresponding source element to a result element.
    /// </returns>
    /// <param name="source">A sequence of values to project.</param>
    /// <param name="collectionSelector">A transform function to apply to each element of the input sequence.</param>
    /// <param name="resultSelector">A transform function to apply to each element of the intermediate sequence.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TCollection">
    ///     The type of the intermediate elements collected by <paramref name="collectionSelector" />
    ///     .
    /// </typeparam>
    /// <typeparam name="TResult">The type of the elements of the resulting sequence.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="collectionSelector" /> or <paramref name="resultSelector" /> is null.
    /// </exception>
    public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, IEnumerable<TCollection>> collectionSelector,
        Func<TSource, TCollection, TResult> resultSelector)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (collectionSelector == null)
        {
            throw Error.ArgumentNull(nameof(collectionSelector));
        }

        if (resultSelector == null)
        {
            throw Error.ArgumentNull(nameof(resultSelector));
        }

        return SelectManyIterator(source, collectionSelector, resultSelector);
    }

    /// <summary>
    ///     Determines whether two sequences are equal by comparing the elements by using the default equality comparer
    ///     for their type.
    /// </summary>
    /// <returns>
    ///     true if the two source sequences are of equal length and their corresponding elements are equal according to
    ///     the default equality comparer for their type; otherwise, false.
    /// </returns>
    /// <param name="first">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to compare to
    ///     <paramref name="second" />.
    /// </param>
    /// <param name="second">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to compare to the first sequence.</param>
    /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="first" /> or <paramref name="second" /> is null.
    /// </exception>
    public static bool SequenceEqual<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second)
    {
        return first.SequenceEqual(second, null);
    }

    /// <summary>
    ///     Determines whether two sequences are equal by comparing their elements by using a specified
    ///     <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.
    /// </summary>
    /// <returns>
    ///     true if the two source sequences are of equal length and their corresponding elements compare equal according
    ///     to <paramref name="comparer" />; otherwise, false.
    /// </returns>
    /// <param name="first">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to compare to
    ///     <paramref name="second" />.
    /// </param>
    /// <param name="second">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to compare to the first sequence.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to use to compare elements.</param>
    /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="first" /> or <paramref name="second" /> is null.
    /// </exception>
    public static bool SequenceEqual<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second,
        IEqualityComparer<TSource> comparer)
    {
        if (comparer == null)
        {
            comparer = EqualityComparer<TSource>.Default;
        }

        if (first == null)
        {
            throw Error.ArgumentNull(nameof(first));
        }

        if (second == null)
        {
            throw Error.ArgumentNull(nameof(second));
        }

        using (var enumerator1 = first.GetEnumerator())
        {
            using (var enumerator2 = second.GetEnumerator())
            {
                while (enumerator1.MoveNext())
                {
                    if (!enumerator2.MoveNext() || !comparer.Equals(enumerator1.Current, enumerator2.Current))
                    {
                        return false;
                    }
                }

                if (enumerator2.MoveNext())
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    ///     Returns the only element of a sequence, and throws an exception if there is not exactly one element in the
    ///     sequence.
    /// </summary>
    /// <returns>The single element of the input sequence.</returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return the single element of.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The input sequence contains more than one element.-or-The input
    ///     sequence is empty.
    /// </exception>
    public static TSource Single<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (source is IList<TSource> sourceList)
        {
            switch (sourceList.Count)
            {
                case 0:
                    throw Error.NoElements();
                case 1:
                    return sourceList[0];
            }
        }
        else
        {
            using (var enumerator = source.GetEnumerator())
            {
                var source1 = enumerator.MoveNext() ? enumerator.Current : throw Error.NoElements();
                if (!enumerator.MoveNext())
                {
                    return source1;
                }
            }
        }

        throw Error.MoreThanOneElement();
    }

    /// <summary>
    ///     Returns the only element of a sequence that satisfies a specified condition, and throws an exception if more
    ///     than one such element exists.
    /// </summary>
    /// <returns>The single element of the input sequence that satisfies a condition.</returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return a single element from.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     No element satisfies the condition in
    ///     <paramref name="predicate" />.-or-More than one element satisfies the condition in <paramref name="predicate" />
    ///     .-or-The source sequence is empty.
    /// </exception>
    public static TSource Single<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (predicate == null)
        {
            throw Error.ArgumentNull(nameof(predicate));
        }

        var source1 = default(TSource);
        long num = 0;
        foreach (var source2 in source)
        {
            if (predicate(source2))
            {
                source1 = source2;
                checked
                {
                    ++num;
                }
            }
        }

        if (num == 0L)
        {
            throw Error.NoMatch();
        }

        if (num == 1L)
        {
            return source1;
        }

        throw Error.MoreThanOneMatch();
    }

    /// <summary>
    ///     Returns the only element of a sequence, or a default value if the sequence is empty; this method throws an
    ///     exception if there is more than one element in the sequence.
    /// </summary>
    /// <returns>
    ///     The single element of the input sequence, or default(<paramref name="TSource" />) if the sequence contains no
    ///     elements.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return the single element of.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">The input sequence contains more than one element.</exception>
    public static TSource SingleOrDefault<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (source is IList<TSource> sourceList)
        {
            switch (sourceList.Count)
            {
                case 0:
                    return default;
                case 1:
                    return sourceList[0];
            }
        }
        else
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    return default;
                }

                var current = enumerator.Current;
                if (!enumerator.MoveNext())
                {
                    return current;
                }
            }
        }

        throw Error.MoreThanOneElement();
    }

    /// <summary>
    ///     Returns the only element of a sequence that satisfies a specified condition or a default value if no such
    ///     element exists; this method throws an exception if more than one element satisfies the condition.
    /// </summary>
    /// <returns>
    ///     The single element of the input sequence that satisfies the condition, or default(<paramref name="TSource" />)
    ///     if no such element is found.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return a single element from.</param>
    /// <param name="predicate">A function to test an element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    public static TSource SingleOrDefault<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (predicate == null)
        {
            throw Error.ArgumentNull(nameof(predicate));
        }

        var source1 = default(TSource);
        long num = 0;
        foreach (var source2 in source)
        {
            if (predicate(source2))
            {
                source1 = source2;
                checked
                {
                    ++num;
                }
            }
        }

        if (num == 0L)
        {
            return default;
        }

        if (num == 1L)
        {
            return source1;
        }

        throw Error.MoreThanOneMatch();
    }

    /// <summary>Bypasses a specified number of elements in a sequence and then returns the remaining elements.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the elements that occur after the
    ///     specified index in the input sequence.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return elements from.</param>
    /// <param name="count">The number of elements to skip before returning the remaining elements.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Skip<TSource>(this IEnumerable<TSource> source, int count)
    {
        return source != null ? SkipIterator(source, count) : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>
    ///     Bypasses elements in a sequence as long as a specified condition is true and then returns the remaining
    ///     elements.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the elements from the input
    ///     sequence starting at the first element in the linear series that does not pass the test specified by
    ///     <paramref name="predicate" />.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return elements from.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    public static IEnumerable<TSource> SkipWhile<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return predicate != null ? SkipWhileIterator(source, predicate) : throw Error.ArgumentNull(nameof(predicate));
    }

    /// <summary>
    ///     Bypasses elements in a sequence as long as a specified condition is true and then returns the remaining
    ///     elements. The element's index is used in the logic of the predicate function.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the elements from the input
    ///     sequence starting at the first element in the linear series that does not pass the test specified by
    ///     <paramref name="predicate" />.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to return elements from.</param>
    /// <param name="predicate">
    ///     A function to test each source element for a condition; the second parameter of the function
    ///     represents the index of the source element.
    /// </param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    public static IEnumerable<TSource> SkipWhile<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, int, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return predicate != null ? SkipWhileIterator(source, predicate) : throw Error.ArgumentNull(nameof(predicate));
    }

    /// <summary>Computes the sum of a sequence of <see cref="T:System.Int32" /> values.</summary>
    /// <returns>The sum of the values in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Int32" /> values to calculate the sum of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
    public static int Sum(this IEnumerable<int> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0;
        foreach (var num2 in source)
        {
            checked
            {
                num1 += num2;
            }
        }

        return num1;
    }

    /// <summary>Computes the sum of a sequence of nullable <see cref="T:System.Int32" /> values.</summary>
    /// <returns>The sum of the values in the sequence.</returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Int32" /> values to calculate the sum of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
    public static int? Sum(this IEnumerable<int?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num = 0;
        foreach (var nullable in source)
        {
            if (nullable.HasValue)
            {
                checked
                {
                    num += nullable.GetValueOrDefault();
                }
            }
        }

        return num;
    }

    /// <summary>Computes the sum of a sequence of <see cref="T:System.Int64" /> values.</summary>
    /// <returns>The sum of the values in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Int64" /> values to calculate the sum of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Int64.MaxValue" />.</exception>
    public static long Sum(this IEnumerable<long> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        long num1 = 0;
        foreach (var num2 in source)
        {
            checked
            {
                num1 += num2;
            }
        }

        return num1;
    }

    /// <summary>Computes the sum of a sequence of nullable <see cref="T:System.Int64" /> values.</summary>
    /// <returns>The sum of the values in the sequence.</returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Int64" /> values to calculate the sum of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Int64.MaxValue" />.</exception>
    public static long? Sum(this IEnumerable<long?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        long num = 0;
        foreach (var nullable in source)
        {
            if (nullable.HasValue)
            {
                checked
                {
                    num += nullable.GetValueOrDefault();
                }
            }
        }

        return num;
    }

    /// <summary>Computes the sum of a sequence of <see cref="T:System.Single" /> values.</summary>
    /// <returns>The sum of the values in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Single" /> values to calculate the sum of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static float Sum(this IEnumerable<float> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0.0;
        foreach (var num2 in source)
        {
            num1 += num2;
        }

        return (float)num1;
    }

    /// <summary>Computes the sum of a sequence of nullable <see cref="T:System.Single" /> values.</summary>
    /// <returns>The sum of the values in the sequence.</returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Single" /> values to calculate the sum of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static float? Sum(this IEnumerable<float?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num = 0.0;
        foreach (var nullable in source)
        {
            if (nullable.HasValue)
            {
                num += nullable.GetValueOrDefault();
            }
        }

        return (float)num;
    }

    /// <summary>Computes the sum of a sequence of <see cref="T:System.Double" /> values.</summary>
    /// <returns>The sum of the values in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Double" /> values to calculate the sum of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static double Sum(this IEnumerable<double> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0.0;
        foreach (var num2 in source)
        {
            num1 += num2;
        }

        return num1;
    }

    /// <summary>Computes the sum of a sequence of nullable <see cref="T:System.Double" /> values.</summary>
    /// <returns>The sum of the values in the sequence.</returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Double" /> values to calculate the sum of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static double? Sum(this IEnumerable<double?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num = 0.0;
        foreach (var nullable in source)
        {
            if (nullable.HasValue)
            {
                num += nullable.GetValueOrDefault();
            }
        }

        return num;
    }

    /// <summary>Computes the sum of a sequence of <see cref="T:System.Decimal" /> values.</summary>
    /// <returns>The sum of the values in the sequence.</returns>
    /// <param name="source">A sequence of <see cref="T:System.Decimal" /> values to calculate the sum of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Decimal.MaxValue" />.</exception>
    public static decimal Sum(this IEnumerable<decimal> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num1 = 0M;
        foreach (var num2 in source)
        {
            num1 += num2;
        }

        return num1;
    }

    /// <summary>Computes the sum of a sequence of nullable <see cref="T:System.Decimal" /> values.</summary>
    /// <returns>The sum of the values in the sequence.</returns>
    /// <param name="source">A sequence of nullable <see cref="T:System.Decimal" /> values to calculate the sum of.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Decimal.MaxValue" />.</exception>
    public static decimal? Sum(this IEnumerable<decimal?> source)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        var num = 0M;
        foreach (var nullable in source)
        {
            if (nullable.HasValue)
            {
                num += nullable.GetValueOrDefault();
            }
        }

        return num;
    }

    /// <summary>
    ///     Computes the sum of the sequence of <see cref="T:System.Int32" /> values that are obtained by invoking a
    ///     transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The sum of the projected values.</returns>
    /// <param name="source">A sequence of values that are used to calculate a sum.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
    public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
    {
        return source.Select(selector).Sum();
    }

    /// <summary>
    ///     Computes the sum of the sequence of nullable <see cref="T:System.Int32" /> values that are obtained by
    ///     invoking a transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The sum of the projected values.</returns>
    /// <param name="source">A sequence of values that are used to calculate a sum.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
    public static int? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
    {
        return source.Select(selector).Sum();
    }

    /// <summary>
    ///     Computes the sum of the sequence of <see cref="T:System.Int64" /> values that are obtained by invoking a
    ///     transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The sum of the projected values.</returns>
    /// <param name="source">A sequence of values that are used to calculate a sum.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Int64.MaxValue" />.</exception>
    public static long Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
    {
        return source.Select(selector).Sum();
    }

    /// <summary>
    ///     Computes the sum of the sequence of nullable <see cref="T:System.Int64" /> values that are obtained by
    ///     invoking a transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The sum of the projected values.</returns>
    /// <param name="source">A sequence of values that are used to calculate a sum.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Int64.MaxValue" />.</exception>
    public static long? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
    {
        return source.Select(selector).Sum();
    }

    /// <summary>
    ///     Computes the sum of the sequence of <see cref="T:System.Single" /> values that are obtained by invoking a
    ///     transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The sum of the projected values.</returns>
    /// <param name="source">A sequence of values that are used to calculate a sum.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static float Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
    {
        return source.Select(selector).Sum();
    }

    /// <summary>
    ///     Computes the sum of the sequence of nullable <see cref="T:System.Single" /> values that are obtained by
    ///     invoking a transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The sum of the projected values.</returns>
    /// <param name="source">A sequence of values that are used to calculate a sum.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static float? Sum<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, float?> selector)
    {
        return source.Select(selector).Sum();
    }

    /// <summary>
    ///     Computes the sum of the sequence of <see cref="T:System.Double" /> values that are obtained by invoking a
    ///     transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The sum of the projected values.</returns>
    /// <param name="source">A sequence of values that are used to calculate a sum.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static double Sum<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, double> selector)
    {
        return source.Select(selector).Sum();
    }

    /// <summary>
    ///     Computes the sum of the sequence of nullable <see cref="T:System.Double" /> values that are obtained by
    ///     invoking a transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The sum of the projected values.</returns>
    /// <param name="source">A sequence of values that are used to calculate a sum.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    public static double? Sum<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, double?> selector)
    {
        return source.Select(selector).Sum();
    }

    /// <summary>
    ///     Computes the sum of the sequence of <see cref="T:System.Decimal" /> values that are obtained by invoking a
    ///     transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The sum of the projected values.</returns>
    /// <param name="source">A sequence of values that are used to calculate a sum.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Decimal.MaxValue" />.</exception>
    public static decimal Sum<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, decimal> selector)
    {
        return source.Select(selector).Sum();
    }

    /// <summary>
    ///     Computes the sum of the sequence of nullable <see cref="T:System.Decimal" /> values that are obtained by
    ///     invoking a transform function on each element of the input sequence.
    /// </summary>
    /// <returns>The sum of the projected values.</returns>
    /// <param name="source">A sequence of values that are used to calculate a sum.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="selector" /> is null.
    /// </exception>
    /// <exception cref="T:System.OverflowException">The sum is larger than <see cref="F:System.Decimal.MaxValue" />.</exception>
    public static decimal? Sum<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, decimal?> selector)
    {
        return source.Select(selector).Sum();
    }

    /// <summary>Returns a specified number of contiguous elements from the start of a sequence.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the specified number of elements
    ///     from the start of the input sequence.
    /// </returns>
    /// <param name="source">The sequence to return elements from.</param>
    /// <param name="count">The number of elements to return.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, int count)
    {
        return source != null ? TakeIterator(source, count) : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>Returns elements from a sequence as long as a specified condition is true.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the elements from the input
    ///     sequence that occur before the element at which the test no longer passes.
    /// </returns>
    /// <param name="source">A sequence to return elements from.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    public static IEnumerable<TSource> TakeWhile<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return predicate != null ? TakeWhileIterator(source, predicate) : throw Error.ArgumentNull(nameof(predicate));
    }

    /// <summary>
    ///     Returns elements from a sequence as long as a specified condition is true. The element's index is used in the
    ///     logic of the predicate function.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains elements from the input sequence
    ///     that occur before the element at which the test no longer passes.
    /// </returns>
    /// <param name="source">The sequence to return elements from.</param>
    /// <param name="predicate">
    ///     A function to test each source element for a condition; the second parameter of the function
    ///     represents the index of the source element.
    /// </param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    public static IEnumerable<TSource> TakeWhile<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, int, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return predicate != null ? TakeWhileIterator(source, predicate) : throw Error.ArgumentNull(nameof(predicate));
    }

    /// <summary>Performs a subsequent ordering of the elements in a sequence in ascending order according to a key.</summary>
    /// <returns>An <see cref="T:System.Linq.IOrderedEnumerable`1" /> whose elements are sorted according to a key.</returns>
    /// <param name="source">An <see cref="T:System.Linq.IOrderedEnumerable`1" /> that contains elements to sort.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(
        this IOrderedEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return source != null
            ? source.CreateOrderedEnumerable(keySelector, null, false)
            : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>Performs a subsequent ordering of the elements in a sequence in ascending order by using a specified comparer.</summary>
    /// <returns>An <see cref="T:System.Linq.IOrderedEnumerable`1" /> whose elements are sorted according to a key.</returns>
    /// <param name="source">An <see cref="T:System.Linq.IOrderedEnumerable`1" /> that contains elements to sort.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IComparer`1" /> to compare keys.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(
        this IOrderedEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey> comparer)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return source.CreateOrderedEnumerable(keySelector, comparer, false);
    }

    /// <summary>Performs a subsequent ordering of the elements in a sequence in descending order, according to a key.</summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.IOrderedEnumerable`1" /> whose elements are sorted in descending order according
    ///     to a key.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Linq.IOrderedEnumerable`1" /> that contains elements to sort.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(
        this IOrderedEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return source != null
            ? source.CreateOrderedEnumerable(keySelector, null, true)
            : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>
    ///     Performs a subsequent ordering of the elements in a sequence in descending order by using a specified
    ///     comparer.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.IOrderedEnumerable`1" /> whose elements are sorted in descending order according
    ///     to a key.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Linq.IOrderedEnumerable`1" /> that contains elements to sort.</param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IComparer`1" /> to compare keys.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(
        this IOrderedEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IComparer<TKey> comparer)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return source.CreateOrderedEnumerable(keySelector, comparer, true);
    }

    /// <summary>Creates an array from a <see cref="T:System.Collections.Generic.IEnumerable`1" />.</summary>
    /// <returns>An array that contains the elements from the input sequence.</returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to create an array from.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static TSource[] ToArray<TSource>(this IEnumerable<TSource> source)
    {
        return source != null ? new Buffer<TSource>(source).ToArray() : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Collections.Generic.Dictionary`2" /> from an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> according to a specified key selector function.
    /// </summary>
    /// <returns>A <see cref="T:System.Collections.Generic.Dictionary`2" /> that contains keys and values.</returns>
    /// <param name="source">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to create a
    ///     <see cref="T:System.Collections.Generic.Dictionary`2" /> from.
    /// </param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.-or-<paramref name="keySelector" /> produces
    ///     a key that is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="keySelector" /> produces duplicate keys for two elements.
    /// </exception>
    public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return source.ToDictionary(keySelector, IdentityFunction<TSource>.Instance, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Collections.Generic.Dictionary`2" /> from an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> according to a specified key selector function and key
    ///     comparer.
    /// </summary>
    /// <returns>A <see cref="T:System.Collections.Generic.Dictionary`2" /> that contains keys and values.</returns>
    /// <param name="source">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to create a
    ///     <see cref="T:System.Collections.Generic.Dictionary`2" /> from.
    /// </param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare keys.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the keys returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.-or-<paramref name="keySelector" /> produces
    ///     a key that is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="keySelector" /> produces duplicate keys for two elements.
    /// </exception>
    public static Dictionary<TKey, TSource> ToDictionary<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey> comparer)
    {
        return source.ToDictionary(keySelector, IdentityFunction<TSource>.Instance, comparer);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Collections.Generic.Dictionary`2" /> from an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> according to specified key selector and element selector
    ///     functions.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.Generic.Dictionary`2" /> that contains values of type
    ///     <paramref name="TElement" /> selected from the input sequence.
    /// </returns>
    /// <param name="source">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to create a
    ///     <see cref="T:System.Collections.Generic.Dictionary`2" /> from.
    /// </param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is null.-or-
    ///     <paramref name="keySelector" /> produces a key that is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="keySelector" /> produces duplicate keys for two elements.
    /// </exception>
    public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector)
    {
        return source.ToDictionary(keySelector, elementSelector, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Collections.Generic.Dictionary`2" /> from an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> according to a specified key selector function, a
    ///     comparer, and an element selector function.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.Generic.Dictionary`2" /> that contains values of type
    ///     <paramref name="TElement" /> selected from the input sequence.
    /// </returns>
    /// <param name="source">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to create a
    ///     <see cref="T:System.Collections.Generic.Dictionary`2" /> from.
    /// </param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare keys.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is null.-or-
    ///     <paramref name="keySelector" /> produces a key that is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="keySelector" /> produces duplicate keys for two elements.
    /// </exception>
    public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        IEqualityComparer<TKey> comparer)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (keySelector == null)
        {
            throw Error.ArgumentNull(nameof(keySelector));
        }

        if (elementSelector == null)
        {
            throw Error.ArgumentNull(nameof(elementSelector));
        }

        var dictionary = new Dictionary<TKey, TElement>(comparer);
        foreach (var source1 in source)
        {
            dictionary.Add(keySelector(source1), elementSelector(source1));
        }

        return dictionary;
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Collections.Generic.List`1" /> from an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" />.
    /// </summary>
    /// <returns>A <see cref="T:System.Collections.Generic.List`1" /> that contains elements from the input sequence.</returns>
    /// <param name="source">
    ///     The <see cref="T:System.Collections.Generic.IEnumerable`1" /> to create a
    ///     <see cref="T:System.Collections.Generic.List`1" /> from.
    /// </param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> is null.
    /// </exception>
    public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
    {
        return source != null ? new List<TSource>(source) : throw Error.ArgumentNull(nameof(source));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Lookup`2" /> from an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> according to a specified key selector function.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Lookup`2" /> that contains keys and values.</returns>
    /// <param name="source">
    ///     The <see cref="T:System.Collections.Generic.IEnumerable`1" /> to create a
    ///     <see cref="T:System.Linq.Lookup`2" /> from.
    /// </param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector)
    {
        return Lookup<TKey, TSource>.Create(source, keySelector, IdentityFunction<TSource>.Instance, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Lookup`2" /> from an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> according to a specified key selector function and key
    ///     comparer.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Lookup`2" /> that contains keys and values.</returns>
    /// <param name="source">
    ///     The <see cref="T:System.Collections.Generic.IEnumerable`1" /> to create a
    ///     <see cref="T:System.Linq.Lookup`2" /> from.
    /// </param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare keys.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> is null.
    /// </exception>
    public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey> comparer)
    {
        return Lookup<TKey, TSource>.Create(source, keySelector, IdentityFunction<TSource>.Instance, comparer);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Lookup`2" /> from an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> according to specified key selector and element selector
    ///     functions.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Lookup`2" /> that contains values of type <paramref name="TElement" /> selected
    ///     from the input sequence.
    /// </returns>
    /// <param name="source">
    ///     The <see cref="T:System.Collections.Generic.IEnumerable`1" /> to create a
    ///     <see cref="T:System.Linq.Lookup`2" /> from.
    /// </param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is null.
    /// </exception>
    public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector)
    {
        return Lookup<TKey, TElement>.Create(source, keySelector, elementSelector, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Lookup`2" /> from an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> according to a specified key selector function, a
    ///     comparer and an element selector function.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Lookup`2" /> that contains values of type <paramref name="TElement" /> selected
    ///     from the input sequence.
    /// </returns>
    /// <param name="source">
    ///     The <see cref="T:System.Collections.Generic.IEnumerable`1" /> to create a
    ///     <see cref="T:System.Linq.Lookup`2" /> from.
    /// </param>
    /// <param name="keySelector">A function to extract a key from each element.</param>
    /// <param name="elementSelector">A transform function to produce a result element value from each element.</param>
    /// <param name="comparer">An <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare keys.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <typeparam name="TKey">The type of the key returned by <paramref name="keySelector" />.</typeparam>
    /// <typeparam name="TElement">The type of the value returned by <paramref name="elementSelector" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="keySelector" /> or <paramref name="elementSelector" /> is null.
    /// </exception>
    public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector,
        IEqualityComparer<TKey> comparer)
    {
        return Lookup<TKey, TElement>.Create(source, keySelector, elementSelector, comparer);
    }

    /// <summary>Produces the set union of two sequences by using the default equality comparer.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the elements from both input
    ///     sequences, excluding duplicates.
    /// </returns>
    /// <param name="first">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose distinct elements form the first
    ///     set for the union.
    /// </param>
    /// <param name="second">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose distinct elements form the
    ///     second set for the union.
    /// </param>
    /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="first" /> or <paramref name="second" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Union<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second)
    {
        if (first == null)
        {
            throw Error.ArgumentNull(nameof(first));
        }

        return second != null ? UnionIterator(first, second, null) : throw Error.ArgumentNull(nameof(second));
    }

    /// <summary>
    ///     Produces the set union of two sequences by using a specified
    ///     <see cref="T:System.Collections.Generic.IEqualityComparer`1" />.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains the elements from both input
    ///     sequences, excluding duplicates.
    /// </returns>
    /// <param name="first">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose distinct elements form the first
    ///     set for the union.
    /// </param>
    /// <param name="second">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> whose distinct elements form the
    ///     second set for the union.
    /// </param>
    /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> to compare values.</param>
    /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="first" /> or <paramref name="second" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Union<TSource>(
        this IEnumerable<TSource> first,
        IEnumerable<TSource> second,
        IEqualityComparer<TSource> comparer)
    {
        if (first == null)
        {
            throw Error.ArgumentNull(nameof(first));
        }

        if (second == null)
        {
            throw Error.ArgumentNull(nameof(second));
        }

        return UnionIterator(first, second, comparer);
    }

    /// <summary>Filters a sequence of values based on a predicate.</summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains elements from the input sequence
    ///     that satisfy the condition.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to filter.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Where<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        if (predicate == null)
        {
            throw Error.ArgumentNull(nameof(predicate));
        }

        switch (source)
        {
            case Iterator<TSource> _:
                return ((Iterator<TSource>)source).Where(predicate);
            case TSource[] _:
                return new WhereArrayIterator<TSource>((TSource[])source, predicate);
            case List<TSource> _:
                return new WhereListIterator<TSource>((List<TSource>)source, predicate);
            default:
                return new WhereEnumerableIterator<TSource>(source, predicate);
        }
    }

    /// <summary>
    ///     Filters a sequence of values based on a predicate. Each element's index is used in the logic of the predicate
    ///     function.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains elements from the input sequence
    ///     that satisfy the condition.
    /// </returns>
    /// <param name="source">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> to filter.</param>
    /// <param name="predicate">
    ///     A function to test each source element for a condition; the second parameter of the function
    ///     represents the index of the source element.
    /// </param>
    /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="predicate" /> is null.
    /// </exception>
    public static IEnumerable<TSource> Where<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, int, bool> predicate)
    {
        if (source == null)
        {
            throw Error.ArgumentNull(nameof(source));
        }

        return predicate != null ? WhereIterator(source, predicate) : throw Error.ArgumentNull(nameof(predicate));
    }

    /// <summary>
    ///     Applies a specified function to the corresponding elements of two sequences, producing a sequence of the
    ///     results.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains elements of the two input
    ///     sequences, combined by <paramref name="resultSelector" />.
    /// </returns>
    /// <param name="first">The first input sequence.</param>
    /// <param name="second">The second input sequence.</param>
    /// <param name="resultSelector">A function that specifies how to combine the corresponding elements of the two sequences.</param>
    /// <typeparam name="TFirst">The type of the elements of the first input sequence.</typeparam>
    /// <typeparam name="TSecond">The type of the elements of the second input sequence.</typeparam>
    /// <typeparam name="TResult">The type of the elements of the result sequence.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="first" /> or <paramref name="second" /> is null.
    /// </exception>
    public static IEnumerable<TResult> Zip<TFirst, TSecond, TResult>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second,
        Func<TFirst, TSecond, TResult> resultSelector)
    {
        if (first == null)
        {
            throw Error.ArgumentNull(nameof(first));
        }

        if (second == null)
        {
            throw Error.ArgumentNull(nameof(second));
        }

        if (resultSelector == null)
        {
            throw Error.ArgumentNull(nameof(resultSelector));
        }

        return ZipIterator(first, second, resultSelector);
    }

    private static IEnumerable<TResult> CastIterator<TResult>(IEnumerable source)
    {
        foreach (TResult result in source)
        {
            yield return result;
        }
    }

    private static Func<TSource, bool> CombinePredicates<TSource>(
        Func<TSource, bool> predicate1,
        Func<TSource, bool> predicate2)
    {
        return x => predicate1(x) && predicate2(x);
    }

    private static Func<TSource, TResult> CombineSelectors<TSource, TMiddle, TResult>(
        Func<TSource, TMiddle> selector1,
        Func<TMiddle, TResult> selector2)
    {
        return x => selector2(selector1(x));
    }

    private static IEnumerable<TSource> ConcatIterator<TSource>(
        IEnumerable<TSource> first,
        IEnumerable<TSource> second)
    {
        foreach (var source in first)
        {
            yield return source;
        }

        foreach (var source in second)
        {
            yield return source;
        }
    }

    private static IEnumerable<TSource> DefaultIfEmptyIterator<TSource>(
        IEnumerable<TSource> source,
        TSource defaultValue)
    {
        using (var e = source.GetEnumerator())
        {
            if (e.MoveNext())
            {
                do
                {
                    yield return e.Current;
                } while (e.MoveNext());
            }
            else
            {
                yield return defaultValue;
            }
        }
    }

    private static IEnumerable<TSource> DistinctIterator<TSource>(
        IEnumerable<TSource> source,
        IEqualityComparer<TSource> comparer)
    {
        var set = new Set<TSource>(comparer);
        foreach (var source1 in source)
        {
            if (set.Add(source1))
            {
                yield return source1;
            }
        }
    }

    private static IEnumerable<TSource> ExceptIterator<TSource>(
        IEnumerable<TSource> first,
        IEnumerable<TSource> second,
        IEqualityComparer<TSource> comparer)
    {
        var set = new Set<TSource>(comparer);
        foreach (var source in second)
        {
            set.Add(source);
        }

        foreach (var source in first)
        {
            if (set.Add(source))
            {
                yield return source;
            }
        }
    }

    private static IEnumerable<TResult> GroupJoinIterator<TOuter, TInner, TKey, TResult>(
        IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        Func<TOuter, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        Func<TOuter, IEnumerable<TInner>, TResult> resultSelector,
        IEqualityComparer<TKey> comparer)
    {
        var lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
        foreach (var outer1 in outer)
        {
            yield return resultSelector(outer1, lookup[outerKeySelector(outer1)]);
        }
    }

    private static IEnumerable<TSource> IntersectIterator<TSource>(
        IEnumerable<TSource> first,
        IEnumerable<TSource> second,
        IEqualityComparer<TSource> comparer)
    {
        var set = new Set<TSource>(comparer);
        foreach (var source in second)
        {
            set.Add(source);
        }

        foreach (var source in first)
        {
            if (set.Remove(source))
            {
                yield return source;
            }
        }
    }

    private static IEnumerable<TResult> JoinIterator<TOuter, TInner, TKey, TResult>(
        IEnumerable<TOuter> outer,
        IEnumerable<TInner> inner,
        Func<TOuter, TKey> outerKeySelector,
        Func<TInner, TKey> innerKeySelector,
        Func<TOuter, TInner, TResult> resultSelector,
        IEqualityComparer<TKey> comparer)
    {
        var lookup = Lookup<TKey, TInner>.CreateForJoin(inner, innerKeySelector, comparer);
        foreach (var outer1 in outer)
        {
            var item = outer1;
            var g = lookup.GetGrouping(outerKeySelector(item), false);
            if (g != null)
            {
                for (var i = 0; i < g.count; ++i)
                {
                    yield return resultSelector(item, g.elements[i]);
                }
            }

            g = null;
            item = default;
        }
    }

    private static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable source)
    {
        foreach (var obj in source)
        {
            if (obj is TResult result)
            {
                yield return result;
            }
        }
    }

    private static IEnumerable<int> RangeIterator(int start, int count)
    {
        for (var i = 0; i < count; ++i)
        {
            yield return start + i;
        }
    }

    private static IEnumerable<TResult> RepeatIterator<TResult>(TResult element, int count)
    {
        for (var i = 0; i < count; ++i)
        {
            yield return element;
        }
    }

    private static IEnumerable<TSource> ReverseIterator<TSource>(IEnumerable<TSource> source)
    {
        var buffer = new Buffer<TSource>(source);
        for (var i = buffer.count - 1; i >= 0; --i)
        {
            yield return buffer.items[i];
        }
    }

    private static IEnumerable<TResult> SelectIterator<TSource, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, int, TResult> selector)
    {
        var index = -1;
        foreach (var source1 in source)
        {
            checked
            {
                ++index;
            }

            yield return selector(source1, index);
        }
    }

    private static IEnumerable<TResult> SelectManyIterator<TSource, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, IEnumerable<TResult>> selector)
    {
        foreach (var source1 in source)
        {
            foreach (var result in selector(source1))
            {
                yield return result;
            }
        }
    }

    private static IEnumerable<TResult> SelectManyIterator<TSource, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, int, IEnumerable<TResult>> selector)
    {
        var index = -1;
        foreach (var source1 in source)
        {
            checked
            {
                ++index;
            }

            foreach (var result in selector(source1, index))
            {
                yield return result;
            }
        }
    }

    private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, int, IEnumerable<TCollection>> collectionSelector,
        Func<TSource, TCollection, TResult> resultSelector)
    {
        var index = -1;
        foreach (var source1 in source)
        {
            var element = source1;
            checked
            {
                ++index;
            }

            foreach (var collection in collectionSelector(element, index))
            {
                yield return resultSelector(element, collection);
            }

            element = default;
        }
    }

    private static IEnumerable<TResult> SelectManyIterator<TSource, TCollection, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, IEnumerable<TCollection>> collectionSelector,
        Func<TSource, TCollection, TResult> resultSelector)
    {
        foreach (var source1 in source)
        {
            var element = source1;
            foreach (var collection in collectionSelector(element))
            {
                yield return resultSelector(element, collection);
            }

            element = default;
        }
    }

    private static IEnumerable<TSource> SkipIterator<TSource>(IEnumerable<TSource> source, int count)
    {
        using (var e = source.GetEnumerator())
        {
            while (count > 0 && e.MoveNext())
            {
                --count;
            }

            if (count <= 0)
            {
                while (e.MoveNext())
                {
                    yield return e.Current;
                }
            }
        }
    }

    private static IEnumerable<TSource> SkipWhileIterator<TSource>(
        IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        var yielding = false;
        foreach (var source1 in source)
        {
            if (!yielding && !predicate(source1))
            {
                yielding = true;
            }

            if (yielding)
            {
                yield return source1;
            }
        }
    }

    private static IEnumerable<TSource> SkipWhileIterator<TSource>(
        IEnumerable<TSource> source,
        Func<TSource, int, bool> predicate)
    {
        var index = -1;
        var yielding = false;
        foreach (var source1 in source)
        {
            checked
            {
                ++index;
            }

            if (!yielding && !predicate(source1, index))
            {
                yielding = true;
            }

            if (yielding)
            {
                yield return source1;
            }
        }
    }

    private static IEnumerable<TSource> TakeIterator<TSource>(IEnumerable<TSource> source, int count)
    {
        if (count > 0)
        {
            foreach (var source1 in source)
            {
                yield return source1;
                if (--count == 0)
                {
                    break;
                }
            }
        }
    }

    private static IEnumerable<TSource> TakeWhileIterator<TSource>(
        IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        foreach (var source1 in source)
        {
            if (predicate(source1))
            {
                yield return source1;
            }
            else
            {
                break;
            }
        }
    }

    private static IEnumerable<TSource> TakeWhileIterator<TSource>(
        IEnumerable<TSource> source,
        Func<TSource, int, bool> predicate)
    {
        var index = -1;
        foreach (var source1 in source)
        {
            checked
            {
                ++index;
            }

            if (predicate(source1, index))
            {
                yield return source1;
            }
            else
            {
                break;
            }
        }
    }

    private static IEnumerable<TSource> UnionIterator<TSource>(
        IEnumerable<TSource> first,
        IEnumerable<TSource> second,
        IEqualityComparer<TSource> comparer)
    {
        var set = new Set<TSource>(comparer);
        foreach (var source in first)
        {
            if (set.Add(source))
            {
                yield return source;
            }
        }

        foreach (var source in second)
        {
            if (set.Add(source))
            {
                yield return source;
            }
        }
    }

    private static IEnumerable<TSource> WhereIterator<TSource>(
        IEnumerable<TSource> source,
        Func<TSource, int, bool> predicate)
    {
        var index = -1;
        foreach (var source1 in source)
        {
            checked
            {
                ++index;
            }

            if (predicate(source1, index))
            {
                yield return source1;
            }
        }
    }

    private static IEnumerable<TResult> ZipIterator<TFirst, TSecond, TResult>(
        IEnumerable<TFirst> first,
        IEnumerable<TSecond> second,
        Func<TFirst, TSecond, TResult> resultSelector)
    {
        using (var e1 = first.GetEnumerator())
        {
            using (var e2 = second.GetEnumerator())
            {
                while (e1.MoveNext() && e2.MoveNext())
                {
                    yield return resultSelector(e1.Current, e2.Current);
                }
            }
        }
    }

    private abstract class Iterator<TSource> :
        IEnumerable<TSource>,
        IEnumerable,
        IEnumerator<TSource>,
        IDisposable,
        IEnumerator
    {
        private readonly int threadId;
        internal TSource current;
        internal int state;

        public Iterator()
        {
            threadId = Thread.CurrentThread.ManagedThreadId;
        }

        public IEnumerator<TSource> GetEnumerator()
        {
            if (threadId == Thread.CurrentThread.ManagedThreadId && state == 0)
            {
                state = 1;
                return this;
            }

            var enumerator = Clone();
            enumerator.state = 1;
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TSource Current => current;

        public virtual void Dispose()
        {
            current = default;
            state = -1;
        }

        public abstract bool MoveNext();

        object IEnumerator.Current => Current;

        void IEnumerator.Reset()
        {
            throw new NotImplementedException();
        }

        public abstract Iterator<TSource> Clone();

        public abstract IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector);

        public abstract IEnumerable<TSource> Where(Func<TSource, bool> predicate);
    }

    private class WhereEnumerableIterator<TSource> : Iterator<TSource>
    {
        private readonly Func<TSource, bool> predicate;
        private readonly IEnumerable<TSource> source;
        private IEnumerator<TSource> enumerator;

        public WhereEnumerableIterator(IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public override Iterator<TSource> Clone()
        {
            return new WhereEnumerableIterator<TSource>(source, predicate);
        }

        public override void Dispose()
        {
            if (enumerator != null)
            {
                enumerator.Dispose();
            }

            enumerator = null;
            base.Dispose();
        }

        public override bool MoveNext()
        {
            switch (state)
            {
                case 1:
                    enumerator = source.GetEnumerator();
                    state = 2;
                    goto case 2;
                case 2:
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        if (predicate(current))
                        {
                            this.current = current;
                            return true;
                        }
                    }

                    Dispose();
                    break;
            }

            return false;
        }

        public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
        {
            return new WhereSelectEnumerableIterator<TSource, TResult>(source, predicate, selector);
        }

        public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
        {
            return new WhereEnumerableIterator<TSource>(source, CombinePredicates(this.predicate, predicate));
        }
    }

    private class WhereArrayIterator<TSource> : Iterator<TSource>
    {
        private readonly Func<TSource, bool> predicate;
        private readonly TSource[] source;
        private int index;

        public WhereArrayIterator(TSource[] source, Func<TSource, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public override Iterator<TSource> Clone()
        {
            return new WhereArrayIterator<TSource>(source, predicate);
        }

        public override bool MoveNext()
        {
            if (state == 1)
            {
                while (index < this.source.Length)
                {
                    var source = this.source[index];
                    ++index;
                    if (predicate(source))
                    {
                        current = source;
                        return true;
                    }
                }

                Dispose();
            }

            return false;
        }

        public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
        {
            return new WhereSelectArrayIterator<TSource, TResult>(source, predicate, selector);
        }

        public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
        {
            return new WhereArrayIterator<TSource>(source, CombinePredicates(this.predicate, predicate));
        }
    }

    private class WhereListIterator<TSource> : Iterator<TSource>
    {
        private readonly Func<TSource, bool> predicate;
        private readonly List<TSource> source;
        private List<TSource>.Enumerator enumerator;

        public WhereListIterator(List<TSource> source, Func<TSource, bool> predicate)
        {
            this.source = source;
            this.predicate = predicate;
        }

        public override Iterator<TSource> Clone()
        {
            return new WhereListIterator<TSource>(source, predicate);
        }

        public override bool MoveNext()
        {
            switch (state)
            {
                case 1:
                    enumerator = source.GetEnumerator();
                    state = 2;
                    goto case 2;
                case 2:
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        if (predicate(current))
                        {
                            this.current = current;
                            return true;
                        }
                    }

                    Dispose();
                    break;
            }

            return false;
        }

        public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
        {
            return new WhereSelectListIterator<TSource, TResult>(source, predicate, selector);
        }

        public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
        {
            return new WhereListIterator<TSource>(source, CombinePredicates(this.predicate, predicate));
        }
    }

    private class SelectEnumerableIterator<TSource, TResult> :
        Iterator<TResult>,
        IIListProvider<TResult>,
        IEnumerable<TResult>,
        IEnumerable
    {
        private readonly Func<TSource, TResult> _selector;
        private readonly IEnumerable<TSource> _source;
        private IEnumerator<TSource> _enumerator;

        public SelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            _source = source;
            _selector = selector;
        }

        public TResult[] ToArray()
        {
            var largeArrayBuilder = new LargeArrayBuilder<TResult>(true);
            foreach (var source in _source)
            {
                largeArrayBuilder.Add(_selector(source));
            }

            return largeArrayBuilder.ToArray();
        }

        public List<TResult> ToList()
        {
            var list = new List<TResult>();
            foreach (var source in _source)
            {
                list.Add(_selector(source));
            }

            return list;
        }

        public int GetCount(bool onlyIfCheap)
        {
            if (onlyIfCheap)
            {
                return -1;
            }

            var count = 0;
            foreach (var source in _source)
            {
                var result = _selector(source);
                checked
                {
                    ++count;
                }
            }

            return count;
        }

        public override Iterator<TResult> Clone()
        {
            return new SelectEnumerableIterator<TSource, TResult>(_source, _selector);
        }

        public override void Dispose()
        {
            if (_enumerator != null)
            {
                _enumerator.Dispose();
                _enumerator = null;
            }

            base.Dispose();
        }

        public override bool MoveNext()
        {
            switch (state)
            {
                case 1:
                    _enumerator = _source.GetEnumerator();
                    state = 2;
                    goto case 2;
                case 2:
                    if (_enumerator.MoveNext())
                    {
                        current = _selector(_enumerator.Current);
                        return true;
                    }

                    Dispose();
                    break;
            }

            return false;
        }

        public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
        {
            return new SelectEnumerableIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
        }

        public override IEnumerable<TResult> Where(Func<TResult, bool> predicate)
        {
            return new WhereEnumerableIterator<TResult>(this, predicate);
        }
    }

    private class WhereSelectEnumerableIterator<TSource, TResult> : Iterator<TResult>
    {
        private readonly Func<TSource, bool> predicate;
        private readonly Func<TSource, TResult> selector;
        private readonly IEnumerable<TSource> source;
        private IEnumerator<TSource> enumerator;

        public WhereSelectEnumerableIterator(
            IEnumerable<TSource> source,
            Func<TSource, bool> predicate,
            Func<TSource, TResult> selector)
        {
            this.source = source;
            this.predicate = predicate;
            this.selector = selector;
        }

        public override Iterator<TResult> Clone()
        {
            return new WhereSelectEnumerableIterator<TSource, TResult>(source, predicate, selector);
        }

        public override void Dispose()
        {
            if (enumerator != null)
            {
                enumerator.Dispose();
            }

            enumerator = null;
            base.Dispose();
        }

        public override bool MoveNext()
        {
            switch (state)
            {
                case 1:
                    enumerator = source.GetEnumerator();
                    state = 2;
                    goto case 2;
                case 2:
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        if (predicate == null || predicate(current))
                        {
                            this.current = selector(current);
                            return true;
                        }
                    }

                    Dispose();
                    break;
            }

            return false;
        }

        public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
        {
            return new WhereSelectEnumerableIterator<TSource, TResult2>(source, predicate,
                CombineSelectors(this.selector, selector));
        }

        public override IEnumerable<TResult> Where(Func<TResult, bool> predicate)
        {
            return new WhereEnumerableIterator<TResult>(this, predicate);
        }
    }

    private class WhereSelectArrayIterator<TSource, TResult> : Iterator<TResult>
    {
        private readonly Func<TSource, bool> predicate;
        private readonly Func<TSource, TResult> selector;
        private readonly TSource[] source;
        private int index;

        public WhereSelectArrayIterator(
            TSource[] source,
            Func<TSource, bool> predicate,
            Func<TSource, TResult> selector)
        {
            this.source = source;
            this.predicate = predicate;
            this.selector = selector;
        }

        public override Iterator<TResult> Clone()
        {
            return new WhereSelectArrayIterator<TSource, TResult>(source, predicate, selector);
        }

        public override bool MoveNext()
        {
            if (state == 1)
            {
                while (index < this.source.Length)
                {
                    var source = this.source[index];
                    ++index;
                    if (predicate == null || predicate(source))
                    {
                        current = selector(source);
                        return true;
                    }
                }

                Dispose();
            }

            return false;
        }

        public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
        {
            return new WhereSelectArrayIterator<TSource, TResult2>(source, predicate,
                CombineSelectors(this.selector, selector));
        }

        public override IEnumerable<TResult> Where(Func<TResult, bool> predicate)
        {
            return new WhereEnumerableIterator<TResult>(this, predicate);
        }
    }

    private class WhereSelectListIterator<TSource, TResult> : Iterator<TResult>
    {
        private readonly Func<TSource, bool> predicate;
        private readonly Func<TSource, TResult> selector;
        private readonly List<TSource> source;
        private List<TSource>.Enumerator enumerator;

        public WhereSelectListIterator(
            List<TSource> source,
            Func<TSource, bool> predicate,
            Func<TSource, TResult> selector)
        {
            this.source = source;
            this.predicate = predicate;
            this.selector = selector;
        }

        public override Iterator<TResult> Clone()
        {
            return new WhereSelectListIterator<TSource, TResult>(source, predicate, selector);
        }

        public override bool MoveNext()
        {
            switch (state)
            {
                case 1:
                    enumerator = source.GetEnumerator();
                    state = 2;
                    goto case 2;
                case 2:
                    while (enumerator.MoveNext())
                    {
                        var current = enumerator.Current;
                        if (predicate == null || predicate(current))
                        {
                            this.current = selector(current);
                            return true;
                        }
                    }

                    Dispose();
                    break;
            }

            return false;
        }

        public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
        {
            return new WhereSelectListIterator<TSource, TResult2>(source, predicate,
                CombineSelectors(this.selector, selector));
        }

        public override IEnumerable<TResult> Where(Func<TResult, bool> predicate)
        {
            return new WhereEnumerableIterator<TResult>(this, predicate);
        }
    }

    private abstract class AppendPrependIterator<TSource> :
        Iterator<TSource>,
        IIListProvider<TSource>,
        IEnumerable<TSource>,
        IEnumerable
    {
        protected readonly IEnumerable<TSource> _source;
        protected IEnumerator<TSource> enumerator;

        protected AppendPrependIterator(IEnumerable<TSource> source)
        {
            _source = source;
        }

        public abstract TSource[] ToArray();

        public abstract List<TSource> ToList();

        public abstract int GetCount(bool onlyIfCheap);

        public abstract AppendPrependIterator<TSource> Append(TSource item);

        public override void Dispose()
        {
            if (enumerator != null)
            {
                enumerator.Dispose();
                enumerator = null;
            }

            base.Dispose();
        }

        public abstract AppendPrependIterator<TSource> Prepend(TSource item);

        public override IEnumerable<TResult> Select<TResult>(Func<TSource, TResult> selector)
        {
            return new SelectEnumerableIterator<TSource, TResult>(this, selector);
        }

        public override IEnumerable<TSource> Where(Func<TSource, bool> predicate)
        {
            return new WhereEnumerableIterator<TSource>(this, predicate);
        }

        protected void GetSourceEnumerator()
        {
            enumerator = _source.GetEnumerator();
        }

        protected bool LoadFromEnumerator()
        {
            if (enumerator.MoveNext())
            {
                current = enumerator.Current;
                return true;
            }

            Dispose();
            return false;
        }
    }

    private class AppendPrepend1Iterator<TSource> : AppendPrependIterator<TSource>
    {
        private readonly bool _appending;
        private readonly TSource _item;

        public AppendPrepend1Iterator(IEnumerable<TSource> source, TSource item, bool appending)
            : base(source)
        {
            _item = item;
            _appending = appending;
        }

        public override AppendPrependIterator<TSource> Append(TSource item)
        {
            return _appending
                ? new AppendPrependN<TSource>(_source, null, new SingleLinkedNode<TSource>(_item).Add(item), 0, 2)
                : (AppendPrependIterator<TSource>)new AppendPrependN<TSource>(_source,
                    new SingleLinkedNode<TSource>(_item), new SingleLinkedNode<TSource>(item), 1, 1);
        }

        public override Iterator<TSource> Clone()
        {
            return new AppendPrepend1Iterator<TSource>(_source, _item, _appending);
        }

        public override int GetCount(bool onlyIfCheap)
        {
            if (_source is IIListProvider<TSource> source)
            {
                var count = source.GetCount(onlyIfCheap);
                return count != -1 ? count + 1 : -1;
            }

            return onlyIfCheap && !(_source is ICollection<TSource>) ? -1 : _source.Count() + 1;
        }

        public override bool MoveNext()
        {
            switch (state)
            {
                case 1:
                    state = 2;
                    if (!_appending)
                    {
                        current = _item;
                        return true;
                    }

                    goto case 2;
                case 2:
                    GetSourceEnumerator();
                    state = 3;
                    goto case 3;
                case 3:
                    if (LoadFromEnumerator())
                    {
                        return true;
                    }

                    if (_appending)
                    {
                        current = _item;
                        return true;
                    }

                    break;
            }

            Dispose();
            return false;
        }

        public override AppendPrependIterator<TSource> Prepend(TSource item)
        {
            return _appending
                ? new AppendPrependN<TSource>(_source, new SingleLinkedNode<TSource>(item),
                    new SingleLinkedNode<TSource>(_item), 1, 1)
                : (AppendPrependIterator<TSource>)new AppendPrependN<TSource>(_source,
                    new SingleLinkedNode<TSource>(_item).Add(item), null, 2, 0);
        }

        public override TSource[] ToArray()
        {
            var count = GetCount(true);
            if (count == -1)
            {
                return LazyToArray();
            }

            var array = new TSource[count];
            int arrayIndex;
            if (_appending)
            {
                arrayIndex = 0;
            }
            else
            {
                array[0] = _item;
                arrayIndex = 1;
            }

            EnumerableHelpers.Copy(_source, array, arrayIndex, count - 1);
            if (_appending)
            {
                array[array.Length - 1] = _item;
            }

            return array;
        }

        public override List<TSource> ToList()
        {
            var count = GetCount(true);
            var list = count == -1 ? new List<TSource>() : new List<TSource>(count);
            if (!_appending)
            {
                list.Add(_item);
            }

            list.AddRange(_source);
            if (_appending)
            {
                list.Add(_item);
            }

            return list;
        }

        private TSource[] LazyToArray()
        {
            var largeArrayBuilder = new LargeArrayBuilder<TSource>(true);
            if (!_appending)
            {
                largeArrayBuilder.SlowAdd(_item);
            }

            largeArrayBuilder.AddRange(_source);
            if (_appending)
            {
                largeArrayBuilder.SlowAdd(_item);
            }

            return largeArrayBuilder.ToArray();
        }
    }

    private class AppendPrependN<TSource> : AppendPrependIterator<TSource>
    {
        private readonly int _appendCount;
        private readonly SingleLinkedNode<TSource> _appended;
        private readonly int _prependCount;
        private readonly SingleLinkedNode<TSource> _prepended;
        private SingleLinkedNode<TSource> _node;

        public AppendPrependN(
            IEnumerable<TSource> source,
            SingleLinkedNode<TSource> prepended,
            SingleLinkedNode<TSource> appended,
            int prependCount,
            int appendCount)
            : base(source)
        {
            _prepended = prepended;
            _appended = appended;
            _prependCount = prependCount;
            _appendCount = appendCount;
        }

        public override AppendPrependIterator<TSource> Append(TSource item)
        {
            return new AppendPrependN<TSource>(_source, _prepended,
                _appended != null ? _appended.Add(item) : new SingleLinkedNode<TSource>(item), _prependCount,
                _appendCount + 1);
        }

        public override Iterator<TSource> Clone()
        {
            return new AppendPrependN<TSource>(_source, _prepended, _appended, _prependCount, _appendCount);
        }

        public override int GetCount(bool onlyIfCheap)
        {
            if (_source is IIListProvider<TSource> source)
            {
                var count = source.GetCount(onlyIfCheap);
                return count != -1 ? count + _appendCount + _prependCount : -1;
            }

            return onlyIfCheap && !(_source is ICollection<TSource>)
                ? -1
                : _source.Count() + _appendCount + _prependCount;
        }

        public override bool MoveNext()
        {
            switch (state)
            {
                case 1:
                    _node = _prepended;
                    state = 2;
                    goto case 2;
                case 2:
                    if (_node != null)
                    {
                        current = _node.Item;
                        _node = _node.Linked;
                        return true;
                    }

                    GetSourceEnumerator();
                    state = 3;
                    goto case 3;
                case 3:
                    if (LoadFromEnumerator())
                    {
                        return true;
                    }

                    if (_appended == null)
                    {
                        return false;
                    }

                    enumerator = _appended.GetEnumerator(_appendCount);
                    state = 4;
                    goto case 4;
                case 4:
                    return LoadFromEnumerator();
                default:
                    Dispose();
                    return false;
            }
        }

        public override AppendPrependIterator<TSource> Prepend(TSource item)
        {
            return new AppendPrependN<TSource>(_source,
                _prepended != null ? _prepended.Add(item) : new SingleLinkedNode<TSource>(item), _appended,
                _prependCount + 1, _appendCount);
        }

        public override TSource[] ToArray()
        {
            var count = GetCount(true);
            if (count == -1)
            {
                return LazyToArray();
            }

            var array = new TSource[count];
            var arrayIndex = 0;
            for (var singleLinkedNode = _prepended;
                 singleLinkedNode != null;
                 singleLinkedNode = singleLinkedNode.Linked)
            {
                array[arrayIndex] = singleLinkedNode.Item;
                ++arrayIndex;
            }

            if (_source is ICollection<TSource> source1)
            {
                source1.CopyTo(array, arrayIndex);
            }
            else
            {
                foreach (var source in _source)
                {
                    array[arrayIndex] = source;
                    ++arrayIndex;
                }
            }

            var length = array.Length;
            for (var singleLinkedNode = _appended; singleLinkedNode != null; singleLinkedNode = singleLinkedNode.Linked)
            {
                --length;
                array[length] = singleLinkedNode.Item;
            }

            return array;
        }

        public override List<TSource> ToList()
        {
            var count = GetCount(true);
            var list = count == -1 ? new List<TSource>() : new List<TSource>(count);
            for (var singleLinkedNode = _prepended;
                 singleLinkedNode != null;
                 singleLinkedNode = singleLinkedNode.Linked)
            {
                list.Add(singleLinkedNode.Item);
            }

            list.AddRange(_source);
            if (_appended != null)
            {
                var enumerator = _appended.GetEnumerator(_appendCount);
                while (enumerator.MoveNext())
                {
                    list.Add(enumerator.Current);
                }
            }

            return list;
        }

        private TSource[] LazyToArray()
        {
            var sparseArrayBuilder = new SparseArrayBuilder<TSource>(true);
            if (_prepended != null)
            {
                sparseArrayBuilder.Reserve(_prependCount);
            }

            sparseArrayBuilder.AddRange(_source);
            if (_appended != null)
            {
                sparseArrayBuilder.Reserve(_appendCount);
            }

            var array = sparseArrayBuilder.ToArray();
            var num1 = 0;
            for (var singleLinkedNode = _prepended;
                 singleLinkedNode != null;
                 singleLinkedNode = singleLinkedNode.Linked)
            {
                array[num1++] = singleLinkedNode.Item;
            }

            var num2 = array.Length - 1;
            for (var singleLinkedNode = _appended; singleLinkedNode != null; singleLinkedNode = singleLinkedNode.Linked)
            {
                array[num2--] = singleLinkedNode.Item;
            }

            return array;
        }
    }

#pragma warning disable CS3002
#pragma warning disable CS3003
    public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
    {
        return source.ToHashSet(null);
    }

    public static HashSet<TSource> ToHashSet<TSource>(
        this IEnumerable<TSource> source,
        IEqualityComparer<TSource> comparer)
    {
        return source != null ? new HashSet<TSource>(source, comparer) : throw Error.ArgumentNull(nameof(source));
    }
#pragma warning restore CS3003
#pragma warning restore CS3002
}