#nullable disable
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal abstract class HashRepartitionStream<TInputOutput, THashKey, TOrderKey> :
    PartitionedStream<Pair<TInputOutput, THashKey>, TOrderKey>
{
    private const int NULL_ELEMENT_HASH_CODE = 0;
    private readonly int m_distributionMod;
    private readonly IEqualityComparer<TInputOutput> m_elementComparer;
    private readonly IEqualityComparer<THashKey> m_keyComparer;

    internal HashRepartitionStream(
        int partitionsCount,
        IComparer<TOrderKey> orderKeyComparer,
        IEqualityComparer<THashKey> hashKeyComparer,
        IEqualityComparer<TInputOutput> elementComparer)
        : base(partitionsCount, orderKeyComparer, OrdinalIndexState.Shuffled)
    {
        m_keyComparer = hashKeyComparer;
        m_elementComparer = elementComparer;
        m_distributionMod = 503;
        while (m_distributionMod < partitionsCount)
        {
            checked
            {
                m_distributionMod *= 2;
            }
        }
    }

    internal int GetHashCode(TInputOutput element)
    {
        return (int.MaxValue & (m_elementComparer == null
            ? element == null ? 0 : element.GetHashCode()
            : m_elementComparer.GetHashCode(element))) % m_distributionMod;
    }

    internal int GetHashCode(THashKey key)
    {
        return (int.MaxValue &
                (m_keyComparer == null ? key == null ? 0 : key.GetHashCode() : m_keyComparer.GetHashCode(key))) %
               m_distributionMod;
    }
}