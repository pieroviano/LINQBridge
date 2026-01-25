#nullable disable
namespace System.Linq.Parallel;

internal abstract class BinaryQueryOperator<TLeftInput, TRightInput, TOutput> :
    QueryOperator<TOutput>
{
    private OrdinalIndexState m_indexState = OrdinalIndexState.Shuffled;

    internal BinaryQueryOperator(
        ParallelQuery<TLeftInput> leftChild,
        ParallelQuery<TRightInput> rightChild)
        : this(QueryOperator<TLeftInput>.AsQueryOperator(leftChild),
            QueryOperator<TRightInput>.AsQueryOperator(rightChild))
    {
    }

    internal BinaryQueryOperator(
        QueryOperator<TLeftInput> leftChild,
        QueryOperator<TRightInput> rightChild)
        : base(false, leftChild.SpecifiedQuerySettings.Merge(rightChild.SpecifiedQuerySettings))
    {
        LeftChild = leftChild;
        RightChild = rightChild;
    }

    internal QueryOperator<TLeftInput> LeftChild { get; }

    internal QueryOperator<TRightInput> RightChild { get; }

    internal sealed override OrdinalIndexState OrdinalIndexState => m_indexState;

    public abstract void WrapPartitionedStream<TLeftKey, TRightKey>(
        PartitionedStream<TLeftInput, TLeftKey> leftPartitionedStream,
        PartitionedStream<TRightInput, TRightKey> rightPartitionedStream,
        IPartitionedStreamRecipient<TOutput> outputRecipient,
        bool preferStriping,
        QuerySettings settings);

    protected void SetOrdinalIndex(OrdinalIndexState indexState)
    {
        m_indexState = indexState;
    }

    internal class BinaryQueryOperatorResults : QueryResults<TOutput>
    {
        private readonly BinaryQueryOperator<TLeftInput, TRightInput, TOutput> m_op;
        private readonly bool m_preferStriping;
        protected QueryResults<TLeftInput> m_leftChildQueryResults;
        protected QueryResults<TRightInput> m_rightChildQueryResults;
        private QuerySettings m_settings;

        internal BinaryQueryOperatorResults(
            QueryResults<TLeftInput> leftChildQueryResults,
            QueryResults<TRightInput> rightChildQueryResults,
            BinaryQueryOperator<TLeftInput, TRightInput, TOutput> op,
            QuerySettings settings,
            bool preferStriping)
        {
            m_leftChildQueryResults = leftChildQueryResults;
            m_rightChildQueryResults = rightChildQueryResults;
            m_op = op;
            m_settings = settings;
            m_preferStriping = preferStriping;
        }

        internal override void GivePartitionedStream(IPartitionedStreamRecipient<TOutput> recipient)
        {
            if (m_settings.ExecutionMode.Value == ParallelExecutionMode.Default && m_op.LimitsParallelism)
            {
                var partitionedStream = ExchangeUtilities.PartitionDataSource(
                    m_op.AsSequentialQuery(m_settings.CancellationState.ExternalCancellationToken),
                    m_settings.DegreeOfParallelism.Value, m_preferStriping);
                recipient.Receive(partitionedStream);
            }
            else if (IsIndexible)
            {
                var partitionedStream =
                    ExchangeUtilities.PartitionDataSource(this, m_settings.DegreeOfParallelism.Value, m_preferStriping);
                recipient.Receive(partitionedStream);
            }
            else
            {
                m_leftChildQueryResults.GivePartitionedStream(
                    new LeftChildResultsRecipient(recipient, this, m_preferStriping, m_settings));
            }
        }

        private class LeftChildResultsRecipient : IPartitionedStreamRecipient<TLeftInput>
        {
            private readonly IPartitionedStreamRecipient<TOutput> m_outputRecipient;
            private readonly bool m_preferStriping;
            private readonly BinaryQueryOperatorResults m_results;
            private readonly QuerySettings m_settings;

            internal LeftChildResultsRecipient(
                IPartitionedStreamRecipient<TOutput> outputRecipient,
                BinaryQueryOperatorResults results,
                bool preferStriping,
                QuerySettings settings)
            {
                m_outputRecipient = outputRecipient;
                m_results = results;
                m_preferStriping = preferStriping;
                m_settings = settings;
            }

            public void Receive<TLeftKey>(PartitionedStream<TLeftInput, TLeftKey> source)
            {
                m_results.m_rightChildQueryResults.GivePartitionedStream(
                    new RightChildResultsRecipient<TLeftKey>(m_outputRecipient, m_results.m_op, source,
                        m_preferStriping, m_settings));
            }
        }

        private class RightChildResultsRecipient<TLeftKey> : IPartitionedStreamRecipient<TRightInput>
        {
            private readonly PartitionedStream<TLeftInput, TLeftKey> m_leftPartitionedStream;
            private readonly BinaryQueryOperator<TLeftInput, TRightInput, TOutput> m_op;
            private readonly IPartitionedStreamRecipient<TOutput> m_outputRecipient;
            private readonly bool m_preferStriping;
            private readonly QuerySettings m_settings;

            internal RightChildResultsRecipient(
                IPartitionedStreamRecipient<TOutput> outputRecipient,
                BinaryQueryOperator<TLeftInput, TRightInput, TOutput> op,
                PartitionedStream<TLeftInput, TLeftKey> leftPartitionedStream,
                bool preferStriping,
                QuerySettings settings)
            {
                m_outputRecipient = outputRecipient;
                m_op = op;
                m_preferStriping = preferStriping;
                m_leftPartitionedStream = leftPartitionedStream;
                m_settings = settings;
            }

            public void Receive<TRightKey>(
                PartitionedStream<TRightInput, TRightKey> rightPartitionedStream)
            {
                m_op.WrapPartitionedStream(m_leftPartitionedStream, rightPartitionedStream, m_outputRecipient,
                    m_preferStriping, m_settings);
            }
        }
    }
}