#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal abstract class UnaryQueryOperator<TInput, TOutput> : QueryOperator<TOutput>
{
    private OrdinalIndexState m_indexState = OrdinalIndexState.Shuffled;

    internal UnaryQueryOperator(IEnumerable<TInput> child)
        : this(QueryOperator<TInput>.AsQueryOperator(child))
    {
    }

    internal UnaryQueryOperator(IEnumerable<TInput> child, bool outputOrdered)
        : this(QueryOperator<TInput>.AsQueryOperator(child), outputOrdered)
    {
    }

    private UnaryQueryOperator(QueryOperator<TInput> child)
        : this(child, child.OutputOrdered, child.SpecifiedQuerySettings)
    {
    }

    internal UnaryQueryOperator(QueryOperator<TInput> child, bool outputOrdered)
        : this(child, outputOrdered, child.SpecifiedQuerySettings)
    {
    }

    private UnaryQueryOperator(
        QueryOperator<TInput> child,
        bool outputOrdered,
        QuerySettings settings)
        : base(outputOrdered, settings)
    {
        Child = child;
    }

    internal QueryOperator<TInput> Child { get; }

    internal sealed override OrdinalIndexState OrdinalIndexState => m_indexState;

    protected void SetOrdinalIndexState(OrdinalIndexState indexState)
    {
        m_indexState = indexState;
    }

    internal abstract void WrapPartitionedStream<TKey>(
        PartitionedStream<TInput, TKey> inputStream,
        IPartitionedStreamRecipient<TOutput> recipient,
        bool preferStriping,
        QuerySettings settings);

    internal class UnaryQueryOperatorResults : QueryResults<TOutput>
    {
        private readonly UnaryQueryOperator<TInput, TOutput> m_op;
        private readonly bool m_preferStriping;
        protected QueryResults<TInput> m_childQueryResults;
        private QuerySettings m_settings;

        internal UnaryQueryOperatorResults(
            QueryResults<TInput> childQueryResults,
            UnaryQueryOperator<TInput, TOutput> op,
            QuerySettings settings,
            bool preferStriping)
        {
            m_childQueryResults = childQueryResults;
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
                m_childQueryResults.GivePartitionedStream(new ChildResultsRecipient(recipient, m_op, m_preferStriping,
                    m_settings));
            }
        }

        private class ChildResultsRecipient : IPartitionedStreamRecipient<TInput>
        {
            private readonly UnaryQueryOperator<TInput, TOutput> m_op;
            private readonly IPartitionedStreamRecipient<TOutput> m_outputRecipient;
            private readonly bool m_preferStriping;
            private readonly QuerySettings m_settings;

            internal ChildResultsRecipient(
                IPartitionedStreamRecipient<TOutput> outputRecipient,
                UnaryQueryOperator<TInput, TOutput> op,
                bool preferStriping,
                QuerySettings settings)
            {
                m_outputRecipient = outputRecipient;
                m_op = op;
                m_preferStriping = preferStriping;
                m_settings = settings;
            }

            public void Receive<TKey>(PartitionedStream<TInput, TKey> inputStream)
            {
                m_op.WrapPartitionedStream(inputStream, m_outputRecipient, m_preferStriping, m_settings);
            }
        }
    }
}