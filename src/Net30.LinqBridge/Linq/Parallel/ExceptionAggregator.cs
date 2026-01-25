using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal static class ExceptionAggregator
{
    internal static void ThrowOCEorAggregateException(Exception ex, CancellationState cancellationState)
    {
        if (!ThrowAnOCE(ex, cancellationState))
        {
            throw new AggregateException(ex);
        }

        CancellationState.ThrowWithStandardMessageIfCanceled(cancellationState.ExternalCancellationToken);
    }

    internal static IEnumerable<TElement> WrapEnumerable<TElement>(IEnumerable<TElement> source,
        CancellationState cancellationState)
    {
        using (var enumerator = source.GetEnumerator())
        {
            while (true)
            {
                var current = default(TElement);
                try
                {
                    if (enumerator.MoveNext())
                    {
                        current = enumerator.Current;
                    }
                    else
                    {
                        break;
                    }
                }
                catch (ThreadAbortException threadAbortException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    ThrowOCEorAggregateException(exception, cancellationState);
                }

                yield return current;
            }
        }
    }

    internal static Func<T, U> WrapFunc<T, U>(Func<T, U> f, CancellationState cancellationState)
    {
        return t =>
        {
            var u = default(U);
            try
            {
                u = f(t);
            }
            catch (ThreadAbortException threadAbortException)
            {
                throw;
            }
            catch (Exception exception)
            {
                ThrowOCEorAggregateException(exception, cancellationState);
            }

            return u;
        };
    }

    internal static IEnumerable<TElement> WrapQueryEnumerator<TElement, TIgnoreKey>(
        QueryOperatorEnumerator<TElement, TIgnoreKey> source, CancellationState cancellationState)
    {
        var tElement = default(TElement);
        var tIgnoreKey = default(TIgnoreKey);
        try
        {
            while (true)
            {
                try
                {
                    if (!source.MoveNext(ref tElement, ref tIgnoreKey))
                    {
                        break;
                    }
                }
                catch (ThreadAbortException threadAbortException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    ThrowOCEorAggregateException(exception, cancellationState);
                }

                yield return tElement;
            }
        }
        finally
        {
            source.Dispose();
        }
    }

    private static bool ThrowAnOCE(Exception ex, CancellationState cancellationState)
    {
        var operationCanceledException = ex as OperationCanceledException;
        if (operationCanceledException != null &&
            operationCanceledException.CancellationToken == cancellationState.ExternalCancellationToken &&
            cancellationState.ExternalCancellationToken.IsCancellationRequested)
        {
            return true;
        }

        if (operationCanceledException != null &&
            operationCanceledException.CancellationToken == cancellationState.MergedCancellationToken &&
            cancellationState.MergedCancellationToken.IsCancellationRequested &&
            cancellationState.ExternalCancellationToken.IsCancellationRequested)
        {
            return true;
        }

        return false;
    }
}