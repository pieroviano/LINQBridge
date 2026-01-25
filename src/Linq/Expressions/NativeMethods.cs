// Type: System.Dynamic.NativeMethods
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

internal static class NativeMethods
{
  [DllImport("oleaut32.dll", PreserveSig = false)]
  internal static extern void VariantClear(IntPtr variant);
}
