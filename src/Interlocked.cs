using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace System
{
    public static class Interlocked
    {
        static object lockObj = new object();

        [__DynamicallyInvokable]
        [ComVisible(false)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SecuritySafeCritical]
        public static T CompareExchange<T>(ref T location1, T value, T comparand)
        {
            lock (lockObj)
            {
                if (location1.Equals(comparand))
                {
                    location1 = value;
                }
                return value;
            }
        }

    }
}
