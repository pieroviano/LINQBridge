using System.Threading;

namespace System;

internal static class OperationCanceledExceptionExt5ension
{
    extension(OperationCanceledException operationCanceledException)
    {
        public CancellationToken CancellationToken => CancellationToken.None;
    }
}