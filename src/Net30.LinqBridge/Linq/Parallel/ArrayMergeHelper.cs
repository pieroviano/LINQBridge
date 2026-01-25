#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class ArrayMergeHelper<TInputOutput> : IMergeHelper<TInputOutput>
{
    private readonly TInputOutput[] m_outputArray;
    private readonly QueryResults<TInputOutput> m_queryResults;
    private readonly QuerySettings m_settings;

    public ArrayMergeHelper(QuerySettings settings, QueryResults<TInputOutput> queryResults)
    {
        m_settings = settings;
        m_queryResults = queryResults;
        m_outputArray = new TInputOutput[m_queryResults.Count];
    }

    public void Execute()
    {
        new QueryExecutionOption<int>(
                QueryOperator<int>.AsQueryOperator(ParallelEnumerable.Range(0, m_queryResults.Count)), m_settings)
            .ForAll(ToArrayElement);
    }

    public IEnumerator<TInputOutput> GetEnumerator()
    {
        return ((IEnumerable<TInputOutput>)GetResultsAsArray()).GetEnumerator();
    }

    public TInputOutput[] GetResultsAsArray()
    {
        return m_outputArray;
    }

    private void ToArrayElement(int index)
    {
        m_outputArray[index] = m_queryResults[index];
    }
}