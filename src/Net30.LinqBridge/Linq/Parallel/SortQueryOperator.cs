#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class SortQueryOperator<TInputOutput, TSortKey> :
    UnaryQueryOperator<TInputOutput, TInputOutput>,
    IOrderedEnumerable<TInputOutput>,
    IEnumerable<TInputOutput>,
    IEnumerable
{
    internal SortQueryOperator(
        IEnumerable<TInputOutput> source,
        Func<TInputOutput, TSortKey> keySelector,
        IComparer<TSortKey> comparer,
        bool descending)
        : base(source, true)
    {
        KeySelector = keySelector;
        KeyComparer = comparer != null ? comparer : Util.GetDefaultComparer<TSortKey>();
        if (descending)
        {
            KeyComparer = new ReverseComparer<TSortKey>(KeyComparer);
        }

        SetOrdinalIndexState(OrdinalIndexState.Shuffled);
    }

    internal Func<TInputOutput, TSortKey> KeySelector { get; }

    internal IComparer<TSortKey> KeyComparer { get; }

    internal override bool LimitsParallelism => false;

    IOrderedEnumerable<TInputOutput> IOrderedEnumerable<TInputOutput>.CreateOrderedEnumerable<TKey2>(
        Func<TInputOutput, TKey2> key2Selector,
        IComparer<TKey2> key2Comparer,
        bool descending)
    {
        key2Comparer = key2Comparer ?? Util.GetDefaultComparer<TKey2>();
        if (descending)
        {
            key2Comparer = new ReverseComparer<TKey2>(key2Comparer);
        }

        return new SortQueryOperator<TInputOutput, Pair<TSortKey, TKey2>>(Child,
            elem => new Pair<TSortKey, TKey2>(KeySelector(elem), key2Selector(elem)),
            new PairComparer<TSortKey, TKey2>(KeyComparer, key2Comparer), false);
    }

    internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
    {
        return CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token).OrderBy(KeySelector, KeyComparer);
    }

    internal override QueryResults<TInputOutput> Open(QuerySettings settings, bool preferStriping)
    {
        return new SortQueryOperatorResults<TInputOutput, TSortKey>(Child.Open(settings, false), this, settings,
            preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TInputOutput, TKey> inputStream,
        IPartitionedStreamRecipient<TInputOutput> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionedStream =
            new PartitionedStream<TInputOutput, TSortKey>(inputStream.PartitionCount, KeyComparer, OrdinalIndexState);
        for (var index = 0; index < partitionedStream.PartitionCount; ++index)
        {
            partitionedStream[index] =
                new SortQueryOperatorEnumerator<TInputOutput, TKey, TSortKey>(inputStream[index], KeySelector,
                    KeyComparer);
        }

        recipient.Receive(partitionedStream);
    }
}