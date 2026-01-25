#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal abstract class InlinedAggregationOperator<TSource, TIntermediate, TResult> :
    UnaryQueryOperator<TSource, TIntermediate>
{
    internal InlinedAggregationOperator(IEnumerable<TSource> child)
        : base(child)
    {
    }

    internal override bool LimitsParallelism => false;

    protected abstract QueryOperatorEnumerator<TIntermediate, int> CreateEnumerator<TKey>(
        int index,
        int count,
        QueryOperatorEnumerator<TSource, TKey> source,
        object sharedData,
        CancellationToken cancellationToken);

    protected abstract TResult InternalAggregate(ref Exception singularExceptionToThrow);

    internal TResult Aggregate()
    {
        var singularExceptionToThrow = (Exception)null;
        TResult result;
        try
        {
            result = InternalAggregate(ref singularExceptionToThrow);
        }
        catch (ThreadAbortException ex)
        {
            throw;
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case AggregateException _:
                    throw;
                case OperationCanceledException canceledException:
                    if (canceledException.CancellationToken ==
                        SpecifiedQuerySettings.CancellationState.ExternalCancellationToken && SpecifiedQuerySettings
                            .CancellationState.ExternalCancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    break;
            }

            throw new AggregateException(ex);
        }

        if (singularExceptionToThrow != null)
        {
            throw singularExceptionToThrow;
        }

        return result;
    }

    internal override IEnumerable<TIntermediate> AsSequentialQuery(CancellationToken token)
    {
        throw new NotSupportedException();
    }

    internal override QueryResults<TIntermediate> Open(QuerySettings settings, bool preferStriping)
    {
        return new UnaryQueryOperatorResults(Child.Open(settings, preferStriping), this, settings, preferStriping);
    }

    internal override void WrapPartitionedStream<TKey>(
        PartitionedStream<TSource, TKey> inputStream,
        IPartitionedStreamRecipient<TIntermediate> recipient,
        bool preferStriping,
        QuerySettings settings)
    {
        var partitionCount = inputStream.PartitionCount;
        var partitionedStream = new PartitionedStream<TIntermediate, int>(partitionCount,
            Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);
        for (var index = 0; index < partitionCount; ++index)
        {
            partitionedStream[index] = CreateEnumerator(index, partitionCount, inputStream[index], null,
                settings.CancellationState.MergedCancellationToken);
        }

        recipient.Receive(partitionedStream);
    }
}