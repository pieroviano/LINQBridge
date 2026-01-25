// Type: System.Dynamic.IDispatch
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("00020400-0000-0000-C000-000000000046")]
[ComImport]
internal interface IDispatch
{
  [MethodImpl(MethodImplOptions.PreserveSig)]
  int TryGetTypeInfoCount(out uint pctinfo);

  [MethodImpl(MethodImplOptions.PreserveSig)]
  int TryGetTypeInfo(uint iTInfo, int lcid, out IntPtr info);

  [MethodImpl(MethodImplOptions.PreserveSig)]
  int TryGetIDsOfNames(ref Guid iid, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2, ArraySubType = UnmanagedType.LPWStr)] string[] names, uint cNames, int lcid, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2, ArraySubType = UnmanagedType.I4), Out] int[] rgDispId);

  [MethodImpl(MethodImplOptions.PreserveSig)]
  int TryInvoke(
    int dispIdMember,
    ref Guid riid,
    int lcid,
    System.Runtime.InteropServices.ComTypes.INVOKEKIND wFlags,
    ref System.Runtime.InteropServices.ComTypes.DISPPARAMS pDispParams,
    out object VarResult,
    out System.Runtime.InteropServices.ComTypes.EXCEPINFO pExcepInfo,
    out uint puArgErr);
}
