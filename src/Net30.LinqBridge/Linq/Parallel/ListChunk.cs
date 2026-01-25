#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal class ListChunk<TInputOutput> : IEnumerable<TInputOutput>, IEnumerable
{
    internal TInputOutput[] m_chunk;
    private ListChunk<TInputOutput> m_tailChunk;

    internal ListChunk(int size)
    {
        m_chunk = new TInputOutput[size];
        Count = 0;
        m_tailChunk = this;
    }

    internal ListChunk<TInputOutput> Next { get; private set; }

    internal int Count { get; private set; }

    public IEnumerator<TInputOutput> GetEnumerator()
    {
        for (var curr = this; curr != null; curr = curr.Next)
        {
            for (var i = 0; i < curr.Count; ++i)
            {
                yield return curr.m_chunk[i];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal void Add(TInputOutput e)
    {
        var listChunk = m_tailChunk;
        if (listChunk.Count == listChunk.m_chunk.Length)
        {
            m_tailChunk = new ListChunk<TInputOutput>(listChunk.Count * 2);
            listChunk = listChunk.Next = m_tailChunk;
        }

        listChunk.m_chunk[listChunk.Count++] = e;
    }
}