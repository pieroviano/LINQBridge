// Type: System.Dynamic.ComRuntimeHelpers
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;

#nullable disable
namespace System.Linq.Expressions;

internal static class ComRuntimeHelpers
{
  [SecurityCritical]
    public static void CheckThrowException(
    int hresult,
    ref ExcepInfo excepInfo,
    uint argErr,
    string message)
  {
    if (ComHresults.IsSuccess(hresult))
      return;
    switch (hresult)
    {
      case -2147352573 /*0x80020003*/:
        throw Error.DispMemberNotFound((object) message);
      case -2147352571 /*0x80020005*/:
        throw Error.DispTypeMismatch((object) argErr, (object) message);
      case -2147352569 /*0x80020007*/:
        throw Error.DispNoNamedArgs((object) message);
      case -2147352567 /*0x80020009*/:
        throw excepInfo.GetException();
      case -2147352566 /*0x8002000A*/:
        throw Error.DispOverflow((object) message);
      case -2147352562 /*0x8002000E*/:
        throw Error.DispBadParamCount((object) message);
      case -2147352561 /*0x8002000F*/:
        throw Error.DispParamNotOptional((object) message);
      default:
        Marshal.ThrowExceptionForHR(hresult);
        break;
    }
  }

  internal static void GetInfoFromType(
    ITypeInfo typeInfo,
    out string name,
    out string documentation)
  {
    typeInfo.GetDocumentation(-1, out name, out documentation, out int _, out string _);
  }

  internal static string GetNameOfMethod(ITypeInfo typeInfo, int memid)
  {
    string[] rgBstrNames = new string[1];
    typeInfo.GetNames(memid, rgBstrNames, 1, out int _);
    return rgBstrNames[0];
  }

  internal static string GetNameOfLib(ITypeLib typeLib)
  {
    string strName;
    typeLib.GetDocumentation(-1, out strName, out string _, out int _, out string _);
    return strName;
  }

  internal static string GetNameOfType(ITypeInfo typeInfo)
  {
    string name;
    ComRuntimeHelpers.GetInfoFromType(typeInfo, out name, out string _);
    return name;
  }

  [SecurityCritical]
  internal static ITypeInfo GetITypeInfoFromIDispatch(
    IDispatch dispatch,
    bool throwIfMissingExpectedTypeInfo)
  {
    uint pctinfo;
    Marshal.ThrowExceptionForHR(dispatch.TryGetTypeInfoCount(out pctinfo));
    if (pctinfo == 0U)
      return (ITypeInfo) null;
    IntPtr info = IntPtr.Zero;
    int typeInfo = dispatch.TryGetTypeInfo(0U, 0, out info);
    if (!ComHresults.IsSuccess(typeInfo))
    {
      ComRuntimeHelpers.CheckIfMissingTypeInfoIsExpected(typeInfo, throwIfMissingExpectedTypeInfo);
      return (ITypeInfo) null;
    }
    if (info == IntPtr.Zero)
    {
      if (throwIfMissingExpectedTypeInfo)
        Marshal.ThrowExceptionForHR(-2147467259 /*0x80004005*/);
      return (ITypeInfo) null;
    }
    try
    {
      return Marshal.GetObjectForIUnknown(info) as ITypeInfo;
    }
    finally
    {
      Marshal.Release(info);
    }
  }

  [SecurityCritical]
  private static void CheckIfMissingTypeInfoIsExpected(
    int hresult,
    bool throwIfMissingExpectedTypeInfo)
  {
    if (hresult == -2147467262 /*0x80004002*/ || !throwIfMissingExpectedTypeInfo)
      return;
    Marshal.ThrowExceptionForHR(hresult);
  }

  [SecurityCritical]
  internal static System.Runtime.InteropServices.ComTypes.TYPEATTR GetTypeAttrForTypeInfo(
    ITypeInfo typeInfo)
  {
    IntPtr ppTypeAttr = IntPtr.Zero;
    typeInfo.GetTypeAttr(out ppTypeAttr);
    if (ppTypeAttr == IntPtr.Zero)
      throw Error.CannotRetrieveTypeInformation();
    try
    {
      return (System.Runtime.InteropServices.ComTypes.TYPEATTR) Marshal.PtrToStructure(ppTypeAttr, typeof (System.Runtime.InteropServices.ComTypes.TYPEATTR));
    }
    finally
    {
      typeInfo.ReleaseTypeAttr(ppTypeAttr);
    }
  }

  [SecurityCritical]
  internal static System.Runtime.InteropServices.ComTypes.TYPELIBATTR GetTypeAttrForTypeLib(
    ITypeLib typeLib)
  {
    IntPtr ppTLibAttr = IntPtr.Zero;
    typeLib.GetLibAttr(out ppTLibAttr);
    if (ppTLibAttr == IntPtr.Zero)
      throw Error.CannotRetrieveTypeInformation();
    try
    {
      return (System.Runtime.InteropServices.ComTypes.TYPELIBATTR) Marshal.PtrToStructure(ppTLibAttr, typeof (System.Runtime.InteropServices.ComTypes.TYPELIBATTR));
    }
    finally
    {
      typeLib.ReleaseTLibAttr(ppTLibAttr);
    }
  }

    public static BoundDispEvent CreateComEvent(object rcw, Guid sourceIid, int dispid)
  {
    return new BoundDispEvent(rcw, sourceIid, dispid);
  }

    public static DispCallable CreateDispCallable(IDispatchComObject dispatch, ComMethodDesc method)
  {
    return new DispCallable(dispatch, method.Name, method.DispId);
  }
}
