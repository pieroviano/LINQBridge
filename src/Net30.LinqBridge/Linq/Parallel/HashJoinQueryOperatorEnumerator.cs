#nullable disable
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal class HashJoinQueryOperatorEnumerator<TLeftInput, TLeftKey, TRightInput, THashKey, TOutput> :
    QueryOperatorEnumerator<TOutput, TLeftKey>
{
    private readonly CancellationToken m_cancellationToken;
    private readonly Func<TLeftInput, IEnumerable<TRightInput>, TOutput> m_groupResultSelector;
    private readonly IEqualityComparer<THashKey> m_keyComparer;
    private readonly QueryOperatorEnumerator<Pair<TLeftInput, THashKey>, TLeftKey> m_leftSource;
    private readonly QueryOperatorEnumerator<Pair<TRightInput, THashKey>, int> m_rightSource;
    private readonly Func<TLeftInput, TRightInput, TOutput> m_singleResultSelector;
    private Mutables m_mutables;

    internal HashJoinQueryOperatorEnumerator(
        QueryOperatorEnumerator<Pair<TLeftInput, THashKey>, TLeftKey> leftSource,
        QueryOperatorEnumerator<Pair<TRightInput, THashKey>, int> rightSource,
        Func<TLeftInput, TRightInput, TOutput> singleResultSelector,
        Func<TLeftInput, IEnumerable<TRightInput>, TOutput> groupResultSelector,
        IEqualityComparer<THashKey> keyComparer,
        CancellationToken cancellationToken)
    {
        m_leftSource = leftSource;
        m_rightSource = rightSource;
        m_singleResultSelector = singleResultSelector;
        m_groupResultSelector = groupResultSelector;
        m_keyComparer = keyComparer;
        m_cancellationToken = cancellationToken;
    }

    protected override void Dispose(bool disposing)
    {
        m_leftSource.Dispose();
        m_rightSource.Dispose();
    }

    internal override bool MoveNext(ref TOutput currentElement, ref TLeftKey currentKey)
    {
        var mutables = m_mutables;
        if (mutables == null)
        {
            mutables = m_mutables = new Mutables();
            mutables.m_rightHashLookup =
                new HashLookup<THashKey, Pair<TRightInput, ListChunk<TRightInput>>>(m_keyComparer);
            var currentElement1 = new Pair<TRightInput, THashKey>();
            var currentKey1 = 0;
            var num = 0;
            while (m_rightSource.MoveNext(ref currentElement1, ref currentKey1))
            {
                if ((num++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                var first = currentElement1.First;
                var second = currentElement1.Second;
                if (second != null)
                {
                    var pair = new Pair<TRightInput, ListChunk<TRightInput>>();
                    if (!mutables.m_rightHashLookup.TryGetValue(second, ref pair))
                    {
                        pair = new Pair<TRightInput, ListChunk<TRightInput>>(first, null);
                        if (m_groupResultSelector != null)
                        {
                            pair.Second = new ListChunk<TRightInput>(2);
                            pair.Second.Add(first);
                        }

                        mutables.m_rightHashLookup.Add(second, pair);
                    }
                    else
                    {
                        if (pair.Second == null)
                        {
                            pair.Second = new ListChunk<TRightInput>(2);
                            mutables.m_rightHashLookup[second] = pair;
                        }

                        pair.Second.Add(first);
                    }
                }
            }
        }

        var currentRightMatches = mutables.m_currentRightMatches;
        if (currentRightMatches != null && mutables.m_currentRightMatchesIndex == currentRightMatches.Count)
        {
            var listChunk = mutables.m_currentRightMatches = currentRightMatches.Next;
            mutables.m_currentRightMatchesIndex = 0;
        }

        if (mutables.m_currentRightMatches == null)
        {
            var currentElement2 = new Pair<TLeftInput, THashKey>();
            var currentKey2 = default(TLeftKey);
            while (m_leftSource.MoveNext(ref currentElement2, ref currentKey2))
            {
                if ((mutables.m_outputLoopCount++ & 63 /*0x3F*/) == 0)
                {
                    CancellationState.ThrowIfCanceled(m_cancellationToken);
                }

                var pair = new Pair<TRightInput, ListChunk<TRightInput>>();
                var first = currentElement2.First;
                var second = currentElement2.Second;
                if (second != null && mutables.m_rightHashLookup.TryGetValue(second, ref pair) &&
                    m_singleResultSelector != null)
                {
                    mutables.m_currentRightMatches = pair.Second;
                    mutables.m_currentRightMatchesIndex = 0;
                    currentElement = m_singleResultSelector(first, pair.First);
                    currentKey = currentKey2;
                    if (pair.Second != null)
                    {
                        mutables.m_currentLeft = first;
                        mutables.m_currentLeftKey = currentKey2;
                    }

                    return true;
                }

                if (m_groupResultSelector != null)
                {
                    var rightInputs = (IEnumerable<TRightInput>)pair.Second ?? ParallelEnumerable.Empty<TRightInput>();
                    currentElement = m_groupResultSelector(first, rightInputs);
                    currentKey = currentKey2;
                    return true;
                }
            }

            return false;
        }

        currentElement = m_singleResultSelector(mutables.m_currentLeft,
            mutables.m_currentRightMatches.m_chunk[mutables.m_currentRightMatchesIndex]);
        currentKey = mutables.m_currentLeftKey;
        ++mutables.m_currentRightMatchesIndex;
        return true;
    }

    private class Mutables
    {
        internal TLeftInput m_currentLeft;
        internal TLeftKey m_currentLeftKey;
        internal ListChunk<TRightInput> m_currentRightMatches;
        internal int m_currentRightMatchesIndex;
        internal int m_outputLoopCount;
        internal HashLookup<THashKey, Pair<TRightInput, ListChunk<TRightInput>>> m_rightHashLookup;
    }
}