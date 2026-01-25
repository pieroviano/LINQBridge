#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> :
    UnaryQueryOperator<TLeftInput, TOutput>
{
    private readonly Func<TLeftInput, int, IEnumerable<TRightInput>> m_indexedRightChildSelector;
    private readonly Func<TLeftInput, TRightInput, TOutput> m_resultSelector;
    private readonly Func<TLeftInput, IEnumerable<TRightInput>> m_rightChildSelector;
    private bool m_limitsParallelism;
    private bool m_prematureMerge;

    internal SelectManyQueryOperator(
        IEnumerable<TLeftInput> leftChild,
        Func<TLeftInput, IEnumerable<TRightInput>> rightChildSelector,
        Func<TLeftInput, int, IEnumerable<TRightInput>> indexedRightChildSelector,
        Func<TLeftInput, TRightInput, TOutput> resultSelector)
        : base(leftChild)
    {
        m_rightChildSelector = rightChildSelector;
        m_indexedRightChildSelector = indexedRightChildSelector;
        m_resultSelector = resultSelector;
        m_outputOrdered = Child.OutputOrdered || indexedRightChildSelector != null;
        InitOrderIndex();
    }

    internal override bool LimitsParallelism => m_limitsParallelism;

    internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
    {
        return m_rightChildSelector != null
            ?
            m_resultSelector != null
                ? CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token)
                    .SelectMany(m_rightChildSelector, m_resultSelector)
                : (IEnumerable<TOutput>)CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token)
                    .SelectMany(m_rightChildSelector)
            : m_resultSelector != null
                ? CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token)
                    .SelectMany(m_indexedRightChildSelector, m_resultSelector)
                : (IEnumerable<TOutput>)CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token)
                    .SelectMany(m_indexedRightChildSelector);
    }

    internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, preferStriping), this, settings, preferStriping);
    }

    internal override void WrapPartitionedStream<TLeftKey>(
        PartitionedStream<TLeftInput, TLeftKey> inputStream,
        IPartitionedStreamRecipient<TOutput> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        if (m_indexedRightChildSelector != null)
        {
            WrapPartitionedStreamIndexed(
                !m_prematureMerge
                    ? (PartitionedStream<TLeftInput, int>)(object)inputStream
                    : QueryOperator<TLeftInput>
                        .ExecuteAndCollectResults(inputStream, partitionCount, OutputOrdered, preferStriping, settings)
                        .GetPartitionedStream(), recipient, settings);
        }
        else if (m_prematureMerge)
        {
            WrapPartitionedStreamNotIndexed(
                QueryOperator<TLeftInput>
                    .ExecuteAndCollectResults(inputStream, partitionCount, OutputOrdered, preferStriping, settings)
                    .GetPartitionedStream(), recipient, settings);
        }
        else
        {
            WrapPartitionedStreamNotIndexed(inputStream, recipient, settings);
        }
    }

    private void InitOrderIndex()
    {
        var ordinalIndexState = Child.OrdinalIndexState;
        if (m_indexedRightChildSelector != null)
        {
            m_prematureMerge = ordinalIndexState.IsWorseThan(OrdinalIndexState.Correct);
            m_limitsParallelism = m_prematureMerge && ordinalIndexState != OrdinalIndexState.Shuffled;
        }
        else if (OutputOrdered)
        {
            m_prematureMerge = ordinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);
        }

        SetOrdinalIndexState(OrdinalIndexState.Increasing);
    }

    private void WrapPartitionedStreamIndexed(
        PartitionedStream<TLeftInput, int> inputStream,
        IPartitionedStreamRecipient<TOutput> recipient,
        QuerySettings settings)
    {
        var keyComparer = new PairComparer<int, int>(inputStream.KeyComparer, Util.GetDefaultComparer<int>());
        var partitionedStream =
            new PartitionedStream<TOutput, Pair<int, int>>(inputStream.PartitionCount, keyComparer, OrdinalIndexState);
        for (var index = 0; index < inputStream.PartitionCount; ++index)
        {
            partitionedStream[index] = new IndexedSelectManyQueryOperatorEnumerator(inputStream[index], this,
                settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream);
    }

    private void WrapPartitionedStreamNotIndexed<TLeftKey>(
        PartitionedStream<TLeftInput, TLeftKey> inputStream,
        IPartitionedStreamRecipient<TOutput> recipient,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var keyComparer = new PairComparer<TLeftKey, int>(inputStream.KeyComparer, Util.GetDefaultComparer<int>());
        var partitionedStream =
            new PartitionedStream<TOutput, Pair<TLeftKey, int>>(partitionCount, keyComparer, OrdinalIndexState);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = new SelectManyQueryOperatorEnumerator<TLeftKey>(inputStream[index], this,
                settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream);
    }

    private class IndexedSelectManyQueryOperatorEnumerator :
        QueryOperatorEnumerator<TOutput, Pair<int, int>>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly QueryOperatorEnumerator<TLeftInput, int> m_leftSource;
        private readonly SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> m_selectManyOperator;
        private IEnumerator<TRightInput> m_currentRightSource;
        private IEnumerator<TOutput> m_currentRightSourceAsOutput;
        private Mutables m_mutables;

        internal IndexedSelectManyQueryOperatorEnumerator(
            QueryOperatorEnumerator<TLeftInput, int> leftSource,
            SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> selectManyOperator,
            CancellationToken cancellationToken)
        {
            m_leftSource = leftSource;
            m_selectManyOperator = selectManyOperator;
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_leftSource.Dispose();
            if (m_currentRightSource == null)
            {
                return;
            }

            m_currentRightSource.Dispose();
        }

        internal override bool MoveNext(ref TOutput currentElement, ref Pair<int, int> currentKey)
        {
            while (true)
            {
                if (m_currentRightSource == null)
                {
                    m_mutables = new Mutables();
                    if ((m_mutables.m_lhsCount++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (m_leftSource.MoveNext(ref m_mutables.m_currentLeftElement,
                            ref m_mutables.m_currentLeftSourceIndex))
                    {
                        m_currentRightSource = m_selectManyOperator
                            .m_indexedRightChildSelector(m_mutables.m_currentLeftElement,
                                m_mutables.m_currentLeftSourceIndex).GetEnumerator();
                        if (m_selectManyOperator.m_resultSelector == null)
                        {
                            m_currentRightSourceAsOutput = (IEnumerator<TOutput>)m_currentRightSource;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (!m_currentRightSource.MoveNext())
                {
                    m_currentRightSource.Dispose();
                    m_currentRightSource = null;
                    m_currentRightSourceAsOutput = null;
                }
                else
                {
                    goto label_8;
                }
            }

            return false;
            label_8:
            ++m_mutables.m_currentRightSourceIndex;
            currentElement = m_selectManyOperator.m_resultSelector == null
                ? m_currentRightSourceAsOutput.Current
                : m_selectManyOperator.m_resultSelector(m_mutables.m_currentLeftElement, m_currentRightSource.Current);
            currentKey = new Pair<int, int>(m_mutables.m_currentLeftSourceIndex, m_mutables.m_currentRightSourceIndex);
            return true;
        }

        private class Mutables
        {
            internal TLeftInput m_currentLeftElement;
            internal int m_currentLeftSourceIndex;
            internal int m_currentRightSourceIndex = -1;
            internal int m_lhsCount;
        }
    }

    private class SelectManyQueryOperatorEnumerator<TLeftKey> :
        QueryOperatorEnumerator<TOutput, Pair<TLeftKey, int>>
    {
        private readonly CancellationToken m_cancellationToken;
        private readonly QueryOperatorEnumerator<TLeftInput, TLeftKey> m_leftSource;
        private readonly SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> m_selectManyOperator;
        private IEnumerator<TRightInput> m_currentRightSource;
        private IEnumerator<TOutput> m_currentRightSourceAsOutput;
        private Mutables m_mutables;

        internal SelectManyQueryOperatorEnumerator(
            QueryOperatorEnumerator<TLeftInput, TLeftKey> leftSource,
            SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> selectManyOperator,
            CancellationToken cancellationToken)
        {
            m_leftSource = leftSource;
            m_selectManyOperator = selectManyOperator;
            m_cancellationToken = cancellationToken;
        }

        protected override void Dispose(bool disposing)
        {
            m_leftSource.Dispose();
            if (m_currentRightSource == null)
            {
                return;
            }

            m_currentRightSource.Dispose();
        }

        internal override bool MoveNext(ref TOutput currentElement, ref Pair<TLeftKey, int> currentKey)
        {
            while (true)
            {
                if (m_currentRightSource == null)
                {
                    m_mutables = new Mutables();
                    if ((m_mutables.m_lhsCount++ & 63 /*0x3F*/) == 0)
                    {
                        CancellationState.ThrowIfCanceled(m_cancellationToken);
                    }

                    if (m_leftSource.MoveNext(ref m_mutables.m_currentLeftElement, ref m_mutables.m_currentLeftKey))
                    {
                        m_currentRightSource = m_selectManyOperator
                            .m_rightChildSelector(m_mutables.m_currentLeftElement).GetEnumerator();
                        if (m_selectManyOperator.m_resultSelector == null)
                        {
                            m_currentRightSourceAsOutput = (IEnumerator<TOutput>)m_currentRightSource;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (!m_currentRightSource.MoveNext())
                {
                    m_currentRightSource.Dispose();
                    m_currentRightSource = null;
                    m_currentRightSourceAsOutput = null;
                }
                else
                {
                    goto label_8;
                }
            }

            return false;
            label_8:
            ++m_mutables.m_currentRightSourceIndex;
            currentElement = m_selectManyOperator.m_resultSelector == null
                ? m_currentRightSourceAsOutput.Current
                : m_selectManyOperator.m_resultSelector(m_mutables.m_currentLeftElement, m_currentRightSource.Current);
            currentKey = new Pair<TLeftKey, int>(m_mutables.m_currentLeftKey, m_mutables.m_currentRightSourceIndex);
            return true;
        }

        private class Mutables
        {
            internal TLeftInput m_currentLeftElement;
            internal TLeftKey m_currentLeftKey;
            internal int m_currentRightSourceIndex = -1;
            internal int m_lhsCount;
        }
    }
}