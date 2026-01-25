#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal static class CancellableEnumerable
{
    internal static IEnumerable<TElement> Wrap<TElement>(
        IEnumerable<TElement> source,
        CancellationToken token)
    {
        var count = 0;
        foreach (var element in source)
        {
            if ((count++ & 63 /*0x3F*/) == 0)
            {
                CancellationState.ThrowIfCanceled(token);
            }

            yield return element;
        }
    }
}