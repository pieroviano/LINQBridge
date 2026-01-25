#pragma warning disable CS3019
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace System.Threading;

internal static class Volatile
{
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static bool Read(ref bool location)
    {
        var flag = location;
        Thread.MemoryBarrier();
        return flag;
    }

    [CLSCompliant(false)]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static sbyte Read(ref sbyte location)
    {
        var num = location;
        Thread.MemoryBarrier();
        return num;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static byte Read(ref byte location)
    {
        var num = location;
        Thread.MemoryBarrier();
        return num;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static short Read(ref short location)
    {
        var num = location;
        Thread.MemoryBarrier();
        return num;
    }

    [CLSCompliant(false)]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static ushort Read(ref ushort location)
    {
        var num = location;
        Thread.MemoryBarrier();
        return num;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static int Read(ref int location)
    {
        var num = location;
        Thread.MemoryBarrier();
        return num;
    }

    [CLSCompliant(false)]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static uint Read(ref uint location)
    {
        var num = location;
        Thread.MemoryBarrier();
        return num;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static long Read(ref long location)
    {
        return Interlocked.CompareExchange(ref location, 0, 0);
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static float Read(ref float location)
    {
        var single = location;
        Thread.MemoryBarrier();
        return single;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static double Read(ref double location)
    {
        return Interlocked.CompareExchange(ref location, 0, 0);
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [SecuritySafeCritical]
    public static T Read<T>(ref T location)
        where T : class
    {
        var t = location;
        Thread.MemoryBarrier();
        return t;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref bool location, bool value)
    {
        Thread.MemoryBarrier();
        location = value;
    }

    [CLSCompliant(false)]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref sbyte location, sbyte value)
    {
        Thread.MemoryBarrier();
        location = value;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref byte location, byte value)
    {
        Thread.MemoryBarrier();
        location = value;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref short location, short value)
    {
        Thread.MemoryBarrier();
        location = value;
    }

    [CLSCompliant(false)]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref ushort location, ushort value)
    {
        Thread.MemoryBarrier();
        location = value;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref int location, int value)
    {
        Thread.MemoryBarrier();
        location = value;
    }

    [CLSCompliant(false)]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref uint location, uint value)
    {
        Thread.MemoryBarrier();
        location = value;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref long location, long value)
    {
        Interlocked.Exchange(ref location, value);
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref IntPtr location, IntPtr value)
    {
        Thread.MemoryBarrier();
        location = value;
    }

    [CLSCompliant(false)]
    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref UIntPtr location, UIntPtr value)
    {
        Thread.MemoryBarrier();
        location = value;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref float location, float value)
    {
        Thread.MemoryBarrier();
        location = value;
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    public static void Write(ref double location, double value)
    {
        Interlocked.Exchange(ref location, value);
    }

    [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
    [SecuritySafeCritical]
    public static void Write<T>(ref T location, T value)
        where T : class
    {
        Thread.MemoryBarrier();
        location = value;
    }
}