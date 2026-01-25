#nullable disable
using System.Threading;

namespace System.Linq.Parallel;

internal class OrderedHashRepartitionEnumerator<TInputOutput, THashKey, TOrderKey> :
    QueryOperatorEnumerator<Pair<TInputOutput, THashKey>, TOrderKey>
{
    private const int ENUMERATION_NOT_STARTED = -1;
    private readonly CancellationToken m_cancellationToken;
    private readonly ListChunk<TOrderKey>[,] m_keyExchangeMatrix;
    private readonly Func<TInputOutput, THashKey> m_keySelector;
    private readonly int m_partitionCount;
    private readonly int m_partitionIndex;
    private readonly HashRepartitionStream<TInputOutput, THashKey, TOrderKey> m_repartitionStream;
    private readonly QueryOperatorEnumerator<TInputOutput, TOrderKey> m_source;
    private readonly ListChunk<Pair<TInputOutput, THashKey>>[,] m_valueExchangeMatrix;
    private CountdownEvent m_barrier;
    private Mutables m_mutables;

    internal OrderedHashRepartitionEnumerator(
        QueryOperatorEnumerator<TInputOutput, TOrderKey> source,
        int partitionCount,
        int partitionIndex,
        Func<TInputOutput, THashKey> keySelector,
        OrderedHashRepartitionStream<TInputOutput, THashKey, TOrderKey> repartitionStream,
        CountdownEvent barrier,
        ListChunk<Pair<TInputOutput, THashKey>>[,] valueExchangeMatrix,
        ListChunk<TOrderKey>[,] keyExchangeMatrix,
        CancellationToken cancellationToken)
    {
        m_source = source;
        m_partitionCount = partitionCount;
        m_partitionIndex = partitionIndex;
        m_keySelector = keySelector;
        m_repartitionStream = repartitionStream;
        m_barrier = barrier;
        m_valueExchangeMatrix = valueExchangeMatrix;
        m_keyExchangeMatrix = keyExchangeMatrix;
        m_cancellationToken = cancellationToken;
    }

    protected override void Dispose(bool disposing)
    {
        if (m_barrier == null)
        {
            return;
        }

        if (m_mutables == null || m_mutables.m_currentBufferIndex == -1)
        {
            m_barrier.Signal();
            m_barrier = null;
        }

        m_source.Dispose();
    }

    internal override bool MoveNext(
        ref Pair<TInputOutput, THashKey> currentElement,
        ref TOrderKey currentKey)
    {
        if (m_partitionCount == 1)
        {
            var currentElement1 = default(TInputOutput);
            if (!m_source.MoveNext(ref currentElement1, ref currentKey))
            {
                return false;
            }

            currentElement = new Pair<TInputOutput, THashKey>(currentElement1,
                m_keySelector == null ? default : m_keySelector(currentElement1));
            return true;
        }

        var mutables = m_mutables ?? (m_mutables = new Mutables());
        if (mutables.m_currentBufferIndex == -1)
        {
            EnumerateAndRedistributeElements();
        }

        while (mutables.m_currentBufferIndex < m_partitionCount)
        {
            if (mutables.m_currentBuffer != null)
            {
                if (++mutables.m_currentIndex < mutables.m_currentBuffer.Count)
                {
                    currentElement = mutables.m_currentBuffer.m_chunk[mutables.m_currentIndex];
                    currentKey = mutables.m_currentKeyBuffer.m_chunk[mutables.m_currentIndex];
                    return true;
                }

                mutables.m_currentIndex = -1;
                mutables.m_currentBuffer = mutables.m_currentBuffer.Next;
                mutables.m_currentKeyBuffer = mutables.m_currentKeyBuffer.Next;
            }
            else
            {
                if (mutables.m_currentBufferIndex == m_partitionIndex)
                {
                    m_barrier.Wait(m_cancellationToken);
                    mutables.m_currentBufferIndex = -1;
                }

                ++mutables.m_currentBufferIndex;
                mutables.m_currentIndex = -1;
                if (mutables.m_currentBufferIndex == m_partitionIndex)
                {
                    ++mutables.m_currentBufferIndex;
                }

                if (mutables.m_currentBufferIndex < m_partitionCount)
                {
                    mutables.m_currentBuffer = m_valueExchangeMatrix[mutables.m_currentBufferIndex, m_partitionIndex];
                    mutables.m_currentKeyBuffer = m_keyExchangeMatrix[mutables.m_currentBufferIndex, m_partitionIndex];
                }
            }
        }

        return false;
    }

    private void EnumerateAndRedistributeElements()
    {
        var mutables = m_mutables;
        var listChunkArray1 = new ListChunk<Pair<TInputOutput, THashKey>>[m_partitionCount];
        var listChunkArray2 = new ListChunk<TOrderKey>[m_partitionCount];
        var currentElement = default(TInputOutput);
        var currentKey = default(TOrderKey);
        var num = 0;
        while (m_source.MoveNext(ref currentElement, ref currentKey))
        {
            if ((num++ & 63 /*0x3F*/) == 0)
            {
                CancellationState.ThrowIfCanceled(m_cancellationToken);
            }

            var hashKey = default(THashKey);
            int index;
            if (m_keySelector != null)
            {
                hashKey = m_keySelector(currentElement);
                index = m_repartitionStream.GetHashCode(hashKey) % m_partitionCount;
            }
            else
            {
                index = m_repartitionStream.GetHashCode(currentElement) % m_partitionCount;
            }

            var listChunk1 = listChunkArray1[index];
            var listChunk2 = listChunkArray2[index];
            if (listChunk1 == null)
            {
                listChunkArray1[index] = listChunk1 = new ListChunk<Pair<TInputOutput, THashKey>>(128 /*0x80*/);
                listChunkArray2[index] = listChunk2 = new ListChunk<TOrderKey>(128 /*0x80*/);
            }

            listChunk1.Add(new Pair<TInputOutput, THashKey>(currentElement, hashKey));
            listChunk2.Add(currentKey);
        }

        for (var index = 0; index < m_partitionCount; ++index)
        {
            m_valueExchangeMatrix[m_partitionIndex, index] = listChunkArray1[index];
            m_keyExchangeMatrix[m_partitionIndex, index] = listChunkArray2[index];
        }

        m_barrier.Signal();
        mutables.m_currentBufferIndex = m_partitionIndex;
        mutables.m_currentBuffer = listChunkArray1[m_partitionIndex];
        mutables.m_currentKeyBuffer = listChunkArray2[m_partitionIndex];
        mutables.m_currentIndex = -1;
    }

    private class Mutables
    {
        internal ListChunk<Pair<TInputOutput, THashKey>> m_currentBuffer;
        internal int m_currentBufferIndex;
        internal int m_currentIndex;
        internal ListChunk<TOrderKey> m_currentKeyBuffer;

        internal Mutables()
        {
            m_currentBufferIndex = -1;
        }
    }
}