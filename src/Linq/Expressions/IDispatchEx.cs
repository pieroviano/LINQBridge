// Type: System.Dynamic.IDispatchEx
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

[InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
[Guid("A6EF9860-C720-11D0-9337-00A0C90DCAA9")]
[ComImport]
internal interface IDispatchEx
{
  [MethodImpl(MethodImplOptions.PreserveSig)]
  int DeleteMemberByDispID(int id);

  [MethodImpl(MethodImplOptions.PreserveSig)]
  int DeleteMemberByName([MarshalAs(UnmanagedType.BStr)] string bstrName, uint grfdex);

  [MethodImpl(MethodImplOptions.PreserveSig)]
  int GetDispID([MarshalAs(UnmanagedType.BStr)] string bstrName, uint grfdex, out int pid);

  [MethodImpl(MethodImplOptions.PreserveSig)]
  int GetMemberName(int id, [MarshalAs(UnmanagedType.BStr)] out string pbstrName);

  [MethodImpl(MethodImplOptions.PreserveSig)]
  void GetMemberProperties(int id, uint grfdexFetch, out uint pgrfdex);

  [MethodImpl(MethodImplOptions.PreserveSig)]
  void GetNameSpaceParent([MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

  [MethodImpl(MethodImplOptions.PreserveSig)]
  void GetNextDispID(uint grfdex, int id, out int pid);

  [MethodImpl(MethodImplOptions.PreserveSig)]
  void InvokeEx(
    int id,
    uint lcid,
    uint wFlags,
    ref System.Runtime.InteropServices.ComTypes.DISPPARAMS pdp,
    out object pVarRes,
    out System.Runtime.InteropServices.ComTypes.EXCEPINFO pei,
    IServiceProvider pspCaller);
}
