#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal class SortHelper<TInputOutput, TKey> : SortHelper<TInputOutput>, IDisposable
{
    private readonly QueryTaskGroupState m_groupState;
    private readonly OrdinalIndexState m_indexState;
    private readonly IComparer<TKey> m_keyComparer;
    private readonly int m_partitionCount;
    private readonly int m_partitionIndex;
    private readonly Barrier[,] m_sharedBarriers;
    private readonly int[][] m_sharedIndices;
    private readonly GrowingArray<TKey>[] m_sharedKeys;
    private readonly TInputOutput[][] m_sharedValues;
    private readonly QueryOperatorEnumerator<TInputOutput, TKey> m_source;

    private SortHelper(
        QueryOperatorEnumerator<TInputOutput, TKey> source,
        int partitionCount,
        int partitionIndex,
        QueryTaskGroupState groupState,
        int[][] sharedIndices,
        OrdinalIndexState indexState,
        IComparer<TKey> keyComparer,
        GrowingArray<TKey>[] sharedkeys,
        TInputOutput[][] sharedValues,
        Barrier[,] sharedBarriers)
    {
        m_source = source;
        m_partitionCount = partitionCount;
        m_partitionIndex = partitionIndex;
        m_groupState = groupState;
        m_sharedIndices = sharedIndices;
        m_indexState = indexState;
        m_keyComparer = keyComparer;
        m_sharedKeys = sharedkeys;
        m_sharedValues = sharedValues;
        m_sharedBarriers = sharedBarriers;
    }

    public void Dispose()
    {
        if (m_partitionIndex != 0)
        {
            return;
        }

        for (var index1 = 0; index1 < m_sharedBarriers.GetLength(0); ++index1)
        {
            for (var index2 = 0; index2 < m_sharedBarriers.GetLength(1); ++index2)
            {
                m_sharedBarriers[index1, index2]?.Dispose();
            }
        }
    }

    internal static SortHelper<TInputOutput, TKey>[] GenerateSortHelpers(
        PartitionedStream<TInputOutput, TKey> partitions,
        QueryTaskGroupState groupState)
    {
        var partitionCount = partitions.PartitionCount;
        var sortHelpers = new SortHelper<TInputOutput, TKey>[partitionCount];
        var num1 = 1;
        var length = 0;
        for (; num1 < partitionCount; num1 <<= 1)
        {
            ++length;
        }

        var sharedIndices = new int[partitionCount][];
        var sharedkeys = new GrowingArray<TKey>[partitionCount];
        var sharedValues = new TInputOutput[partitionCount][];
        var sharedBarriers = new Barrier[length, partitionCount];
        if (partitionCount > 1)
        {
            var num2 = 1;
            for (var index1 = 0; index1 < sharedBarriers.GetLength(0); ++index1)
            {
                for (var index2 = 0; index2 < sharedBarriers.GetLength(1); ++index2)
                {
                    if (index2 % num2 == 0)
                    {
                        sharedBarriers[index1, index2] = new Barrier(2);
                    }
                }

                num2 *= 2;
            }
        }

        for (var index = 0; index < partitionCount; ++index)
        {
            sortHelpers[index] = new SortHelper<TInputOutput, TKey>(partitions[index], partitionCount, index,
                groupState, sharedIndices, partitions.OrdinalIndexState, partitions.KeyComparer, sharedkeys,
                sharedValues, sharedBarriers);
        }

        return sortHelpers;
    }

    internal override TInputOutput[] Sort()
    {
        var keys = (GrowingArray<TKey>)null;
        var values = (List<TInputOutput>)null;
        BuildKeysFromSource(ref keys, ref values);
        QuickSortIndicesInPlace(keys, values, m_indexState);
        if (m_partitionCount > 1)
        {
            MergeSortCooperatively();
        }

        return m_sharedValues[m_partitionIndex];
    }

    private void BuildKeysFromSource(ref GrowingArray<TKey> keys, ref List<TInputOutput> values)
    {
        values = new List<TInputOutput>();
        var cancellationToken = m_groupState.CancellationState.MergedCancellationToken;
        try
        {
            var currentElement = default(TInputOutput);
            var currentKey = default(TKey);
            var flag = m_source.MoveNext(ref currentElement, ref currentKey);
            if (keys == null)
            {
                keys = new GrowingArray<TKey>();
            }

            if (!flag)
            {
                return;
            }

            var num = 0;
            do
            {
                if ((num++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(cancellationToken);
                }

                keys.Add(currentKey);
                values.Add(currentElement);
            } while (m_source.MoveNext(ref currentElement, ref currentKey));
        }
        finally
        {
            m_source.Dispose();
        }
    }

    private int ComputePartnerIndex(int phase)
    {
        var num = 1 << phase;
        return m_partitionIndex + (m_partitionIndex % (num * 2) == 0 ? num : -num);
    }

    private void MergeSortCooperatively()
    {
        var cancellationToken = m_groupState.CancellationState.MergedCancellationToken;
        var length1 = m_sharedBarriers.GetLength(0);
        for (var phase = 0; phase < length1; ++phase)
        {
            var flag = phase == length1 - 1;
            var partnerIndex = ComputePartnerIndex(phase);
            if (partnerIndex < m_partitionCount)
            {
                var sharedIndex1 = m_sharedIndices[m_partitionIndex];
                var sharedKey1 = m_sharedKeys[m_partitionIndex];
                var internalArray1 = sharedKey1.InternalArray;
                var sharedValue1 = m_sharedValues[m_partitionIndex];
                m_sharedBarriers[phase, Math.Min(m_partitionIndex, partnerIndex)].SignalAndWait(cancellationToken);
                if (m_partitionIndex < partnerIndex)
                {
                    var sharedIndex2 = m_sharedIndices[partnerIndex];
                    var internalArray2 = m_sharedKeys[partnerIndex].InternalArray;
                    var sharedValue2 = m_sharedValues[partnerIndex];
                    m_sharedIndices[partnerIndex] = sharedIndex1;
                    m_sharedKeys[partnerIndex] = sharedKey1;
                    m_sharedValues[partnerIndex] = sharedValue1;
                    var length2 = sharedValue1.Length;
                    var length3 = sharedValue2.Length;
                    var length4 = length2 + length3;
                    var numArray = (int[])null;
                    var destinationArray = new TInputOutput[length4];
                    if (!flag)
                    {
                        numArray = new int[length4];
                    }

                    m_sharedIndices[m_partitionIndex] = numArray;
                    m_sharedKeys[m_partitionIndex] = sharedKey1;
                    m_sharedValues[m_partitionIndex] = destinationArray;
                    m_sharedBarriers[phase, m_partitionIndex].SignalAndWait(cancellationToken);
                    var num = (length4 + 1) / 2;
                    var index1 = 0;
                    var index2 = 0;
                    var index3 = 0;
                    for (; index1 < num; ++index1)
                    {
                        if ((index1 & 63 /*0x3F*/) == 0)
                        {
                            CancellationState.ThrowIfCanceled(cancellationToken);
                        }

                        if (index2 < length2 && (index3 >= length3 ||
                                                 m_keyComparer.Compare(internalArray1[sharedIndex1[index2]],
                                                     internalArray2[sharedIndex2[index3]]) <= 0))
                        {
                            if (flag)
                            {
                                destinationArray[index1] = sharedValue1[sharedIndex1[index2]];
                            }
                            else
                            {
                                numArray[index1] = sharedIndex1[index2];
                            }

                            ++index2;
                        }
                        else
                        {
                            if (flag)
                            {
                                destinationArray[index1] = sharedValue2[sharedIndex2[index3]];
                            }
                            else
                            {
                                numArray[index1] = length2 + sharedIndex2[index3];
                            }

                            ++index3;
                        }
                    }

                    if (!flag && length2 > 0)
                    {
                        Array.Copy(sharedValue1, 0, destinationArray, 0, length2);
                    }

                    m_sharedBarriers[phase, m_partitionIndex].SignalAndWait(cancellationToken);
                }
                else
                {
                    m_sharedBarriers[phase, partnerIndex].SignalAndWait(cancellationToken);
                    var sharedIndex3 = m_sharedIndices[m_partitionIndex];
                    var internalArray3 = m_sharedKeys[m_partitionIndex].InternalArray;
                    var sharedValue3 = m_sharedValues[m_partitionIndex];
                    var sharedIndex4 = m_sharedIndices[partnerIndex];
                    var sharedKey2 = m_sharedKeys[partnerIndex];
                    var sharedValue4 = m_sharedValues[partnerIndex];
                    var length5 = sharedValue3.Length;
                    var length6 = sharedValue1.Length;
                    var num1 = length5 + length6;
                    var num2 = (num1 + 1) / 2;
                    var index4 = num1 - 1;
                    var index5 = length5 - 1;
                    var index6 = length6 - 1;
                    for (; index4 >= num2; --index4)
                    {
                        if ((index4 & 63 /*0x3F*/) == 0)
                        {
                            CancellationState.ThrowIfCanceled(cancellationToken);
                        }

                        if (index5 >= 0 && (index6 < 0 || m_keyComparer.Compare(internalArray3[sharedIndex3[index5]],
                                internalArray1[sharedIndex1[index6]]) > 0))
                        {
                            if (flag)
                            {
                                sharedValue4[index4] = sharedValue3[sharedIndex3[index5]];
                            }
                            else
                            {
                                sharedIndex4[index4] = sharedIndex3[index5];
                            }

                            --index5;
                        }
                        else
                        {
                            if (flag)
                            {
                                sharedValue4[index4] = sharedValue1[sharedIndex1[index6]];
                            }
                            else
                            {
                                sharedIndex4[index4] = length5 + sharedIndex1[index6];
                            }

                            --index6;
                        }
                    }

                    if (!flag && sharedValue1.Length != 0)
                    {
                        sharedKey2.CopyFrom(internalArray1, sharedValue1.Length);
                        Array.Copy(sharedValue1, 0, sharedValue4, length5, sharedValue1.Length);
                    }

                    m_sharedBarriers[phase, partnerIndex].SignalAndWait(cancellationToken);
                    break;
                }
            }
        }
    }

    private void QuickSort(
        int left,
        int right,
        TKey[] keys,
        int[] indices,
        CancellationToken cancelToken)
    {
        if (right - left > 63 /*0x3F*/)
        {
            CancellationState.ThrowIfCanceled(cancelToken);
        }

        do
        {
            var left1 = left;
            var right1 = right;
            var index1 = indices[left1 + ((right1 - left1) >> 1)];
            var key = keys[index1];
            do
            {
                while (m_keyComparer.Compare(keys[indices[left1]], key) < 0)
                {
                    ++left1;
                }

                while (m_keyComparer.Compare(keys[indices[right1]], key) > 0)
                {
                    --right1;
                }

                if (left1 <= right1)
                {
                    if (left1 < right1)
                    {
                        var index2 = indices[left1];
                        indices[left1] = indices[right1];
                        indices[right1] = index2;
                    }

                    ++left1;
                    --right1;
                }
                else
                {
                    break;
                }
            } while (left1 <= right1);

            if (right1 - left <= right - left1)
            {
                if (left < right1)
                {
                    QuickSort(left, right1, keys, indices, cancelToken);
                }

                left = left1;
            }
            else
            {
                if (left1 < right)
                {
                    QuickSort(left1, right, keys, indices, cancelToken);
                }

                right = right1;
            }
        } while (left < right);
    }

    private void QuickSortIndicesInPlace(
        GrowingArray<TKey> keys,
        List<TInputOutput> values,
        OrdinalIndexState ordinalIndexState)
    {
        var indices = new int[values.Count];
        for (var index = 0; index < indices.Length; ++index)
        {
            indices[index] = index;
        }

        if (indices.Length > 1 && ordinalIndexState.IsWorseThan(OrdinalIndexState.Increasing))
        {
            QuickSort(0, indices.Length - 1, keys.InternalArray, indices,
                m_groupState.CancellationState.MergedCancellationToken);
        }

        if (m_partitionCount == 1)
        {
            var inputOutputArray = new TInputOutput[values.Count];
            for (var index = 0; index < indices.Length; ++index)
            {
                inputOutputArray[index] = values[indices[index]];
            }

            m_sharedValues[m_partitionIndex] = inputOutputArray;
        }
        else
        {
            m_sharedIndices[m_partitionIndex] = indices;
            m_sharedKeys[m_partitionIndex] = keys;
            m_sharedValues[m_partitionIndex] = new TInputOutput[values.Count];
            values.CopyTo(m_sharedValues[m_partitionIndex]);
        }
    }
}