#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace System.Linq.Parallel;

internal class QueryOpeningEnumerator<TOutput> : IEnumerator<TOutput>, IDisposable, IEnumerator
{
    private readonly ParallelMergeOptions? m_mergeOptions;
    private readonly QueryOperator<TOutput> m_queryOperator;
    private readonly bool m_suppressOrderPreservation;
    private readonly CancellationTokenSource m_topLevelCancellationTokenSource = new();
    private readonly Shared<bool> m_topLevelDisposedFlag = new(false);
    private bool m_hasQueryOpeningFailed;
    private int m_moveNextIteration;
    private IEnumerator<TOutput> m_openedQueryEnumerator;
    private QuerySettings m_querySettings;

    internal QueryOpeningEnumerator(
        QueryOperator<TOutput> queryOperator,
        ParallelMergeOptions? mergeOptions,
        bool suppressOrderPreservation)
    {
        m_queryOperator = queryOperator;
        m_mergeOptions = mergeOptions;
        m_suppressOrderPreservation = suppressOrderPreservation;
    }

    public TOutput Current => m_openedQueryEnumerator != null
        ? m_openedQueryEnumerator.Current
        : throw new InvalidOperationException(Strings.PLINQ_CommonEnumerator_Current_NotStarted());

    public void Dispose()
    {
        m_topLevelDisposedFlag.Value = true;
        m_topLevelCancellationTokenSource.Cancel();
        if (m_openedQueryEnumerator != null)
        {
            m_openedQueryEnumerator.Dispose();
            m_querySettings.CleanStateAtQueryEnd();
        }

        QueryLifecycle.LogicalQueryExecutionEnd(m_querySettings.QueryId);
    }

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (m_topLevelDisposedFlag.Value)
        {
            throw new ObjectDisposedException("enumerator", Strings.PLINQ_DisposeRequested());
        }

        if (m_openedQueryEnumerator == null)
        {
            OpenQuery();
        }

        var flag = m_openedQueryEnumerator.MoveNext();
        if ((m_moveNextIteration & 63 /*0x3F*/) == 0)
        {
            CancellationState.ThrowWithStandardMessageIfCanceled(m_querySettings.CancellationState
                .ExternalCancellationToken);
        }

        ++m_moveNextIteration;
        return flag;
    }

    public void Reset()
    {
        throw new NotSupportedException();
    }

    private void OpenQuery()
    {
        if (m_hasQueryOpeningFailed)
        {
            throw new InvalidOperationException(Strings.PLINQ_EnumerationPreviouslyFailed());
        }

        try
        {
            m_querySettings = m_queryOperator.SpecifiedQuerySettings
                .WithPerExecutionSettings(m_topLevelCancellationTokenSource, m_topLevelDisposedFlag).WithDefaults();
            QueryLifecycle.LogicalQueryExecutionBegin(m_querySettings.QueryId);
            m_openedQueryEnumerator =
                m_queryOperator.GetOpenedEnumerator(m_mergeOptions, m_suppressOrderPreservation, false,
                    m_querySettings);
            CancellationState.ThrowWithStandardMessageIfCanceled(m_querySettings.CancellationState
                .ExternalCancellationToken);
        }
        catch
        {
            m_hasQueryOpeningFailed = true;
            throw;
        }
    }
}