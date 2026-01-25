// Type: System.Dynamic.ComTypeClassDesc
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Security;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class ComTypeClassDesc : ComTypeDesc
{
  private LinkedList<string> _itfs;
  private LinkedList<string> _sourceItfs;

  [SecurityCritical]
  internal ComTypeClassDesc(ITypeInfo typeInfo)
    : base(typeInfo)
  {
    TYPEATTR typeAttrForTypeInfo = ComRuntimeHelpers.GetTypeAttrForTypeInfo(typeInfo);
    this.Guid = typeAttrForTypeInfo.guid;
    for (int index = 0; index < (int) typeAttrForTypeInfo.cImplTypes; ++index)
    {
      int href;
      typeInfo.GetRefTypeOfImplType(index, out href);
      ITypeInfo ppTI;
      typeInfo.GetRefTypeInfo(href, out ppTI);
      IMPLTYPEFLAGS pImplTypeFlags;
      typeInfo.GetImplTypeFlags(index, out pImplTypeFlags);
      bool isSourceItf = (pImplTypeFlags & IMPLTYPEFLAGS.IMPLTYPEFLAG_FSOURCE) != 0;
      this.AddInterface(ppTI, isSourceItf);
    }
  }

  private void AddInterface(ITypeInfo itfTypeInfo, bool isSourceItf)
  {
    string nameOfType = ComRuntimeHelpers.GetNameOfType(itfTypeInfo);
    if (isSourceItf)
    {
      if (this._sourceItfs == null)
        this._sourceItfs = new LinkedList<string>();
      this._sourceItfs.AddLast(nameOfType);
    }
    else
    {
      if (this._itfs == null)
        this._itfs = new LinkedList<string>();
      this._itfs.AddLast(nameOfType);
    }
  }

  internal bool Implements(string itfName, bool isSourceItf)
  {
    return isSourceItf ? this._sourceItfs.Contains(itfName) : this._itfs.Contains(itfName);
  }
}
