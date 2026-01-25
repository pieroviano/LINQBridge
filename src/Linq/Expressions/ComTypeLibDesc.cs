// Type: System.Dynamic.ComTypeLibDesc
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices.ComTypes;
using System.Security;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class ComTypeLibDesc
{
  private LinkedList<ComTypeClassDesc> _classes;
  private Dictionary<string, ComTypeEnumDesc> _enums;
  private string _typeLibName;
  private static readonly Dictionary<Guid, ComTypeLibDesc> _CachedTypeLibDesc = new Dictionary<Guid, ComTypeLibDesc>();

  private ComTypeLibDesc()
  {
    this._enums = new Dictionary<string, ComTypeEnumDesc>();
    this._classes = new LinkedList<ComTypeClassDesc>();
  }

  public override string ToString()
  {
    return string.Format((IFormatProvider) CultureInfo.CurrentCulture, "<type library {0}>", new object[1]
    {
      (object) this._typeLibName
    });
  }

  [SecurityCritical]
  internal static ComTypeLibDesc GetFromTypeLib(ITypeLib typeLib)
  {
    TYPELIBATTR typeAttrForTypeLib = ComRuntimeHelpers.GetTypeAttrForTypeLib(typeLib);
    lock (ComTypeLibDesc._CachedTypeLibDesc)
    {
      ComTypeLibDesc fromTypeLib;
      if (ComTypeLibDesc._CachedTypeLibDesc.TryGetValue(typeAttrForTypeLib.guid, out fromTypeLib))
        return fromTypeLib;
    }
    ComTypeLibDesc fromTypeLib1 = new ComTypeLibDesc();
    fromTypeLib1._typeLibName = ComRuntimeHelpers.GetNameOfLib(typeLib);
    int typeInfoCount = typeLib.GetTypeInfoCount();
    for (int index = 0; index < typeInfoCount; ++index)
    {
      TYPEKIND pTKind;
      typeLib.GetTypeInfoType(index, out pTKind);
      ITypeInfo ppTI;
      switch (pTKind)
      {
        case TYPEKIND.TKIND_ENUM:
          typeLib.GetTypeInfo(index, out ppTI);
          ComTypeEnumDesc comTypeEnumDesc = new ComTypeEnumDesc(ppTI);
          fromTypeLib1._enums.Add(comTypeEnumDesc.TypeName, comTypeEnumDesc);
          break;
        case TYPEKIND.TKIND_COCLASS:
          typeLib.GetTypeInfo(index, out ppTI);
          ComTypeClassDesc comTypeClassDesc = new ComTypeClassDesc(ppTI);
          fromTypeLib1._classes.AddLast(comTypeClassDesc);
          break;
      }
    }
    lock (ComTypeLibDesc._CachedTypeLibDesc)
    {
      ComTypeLibDesc fromTypeLib2;
      if (ComTypeLibDesc._CachedTypeLibDesc.TryGetValue(typeAttrForTypeLib.guid, out fromTypeLib2))
        return fromTypeLib2;
      ComTypeLibDesc._CachedTypeLibDesc.Add(typeAttrForTypeLib.guid, fromTypeLib1);
    }
    return fromTypeLib1;
  }

  internal ComTypeClassDesc GetCoClassForInterface(string itfName)
  {
    foreach (ComTypeClassDesc classForInterface in this._classes)
    {
      if (classForInterface.Implements(itfName, false))
        return classForInterface;
    }
    return (ComTypeClassDesc) null;
  }
}
