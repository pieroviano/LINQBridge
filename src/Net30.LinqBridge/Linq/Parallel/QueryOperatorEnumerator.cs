#nullable disable
using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal abstract class QueryOperatorEnumerator<TElement, TKey>
{
    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    internal IEnumerator<TElement> AsClassicEnumerator()
    {
        return new QueryOperatorClassicEnumerator(this);
    }

    internal abstract bool MoveNext(ref TElement currentElement, ref TKey currentKey);

    internal virtual void Reset()
    {
    }

    private class QueryOperatorClassicEnumerator : IEnumerator<TElement>, IDisposable, IEnumerator
    {
        private TElement m_current;
        private QueryOperatorEnumerator<TElement, TKey> m_operatorEnumerator;

        internal QueryOperatorClassicEnumerator(
            QueryOperatorEnumerator<TElement, TKey> operatorEnumerator)
        {
            m_operatorEnumerator = operatorEnumerator;
        }

        public bool MoveNext()
        {
            var currentKey = default(TKey);
            return m_operatorEnumerator.MoveNext(ref m_current, ref currentKey);
        }

        public TElement Current => m_current;

        object IEnumerator.Current => m_current;

        public void Dispose()
        {
            m_operatorEnumerator.Dispose();
            m_operatorEnumerator = null;
        }

        public void Reset()
        {
            m_operatorEnumerator.Reset();
        }
    }
}