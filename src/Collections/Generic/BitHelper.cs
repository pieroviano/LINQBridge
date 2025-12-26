using System;
using System.Security;

namespace System.Collections.Generic;

internal class BitHelper
{
    private const byte MarkedBitFlag = 1;

    private const byte IntSize = 32;

    private readonly int _length;

    private readonly unsafe int* _arrayPtr;

    private readonly int[]? _array;

    private readonly bool useStackAlloc;

    [SecurityCritical]
    internal unsafe BitHelper(int* bitArrayPtr, int length)
    {
        _arrayPtr = bitArrayPtr;
        _length = length;
        useStackAlloc = true;
    }

    internal BitHelper(int[] bitArray, int length)
    {
        _array = bitArray;
        _length = length;
    }

    [SecurityCritical]
    internal bool IsMarked(int bitPosition)
    {
        unsafe
        {
            if (!useStackAlloc)
            {
                var num = bitPosition / 32;
                if (num >= _length || num < 0)
                {
                    return false;
                }

                var array = _array;
                return array != null && (array[num] & 1 << (bitPosition % 32 & 31)) != 0;
            }
            var num1 = bitPosition / 32;
            if (num1 >= _length || num1 < 0)
            {
                return false;
            }
            return (*(_arrayPtr + num1 * 4) & 1 << (bitPosition % 32 & 31)) != 0;
        }
    }

    [SecurityCritical]
    internal void MarkBit(int bitPosition)
    {
        unsafe
        {
            if (!useStackAlloc)
            {
                var num = bitPosition / 32;
                if (num < _length && num >= 0)
                {
                    var array = _array;
                    if (array != null)
                    {
                        ref var mArray = ref array[num];
                        mArray = mArray | 1 << (bitPosition % 32 & 31);
                    }
                }
            }
            else
            {
                var num1 = bitPosition / 32;
                if (num1 < _length && num1 >= 0)
                {
                    var mArrayPtr = _arrayPtr + num1 * 4;
                    *mArrayPtr = *mArrayPtr | 1 << (bitPosition % 32 & 31);
                }
            }
        }
    }

    internal static int ToIntArrayLength(int n)
    {
        if (n <= 0)
        {
            return 0;
        }
        return (n - 1) / 32 + 1;
    }
}