namespace System.Threading;

internal class SparselyPopulatedArray<T>
    where T : class
{
    private readonly SparselyPopulatedArrayFragment<T> m_head;

    private volatile SparselyPopulatedArrayFragment<T> m_tail;

    internal SparselyPopulatedArray(int initialSize)
    {
        var sparselyPopulatedArrayFragment = new SparselyPopulatedArrayFragment<T>(initialSize);
        var sparselyPopulatedArrayFragment1 = sparselyPopulatedArrayFragment;
        m_tail = sparselyPopulatedArrayFragment;
        m_head = sparselyPopulatedArrayFragment1;
    }

    internal SparselyPopulatedArrayFragment<T> Tail => m_tail;

    internal SparselyPopulatedArrayAddInfo<T> Add(T element)
    {
        int freeCount;
        while (true)
        {
            var mTail = m_tail;
            while (mTail.m_next != null)
            {
                var mNext = mTail.m_next;
                mTail = mNext;
                m_tail = mNext;
            }

            for (var i = mTail; i != null; i = i.m_prev)
            {
                if (i.m_freeCount < 1)
                {
                    i.m_freeCount--;
                }

                if (i.m_freeCount > 0 || i.m_freeCount < -10)
                {
                    var length = i.Length;
                    var mFreeCount = (length - i.m_freeCount) % length;
                    if (mFreeCount < 0)
                    {
                        mFreeCount = 0;
                        i.m_freeCount--;
                    }

                    for (var j = 0; j < length; j++)
                    {
                        var num = (mFreeCount + j) % length;
                        if (i.m_elements[num] == null)
                        {
                            var t = default(T);
                            if (Interlocked.CompareExchange<T>(ref i.m_elements[num], element, t) == null)
                            {
                                var mFreeCount1 = i.m_freeCount - 1;
                                var sparselyPopulatedArrayFragment = i;
                                if (mFreeCount1 > 0)
                                {
                                    freeCount = mFreeCount1;
                                }
                                else
                                {
                                    freeCount = 0;
                                }

                                sparselyPopulatedArrayFragment.m_freeCount = freeCount;
                                return new SparselyPopulatedArrayAddInfo<T>(i, num);
                            }
                        }
                    }
                }
            }

            var sparselyPopulatedArrayFragment1 =
                new SparselyPopulatedArrayFragment<T>(
                    mTail.m_elements.Length == 4096 ? 4096 : mTail.m_elements.Length * 2, mTail);
            if (Interlocked.CompareExchange<SparselyPopulatedArrayFragment<T>>(ref mTail.m_next,
                    sparselyPopulatedArrayFragment1, null) == null)
            {
                m_tail = sparselyPopulatedArrayFragment1;
            }
        }
    }
}