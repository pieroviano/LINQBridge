#nullable disable
using System.Runtime.InteropServices;

namespace System.Linq.Parallel;

internal static class Scheduling
{
    internal const bool DefaultPreserveOrder = false;
    internal const int DEFAULT_BOUNDED_BUFFER_CAPACITY = 512 /*0x0200*/;
    internal const int DEFAULT_BYTES_PER_CHUNK = 512 /*0x0200*/;
    internal const int ZOMBIED_PRODUCER_TIMEOUT = -1;
    internal const int MAX_SUPPORTED_DOP = 512 /*0x0200*/;
    internal static int DefaultDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 512 /*0x0200*/);

    internal static int GetDefaultChunkSize<T>()
    {
        return !typeof(T).IsValueType ? 512 /*0x0200*/ / IntPtr.Size :
            typeof(T).StructLayoutAttribute.Value != LayoutKind.Explicit ? 128 /*0x80*/ :
            Math.Max(1, 512 /*0x0200*/ / Marshal.SizeOf(typeof(T)));
    }

    internal static int GetDefaultDegreeOfParallelism()
    {
        return DefaultDegreeOfParallelism;
    }
}