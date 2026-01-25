#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class JoinQueryOperator<TLeftInput, TRightInput, TKey, TOutput> :
    BinaryQueryOperator<TLeftInput, TRightInput, TOutput>
{
    private readonly IEqualityComparer<TKey> m_keyComparer;
    private readonly Func<TLeftInput, TKey> m_leftKeySelector;
    private readonly Func<TLeftInput, TRightInput, TOutput> m_resultSelector;
    private readonly Func<TRightInput, TKey> m_rightKeySelector;

    internal JoinQueryOperator(
        ParallelQuery<TLeftInput> left,
        ParallelQuery<TRightInput> right,
        Func<TLeftInput, TKey> leftKeySelector,
        Func<TRightInput, TKey> rightKeySelector,
        Func<TLeftInput, TRightInput, TOutput> resultSelector,
        IEqualityComparer<TKey> keyComparer)
        : base(left, right)
    {
        m_leftKeySelector = leftKeySelector;
        m_rightKeySelector = rightKeySelector;
        m_resultSelector = resultSelector;
        m_keyComparer = keyComparer;
        m_outputOrdered = LeftChild.OutputOrdered;
        SetOrdinalIndex(OrdinalIndexState.Shuffled);
    }

    internal override bool LimitsParallelism => false;

    public override void WrapPartitionedStream<TLeftKey, TRightKey>(
        PartitionedStream<TLeftInput, TLeftKey> leftStream,
        PartitionedStream<TRightInput, TRightKey> rightStream,
        IPartitionedStreamRecipient<TOutput> outputRecipient,
        bool preferStriping,
        QuerySettings settings)
    {
        if (LeftChild.OutputOrdered)
        {
            WrapPartitionedStreamHelper(
                ExchangeUtilities.HashRepartitionOrdered(leftStream, m_leftKeySelector, m_keyComparer, null,
                    settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient,
                settings.CancellationState.MergedCancellationToken);
        }
        else
        {
            WrapPartitionedStreamHelper(
                ExchangeUtilities.HashRepartition(leftStream, m_leftKeySelector, m_keyComparer, null,
                    settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient,
                settings.CancellationState.MergedCancellationToken);
        }
    }

    internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
    {
        return CancellableEnumerable.Wrap(LeftChild.AsSequentialQuery(token), token).Join(
            CancellableEnumerable.Wrap(RightChild.AsSequentialQuery(token), token), m_leftKeySelector,
            m_rightKeySelector, m_resultSelector, m_keyComparer);
    }

    internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
    {
        return new BinaryQueryOperatorResults(LeftChild.Open(settings, false), RightChild.Open(settings, false), this,
            settings, false);
    }

    private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(
        PartitionedStream<Pair<TLeftInput, TKey>, TLeftKey> leftHashStream,
        PartitionedStream<TRightInput, TRightKey> rightPartitionedStream,
        IPartitionedStreamRecipient<TOutput> outputRecipient,
        CancellationToken cancellationToken)
    {
        var partitionCount = leftHashStream.PartitionCount;
        var partitionedStream1 = ExchangeUtilities.HashRepartition(rightPartitionedStream, m_rightKeySelector,
            m_keyComparer, null, cancellationToken);
        var partitionedStream2 =
            new PartitionedStream<TOutput, TLeftKey>(partitionCount, leftHashStream.KeyComparer, OrdinalIndexState);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream2[index] =
                new HashJoinQueryOperatorEnumerator<TLeftInput, TLeftKey, TRightInput, TKey, TOutput>(
                    leftHashStream[index], partitionedStream1[index], m_resultSelector, null, m_keyComparer,
                    cancellationToken);
        }

        outputRecipient.Receive(partitionedStream2);
    }
}