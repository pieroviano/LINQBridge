#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class GroupByQueryOperator<TSource, TGroupKey, TElement> :
    UnaryQueryOperator<TSource, IGrouping<TGroupKey, TElement>>
{
    private readonly Func<TSource, TElement> m_elementSelector;
    private readonly IEqualityComparer<TGroupKey> m_keyComparer;
    private readonly Func<TSource, TGroupKey> m_keySelector;

    internal GroupByQueryOperator(
        IEnumerable<TSource> child,
        Func<TSource, TGroupKey> keySelector,
        Func<TSource, TElement> elementSelector,
        IEqualityComparer<TGroupKey> keyComparer)
        : base(child)
    {
        m_keySelector = keySelector;
        m_elementSelector = elementSelector;
        m_keyComparer = keyComparer;
        SetOrdinalIndexState(OrdinalIndexState.Shuffled);
    }

    internal override bool LimitsParallelism => false;

    internal override IEnumerable<IGrouping<TGroupKey, TElement>> AsSequentialQuery(
        CancellationToken token)
    {
        var source = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
        return m_elementSelector == null
            ? (IEnumerable<IGrouping<TGroupKey, TElement>>)source.GroupBy(m_keySelector, m_keyComparer)
            : source.GroupBy(m_keySelector, m_elementSelector, m_keyComparer);
    }

    internal override QueryResults<IGrouping<TGroupKey, TElement>> Open(
        QuerySettings settings,
        bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, false), this, settings, false);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TSource, TKey> inputStream,
        IPartitionedStreamRecipient<IGrouping<TGroupKey, TElement>> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        if (Child.OutputOrdered)
        {
            WrapPartitionedStreamHelperOrdered(
                ExchangeUtilities.HashRepartitionOrdered(inputStream, m_keySelector, m_keyComparer, null,
                    settings.CancellationState.MergedCancellationToken), recipient,
                settings.CancellationState.MergedCancellationToken);
        }
        else
        {
            WrapPartitionedStreamHelper<TKey, int>(
                ExchangeUtilities.HashRepartition(inputStream, m_keySelector, m_keyComparer, null,
                    settings.CancellationState.MergedCancellationToken), recipient,
                settings.CancellationState.MergedCancellationToken);
        }
    }

    private void WrapPartitionedStreamHelper<TIgnoreKey, TKey>(
        PartitionedStream<Pair<TSource, TGroupKey>, TKey> hashStream,
        IPartitionedStreamRecipient<IGrouping<TGroupKey, TElement>> recipient,
        CancellationToken cancellationToken)
    {
        var partitionCount = hashStream.PartitionCount;
        var partitionedStream =
            new PartitionedStream<IGrouping<TGroupKey, TElement>, TKey>(partitionCount, hashStream.KeyComparer,
                OrdinalIndexState.Shuffled);
        for (var index = 0; index < partitionCount; ++index)
        {
            if (m_elementSelector == null)
            {
                var operatorEnumerator =
                    new GroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TKey>(hashStream[index],
                        m_keyComparer, cancellationToken);
                partitionedStream[index] =
                    (QueryOperatorEnumerator<IGrouping<TGroupKey, TElement>, TKey>)(object)operatorEnumerator;
            }
            else
            {
                partitionedStream[index] =
                    new GroupByElementSelectorQueryOperatorEnumerator<TSource, TGroupKey, TElement, TKey>(
                        hashStream[index], m_keyComparer, m_elementSelector, cancellationToken);
            }
        }

        recipient.Receive(partitionedStream);
    }

    private void WrapPartitionedStreamHelperOrdered<TKey>(
        PartitionedStream<Pair<TSource, TGroupKey>, TKey> hashStream,
        IPartitionedStreamRecipient<IGrouping<TGroupKey, TElement>> recipient,
        CancellationToken cancellationToken)
    {
        var partitionCount = hashStream.PartitionCount;
        var partitionedStream =
            new PartitionedStream<IGrouping<TGroupKey, TElement>, TKey>(partitionCount, hashStream.KeyComparer,
                OrdinalIndexState.Shuffled);
        var keyComparer = hashStream.KeyComparer;
        for (var index = 0; index < partitionCount; ++index)
        {
            if (m_elementSelector == null)
            {
                var operatorEnumerator =
                    new OrderedGroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TKey>(hashStream[index],
                        m_keySelector, m_keyComparer, keyComparer, cancellationToken);
                partitionedStream[index] =
                    (QueryOperatorEnumerator<IGrouping<TGroupKey, TElement>, TKey>)(object)operatorEnumerator;
            }
            else
            {
                partitionedStream[index] =
                    new OrderedGroupByElementSelectorQueryOperatorEnumerator<TSource, TGroupKey, TElement, TKey>(
                        hashStream[index], m_keySelector, m_elementSelector, m_keyComparer, keyComparer,
                        cancellationToken);
            }
        }

        recipient.Receive(partitionedStream);
    }
}