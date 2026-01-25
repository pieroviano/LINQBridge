#nullable disable
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Properties;
using System.Threading;

namespace System.Linq.Parallel;

internal class PartitionerQueryOperator<TElement> : QueryOperator<TElement>
{
    private readonly Partitioner<TElement> m_partitioner;

    internal PartitionerQueryOperator(Partitioner<TElement> partitioner)
        : base(false, QuerySettings.Empty)
    {
        m_partitioner = partitioner;
    }

    internal bool Orderable => m_partitioner is OrderablePartitioner<TElement>;

    internal override OrdinalIndexState OrdinalIndexState => GetOrdinalIndexState(m_partitioner);

    internal override bool LimitsParallelism => false;

    internal override IEnumerable<TElement> AsSequentialQuery(CancellationToken token)
    {
        using (var enumerator = m_partitioner.GetPartitions(1)[0])
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }

    internal static OrdinalIndexState GetOrdinalIndexState(Partitioner<TElement> partitioner)
    {
        if (!(partitioner is OrderablePartitioner<TElement> orderablePartitioner) ||
            !orderablePartitioner.KeysOrderedInEachPartition)
        {
            return OrdinalIndexState.Shuffled;
        }

        return orderablePartitioner.KeysNormalized ? OrdinalIndexState.Correct : OrdinalIndexState.Increasing;
    }

    internal override QueryResults<TElement> Open(QuerySettings settings, bool preferStriping)
    {
        return new PartitionerQueryOperatorResults(m_partitioner, settings);
    }

    private class PartitionerQueryOperatorResults : QueryResults<TElement>
    {
        private readonly Partitioner<TElement> m_partitioner;
        private QuerySettings m_settings;

        internal PartitionerQueryOperatorResults(
            Partitioner<TElement> partitioner,
            QuerySettings settings)
        {
            m_partitioner = partitioner;
            m_settings = settings;
        }

        internal override void GivePartitionedStream(IPartitionedStreamRecipient<TElement> recipient)
        {
            var value = m_settings.DegreeOfParallelism.Value;
            var mPartitioner = m_partitioner as OrderablePartitioner<TElement>;
            var partitionedStream = new PartitionedStream<TElement, int>(value, Util.GetDefaultComparer<int>(),
                mPartitioner != null ? GetOrdinalIndexState(mPartitioner) : OrdinalIndexState.Shuffled);
            if (mPartitioner == null)
            {
                var partitions = m_partitioner.GetPartitions(value);
                if (partitions == null)
                {
                    throw new InvalidOperationException(LinqBridge.PartitionerQueryOperator_NullPartitionList);
                }

                if (partitions.Count != value)
                {
                    throw new InvalidOperationException(LinqBridge.PartitionerQueryOperator_WrongNumberOfPartitions);
                }

                for (var i = 0; i < value; i++)
                {
                    var item = partitions[i];
                    if (item == null)
                    {
                        throw new InvalidOperationException(LinqBridge.PartitionerQueryOperator_NullPartition);
                    }

                    partitionedStream[i] = new PartitionerEnumerator(item);
                }
            }
            else
            {
                var orderablePartitions = mPartitioner.GetOrderablePartitions(value);
                if (orderablePartitions == null)
                {
                    throw new InvalidOperationException(LinqBridge.PartitionerQueryOperator_NullPartitionList);
                }

                if (orderablePartitions.Count != value)
                {
                    throw new InvalidOperationException(LinqBridge.PartitionerQueryOperator_WrongNumberOfPartitions);
                }

                for (var j = 0; j < value; j++)
                {
                    var enumerator = orderablePartitions[j];
                    if (enumerator == null)
                    {
                        throw new InvalidOperationException(LinqBridge.PartitionerQueryOperator_NullPartition);
                    }

                    partitionedStream[j] = new OrderablePartitionerEnumerator(enumerator);
                }
            }

            recipient.Receive(partitionedStream);
        }
    }

    private class OrderablePartitionerEnumerator : QueryOperatorEnumerator<TElement, int>
    {
        private readonly IEnumerator<KeyValuePair<long, TElement>> m_sourceEnumerator;

        internal OrderablePartitionerEnumerator(
            IEnumerator<KeyValuePair<long, TElement>> sourceEnumerator)
        {
            m_sourceEnumerator = sourceEnumerator;
        }

        protected override void Dispose(bool disposing)
        {
            m_sourceEnumerator.Dispose();
        }

        internal override bool MoveNext(ref TElement currentElement, ref int currentKey)
        {
            if (!m_sourceEnumerator.MoveNext())
            {
                return false;
            }

            var current = m_sourceEnumerator.Current;
            currentElement = current.Value;
            currentKey = checked((int)current.Key);
            return true;
        }
    }

    private class PartitionerEnumerator : QueryOperatorEnumerator<TElement, int>
    {
        private readonly IEnumerator<TElement> m_sourceEnumerator;

        internal PartitionerEnumerator(IEnumerator<TElement> sourceEnumerator)
        {
            m_sourceEnumerator = sourceEnumerator;
        }

        protected override void Dispose(bool disposing)
        {
            m_sourceEnumerator.Dispose();
        }

        internal override bool MoveNext(ref TElement currentElement, ref int currentKey)
        {
            if (!m_sourceEnumerator.MoveNext())
            {
                return false;
            }

            currentElement = m_sourceEnumerator.Current;
            currentKey = 0;
            return true;
        }
    }
}