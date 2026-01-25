#nullable disable
using System.Linq.Expressions;
using System.Threading;

namespace System.Linq.Parallel;

internal class CancellationState
{
    internal const int POLL_INTERVAL = 63 /*0x3F*/;
    internal CancellationToken ExternalCancellationToken;
    internal CancellationTokenSource InternalCancellationTokenSource;
    internal CancellationTokenSource MergedCancellationTokenSource;
    internal Shared<bool> TopLevelDisposedFlag;

    internal CancellationState(CancellationToken externalCancellationToken)
    {
        ExternalCancellationToken = externalCancellationToken;
        TopLevelDisposedFlag = new Shared<bool>(false);
    }

    internal CancellationToken MergedCancellationToken => MergedCancellationTokenSource != null
        ? MergedCancellationTokenSource.Token
        : new CancellationToken(false);

    internal static void ThrowIfCanceled(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            throw new OperationCanceledException();
        }
    }

    internal static void ThrowWithStandardMessageIfCanceled(
        CancellationToken externalCancellationToken)
    {
        if (externalCancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(Strings.PLINQ_ExternalCancellationRequested());
        }
    }
}