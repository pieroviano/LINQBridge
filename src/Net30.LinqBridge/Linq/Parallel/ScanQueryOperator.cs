#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ScanQueryOperator<TElement> : QueryOperator<TElement>
{
    internal ScanQueryOperator(IEnumerable<TElement> data)
        : base(false, QuerySettings.Empty)
    {
        if (data is ParallelEnumerableWrapper<TElement> enumerableWrapper)
        {
            data = enumerableWrapper.WrappedEnumerable;
        }

        Data = data;
    }

    public IEnumerable<TElement> Data { get; }

    internal override OrdinalIndexState OrdinalIndexState =>
        !(Data is IList<TElement>) ? OrdinalIndexState.Correct : OrdinalIndexState.Indexible;

    internal override bool LimitsParallelism => false;

    internal override IEnumerable<TElement> AsSequentialQuery(CancellationToken token)
    {
        return Data;
    }

    internal override IEnumerator<TElement> GetEnumerator(
        ParallelMergeOptions? mergeOptions,
        bool suppressOrderPreservation)
    {
        return Data.GetEnumerator();
    }

    internal override QueryResults<TElement> Open(QuerySettings settings, bool preferStriping)
    {
        if (!(Data is IList<TElement> data))
        {
            return new ScanEnumerableQueryOperatorResults(Data, settings);
        }

        var valueOrDefault = settings.DegreeOfParallelism.GetValueOrDefault();
        var num = preferStriping ? 1 : 0;
        return new ListQueryResults<TElement>(data, valueOrDefault, num != 0);
    }

    private class ScanEnumerableQueryOperatorResults : QueryResults<TElement>
    {
        private readonly IEnumerable<TElement> m_data;
        private QuerySettings m_settings;

        internal ScanEnumerableQueryOperatorResults(IEnumerable<TElement> data, QuerySettings settings)
        {
            m_data = data;
            m_settings = settings;
        }

        internal override void GivePartitionedStream(IPartitionedStreamRecipient<TElement> recipient)
        {
            var partitionedStream =
                ExchangeUtilities.PartitionDataSource(m_data, m_settings.DegreeOfParallelism.Value, false);
            recipient.Receive(partitionedStream);
        }
    }
}