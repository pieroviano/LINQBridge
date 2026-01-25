// Type: System.Dynamic.IDispatchComObject
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using System.Security.Permissions;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class IDispatchComObject : ComObject, IDynamicMetaObjectProvider
{
  private readonly IDispatch _dispatchObject;
  private ComTypeDesc _comTypeDesc;
  private static readonly Dictionary<Guid, ComTypeDesc> _CacheComTypeDesc = new Dictionary<Guid, ComTypeDesc>();
  private bool _useTypeDescCache;

  internal IDispatchComObject(IDispatch rcw)
    : base((object) rcw)
  {
    this._dispatchObject = rcw;
    this._useTypeDescCache = !(rcw is IDispatchEx);
  }

  public override string ToString()
  {
    ComTypeDesc comTypeDesc = this._comTypeDesc;
    string str = (string) null;
    if (comTypeDesc != null)
      str = comTypeDesc.TypeName;
    if (string.IsNullOrEmpty(str))
      str = "IDispatch";
    return string.Format((IFormatProvider) CultureInfo.CurrentCulture, "{0} ({1})", new object[2]
    {
      (object) this.RuntimeCallableWrapper.ToString(),
      (object) str
    });
  }

  public ComTypeDesc ComTypeDesc
  {
    get
    {
      this.EnsureScanDefinedMethods();
      return this._comTypeDesc;
    }
  }

  public IDispatch DispatchObject
  {
    get => this._dispatchObject;
  }

  private static int GetIDsOfNames(IDispatch dispatch, string name, out int dispId)
  {
    int[] rgDispId = new int[1];
    Guid empty = Guid.Empty;
    int idsOfNames = dispatch.TryGetIDsOfNames(ref empty, new string[1]
    {
      name
    }, 1U, 0, rgDispId);
    dispId = rgDispId[0];
    return idsOfNames;
  }

  private static int Invoke(IDispatch dispatch, int memberDispId, out object result)
  {
    Guid empty = Guid.Empty;
    System.Runtime.InteropServices.ComTypes.DISPPARAMS pDispParams = new System.Runtime.InteropServices.ComTypes.DISPPARAMS();
    System.Runtime.InteropServices.ComTypes.EXCEPINFO pExcepInfo = new System.Runtime.InteropServices.ComTypes.EXCEPINFO();
    return dispatch.TryInvoke(memberDispId, ref empty, 0, System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYGET, ref pDispParams, out result, out pExcepInfo, out uint _);
  }

  internal bool TryGetGetItem(out ComMethodDesc value)
  {
    ComMethodDesc getItem = this._comTypeDesc.GetItem;
    if (getItem == null)
      return this.SlowTryGetGetItem(out value);
    value = getItem;
    return true;
  }

  private bool SlowTryGetGetItem(out ComMethodDesc value)
  {
    this.EnsureScanDefinedMethods();
    ComMethodDesc getItem = this._comTypeDesc.GetItem;
    if (getItem == null)
    {
      this._comTypeDesc.EnsureGetItem(new ComMethodDesc("[PROPERTYGET, DISPID(0)]", 0, System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYGET));
      getItem = this._comTypeDesc.GetItem;
    }
    value = getItem;
    return true;
  }

  internal bool TryGetSetItem(out ComMethodDesc value)
  {
    ComMethodDesc setItem = this._comTypeDesc.SetItem;
    if (setItem == null)
      return this.SlowTryGetSetItem(out value);
    value = setItem;
    return true;
  }

  private bool SlowTryGetSetItem(out ComMethodDesc value)
  {
    this.EnsureScanDefinedMethods();
    ComMethodDesc setItem = this._comTypeDesc.SetItem;
    if (setItem == null)
    {
      this._comTypeDesc.EnsureSetItem(new ComMethodDesc("[PROPERTYPUT, DISPID(0)]", 0, System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT));
      setItem = this._comTypeDesc.SetItem;
    }
    value = setItem;
    return true;
  }

  internal bool TryGetMemberMethod(string name, out ComMethodDesc method)
  {
    this.EnsureScanDefinedMethods();
    return this._comTypeDesc.TryGetFunc(name, out method);
  }

  internal bool TryGetMemberEvent(string name, out ComEventDesc @event)
  {
    this.EnsureScanDefinedEvents();
    return this._comTypeDesc.TryGetEvent(name, out @event);
  }

  internal bool TryGetMemberMethodExplicit(string name, out ComMethodDesc method)
  {
    this.EnsureScanDefinedMethods();
    int dispId;
    int idsOfNames = IDispatchComObject.GetIDsOfNames(this._dispatchObject, name, out dispId);
    switch (idsOfNames)
    {
      case -2147352570 /*0x80020006*/:
        method = (ComMethodDesc) null;
        return false;
      case 0:
        ComMethodDesc method1 = new ComMethodDesc(name, dispId, System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_FUNC);
        this._comTypeDesc.AddFunc(name, method1);
        method = method1;
        return true;
      default:
        throw Error.CouldNotGetDispId((object) name, (object) string.Format((IFormatProvider) CultureInfo.InvariantCulture, "0x{0:X})", new object[1]
        {
          (object) idsOfNames
        }));
    }
  }

  internal bool TryGetPropertySetterExplicit(
    string name,
    out ComMethodDesc method,
    Type limitType,
    bool holdsNull)
  {
    this.EnsureScanDefinedMethods();
    int dispId;
    int idsOfNames = IDispatchComObject.GetIDsOfNames(this._dispatchObject, name, out dispId);
    switch (idsOfNames)
    {
      case -2147352570 /*0x80020006*/:
        method = (ComMethodDesc) null;
        return false;
      case 0:
        ComMethodDesc method1 = new ComMethodDesc(name, dispId, System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT);
        this._comTypeDesc.AddPut(name, method1);
        ComMethodDesc method2 = new ComMethodDesc(name, dispId, System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF);
        this._comTypeDesc.AddPutRef(name, method2);
        method = !ComBinderHelpers.PreferPut(limitType, holdsNull) ? method2 : method1;
        return true;
      default:
        throw Error.CouldNotGetDispId((object) name, (object) string.Format((IFormatProvider) CultureInfo.InvariantCulture, "0x{0:X})", new object[1]
        {
          (object) idsOfNames
        }));
    }
  }

  internal override IList<string> GetMemberNames(bool dataOnly)
  {
    this.EnsureScanDefinedMethods();
    this.EnsureScanDefinedEvents();
    return (IList<string>) this.ComTypeDesc.GetMemberNames(dataOnly);
  }

  internal override IList<KeyValuePair<string, object>> GetMembers(IEnumerable<string> names)
  {
    if (names == null)
      names = (IEnumerable<string>) this.GetMemberNames(true);
    Type type = this.RuntimeCallableWrapper.GetType();
    List<KeyValuePair<string, object>> keyValuePairList = new List<KeyValuePair<string, object>>();
    foreach (string name in names)
    {
      ComMethodDesc method;
      if (name != null && this.ComTypeDesc.TryGetFunc(name, out method))
      {
        if (method.IsDataMember)
        {
          try
          {
            object obj = type.InvokeMember(method.Name, BindingFlags.GetProperty, (Binder) null, this.RuntimeCallableWrapper, new object[0], CultureInfo.InvariantCulture);
            keyValuePairList.Add(new KeyValuePair<string, object>(method.Name, obj));
          }
          catch (Exception ex)
          {
            keyValuePairList.Add(new KeyValuePair<string, object>(method.Name, (object) ex));
          }
        }
      }
    }
    return (IList<KeyValuePair<string, object>>) keyValuePairList.ToArray();
  }

  DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
  {
    this.EnsureScanDefinedMethods();
    return (DynamicMetaObject) new IDispatchMetaObject(parameter, this);
  }

  [SecurityCritical]
  private static void GetFuncDescForDescIndex(
    ITypeInfo typeInfo,
    int funcIndex,
    out System.Runtime.InteropServices.ComTypes.FUNCDESC funcDesc,
    out IntPtr funcDescHandle)
  {
    IntPtr ppFuncDesc = IntPtr.Zero;
    typeInfo.GetFuncDesc(funcIndex, out ppFuncDesc);
    funcDesc = !(ppFuncDesc == IntPtr.Zero) ? (System.Runtime.InteropServices.ComTypes.FUNCDESC) Marshal.PtrToStructure(ppFuncDesc, typeof (System.Runtime.InteropServices.ComTypes.FUNCDESC)) : throw Error.CannotRetrieveTypeInformation();
    funcDescHandle = ppFuncDesc;
  }

  [SecuritySafeCritical]
  private void EnsureScanDefinedEvents()
  {
    if (this._comTypeDesc != null && this._comTypeDesc.Events != null)
      return;
    new PermissionSet(PermissionState.Unrestricted).Demand();
    ITypeInfo infoFromIdispatch = ComRuntimeHelpers.GetITypeInfoFromIDispatch(this._dispatchObject, true);
    if (infoFromIdispatch == null)
    {
      this._comTypeDesc = ComTypeDesc.CreateEmptyTypeDesc();
    }
    else
    {
      System.Runtime.InteropServices.ComTypes.TYPEATTR typeAttrForTypeInfo1 = ComRuntimeHelpers.GetTypeAttrForTypeInfo(infoFromIdispatch);
      if (this._comTypeDesc == null && this._useTypeDescCache)
      {
        lock (IDispatchComObject._CacheComTypeDesc)
        {
          if (IDispatchComObject._CacheComTypeDesc.TryGetValue(typeAttrForTypeInfo1.guid, out this._comTypeDesc))
          {
            if (this._comTypeDesc.Events != null)
              return;
          }
        }
      }
      ComTypeDesc comTypeDesc1 = ComTypeDesc.FromITypeInfo(infoFromIdispatch, typeAttrForTypeInfo1);
      Dictionary<string, ComEventDesc> events;
      if (!(this.RuntimeCallableWrapper is IConnectionPointContainer))
      {
        events = ComTypeDesc.EmptyEvents;
      }
      else
      {
        ITypeInfo coClassTypeInfo;
        if ((coClassTypeInfo = IDispatchComObject.GetCoClassTypeInfo(this.RuntimeCallableWrapper, infoFromIdispatch)) == null)
        {
          events = ComTypeDesc.EmptyEvents;
        }
        else
        {
          events = new Dictionary<string, ComEventDesc>();
          System.Runtime.InteropServices.ComTypes.TYPEATTR typeAttrForTypeInfo2 = ComRuntimeHelpers.GetTypeAttrForTypeInfo(coClassTypeInfo);
          for (int index = 0; index < (int) typeAttrForTypeInfo2.cImplTypes; ++index)
          {
            int href;
            coClassTypeInfo.GetRefTypeOfImplType(index, out href);
            ITypeInfo ppTI;
            coClassTypeInfo.GetRefTypeInfo(href, out ppTI);
            System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS pImplTypeFlags;
            coClassTypeInfo.GetImplTypeFlags(index, out pImplTypeFlags);
            if ((pImplTypeFlags & System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS.IMPLTYPEFLAG_FSOURCE) != (System.Runtime.InteropServices.ComTypes.IMPLTYPEFLAGS) 0)
              IDispatchComObject.ScanSourceInterface(ppTI, ref events);
          }
          if (events.Count == 0)
            events = ComTypeDesc.EmptyEvents;
        }
      }
      if (this._useTypeDescCache)
      {
        lock (IDispatchComObject._CacheComTypeDesc)
        {
          ComTypeDesc comTypeDesc2;
          if (IDispatchComObject._CacheComTypeDesc.TryGetValue(typeAttrForTypeInfo1.guid, out comTypeDesc2))
          {
            this._comTypeDesc = comTypeDesc2;
          }
          else
          {
            this._comTypeDesc = comTypeDesc1;
            IDispatchComObject._CacheComTypeDesc.Add(typeAttrForTypeInfo1.guid, this._comTypeDesc);
          }
          this._comTypeDesc.Events = events;
        }
      }
      else
      {
        comTypeDesc1.Events = events;
        this._comTypeDesc = comTypeDesc1;
      }
    }
  }

  [SecurityCritical]
  private static void ScanSourceInterface(
    ITypeInfo sourceTypeInfo,
    ref Dictionary<string, ComEventDesc> events)
  {
    System.Runtime.InteropServices.ComTypes.TYPEATTR typeAttrForTypeInfo = ComRuntimeHelpers.GetTypeAttrForTypeInfo(sourceTypeInfo);
    for (int funcIndex = 0; funcIndex < (int) typeAttrForTypeInfo.cFuncs; ++funcIndex)
    {
      IntPtr funcDescHandle = IntPtr.Zero;
      try
      {
        System.Runtime.InteropServices.ComTypes.FUNCDESC funcDesc;
        IDispatchComObject.GetFuncDescForDescIndex(sourceTypeInfo, funcIndex, out funcDesc, out funcDescHandle);
        if (((int) funcDesc.wFuncFlags & 64 /*0x40*/) == 0)
        {
          if (((int) funcDesc.wFuncFlags & 1) == 0)
          {
            string upper = ComRuntimeHelpers.GetNameOfMethod(sourceTypeInfo, funcDesc.memid).ToUpper(CultureInfo.InvariantCulture);
            if (!events.ContainsKey(upper))
              events.Add(upper, new ComEventDesc()
              {
                dispid = funcDesc.memid,
                sourceIID = typeAttrForTypeInfo.guid
              });
          }
        }
      }
      finally
      {
        if (funcDescHandle != IntPtr.Zero)
          sourceTypeInfo.ReleaseFuncDesc(funcDescHandle);
      }
    }
  }

  [SecurityCritical]
  private static ITypeInfo GetCoClassTypeInfo(object rcw, ITypeInfo typeInfo)
  {
    if (rcw is IProvideClassInfo provideClassInfo)
    {
      IntPtr info = IntPtr.Zero;
      try
      {
        provideClassInfo.GetClassInfo(out info);
        if (info != IntPtr.Zero)
          return Marshal.GetObjectForIUnknown(info) as ITypeInfo;
      }
      finally
      {
        if (info != IntPtr.Zero)
          Marshal.Release(info);
      }
    }
    ITypeLib ppTLB;
    typeInfo.GetContainingTypeLib(out ppTLB, out int _);
    string nameOfType = ComRuntimeHelpers.GetNameOfType(typeInfo);
    ComTypeClassDesc classForInterface = ComTypeLibDesc.GetFromTypeLib(ppTLB).GetCoClassForInterface(nameOfType);
    if (classForInterface == null)
      return (ITypeInfo) null;
    Guid guid = classForInterface.Guid;
    ITypeInfo ppTInfo;
    ppTLB.GetTypeInfoOfGuid(ref guid, out ppTInfo);
    return ppTInfo;
  }

  [SecuritySafeCritical]
  private void EnsureScanDefinedMethods()
  {
    if (this._comTypeDesc != null && this._comTypeDesc.Funcs != null)
      return;
    new PermissionSet(PermissionState.Unrestricted).Demand();
    ITypeInfo infoFromIdispatch = ComRuntimeHelpers.GetITypeInfoFromIDispatch(this._dispatchObject, true);
    if (infoFromIdispatch == null)
    {
      this._comTypeDesc = ComTypeDesc.CreateEmptyTypeDesc();
    }
    else
    {
      System.Runtime.InteropServices.ComTypes.TYPEATTR typeAttrForTypeInfo = ComRuntimeHelpers.GetTypeAttrForTypeInfo(infoFromIdispatch);
      if (this._comTypeDesc == null && this._useTypeDescCache)
      {
        lock (IDispatchComObject._CacheComTypeDesc)
        {
          if (IDispatchComObject._CacheComTypeDesc.TryGetValue(typeAttrForTypeInfo.guid, out this._comTypeDesc))
          {
            if (this._comTypeDesc.Funcs != null)
              return;
          }
        }
      }
      ComTypeDesc comTypeDesc1 = ComTypeDesc.FromITypeInfo(infoFromIdispatch, typeAttrForTypeInfo);
      ComMethodDesc candidate1 = (ComMethodDesc) null;
      ComMethodDesc candidate2 = (ComMethodDesc) null;
      Hashtable hashtable1 = new Hashtable((int) typeAttrForTypeInfo.cFuncs);
      Hashtable hashtable2 = new Hashtable();
      Hashtable hashtable3 = new Hashtable();
      for (int funcIndex = 0; funcIndex < (int) typeAttrForTypeInfo.cFuncs; ++funcIndex)
      {
        IntPtr funcDescHandle = IntPtr.Zero;
        try
        {
          System.Runtime.InteropServices.ComTypes.FUNCDESC funcDesc;
          IDispatchComObject.GetFuncDescForDescIndex(infoFromIdispatch, funcIndex, out funcDesc, out funcDescHandle);
          if (((int) funcDesc.wFuncFlags & 1) == 0)
          {
            ComMethodDesc comMethodDesc = new ComMethodDesc(infoFromIdispatch, funcDesc);
            string upper = comMethodDesc.Name.ToUpper(CultureInfo.InvariantCulture);
            if ((funcDesc.invkind & System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT) != (System.Runtime.InteropServices.ComTypes.INVOKEKIND) 0)
            {
              hashtable2.Add((object) upper, (object) comMethodDesc);
              if (comMethodDesc.DispId == 0)
              {
                if (candidate2 == null)
                  candidate2 = comMethodDesc;
              }
            }
            else if ((funcDesc.invkind & System.Runtime.InteropServices.ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF) != (System.Runtime.InteropServices.ComTypes.INVOKEKIND) 0)
            {
              hashtable3.Add((object) upper, (object) comMethodDesc);
              if (comMethodDesc.DispId == 0)
              {
                if (candidate2 == null)
                  candidate2 = comMethodDesc;
              }
            }
            else if (funcDesc.memid == -4)
            {
              hashtable1.Add((object) "GETENUMERATOR", (object) comMethodDesc);
            }
            else
            {
              hashtable1.Add((object) upper, (object) comMethodDesc);
              if (funcDesc.memid == 0)
                candidate1 = comMethodDesc;
            }
          }
        }
        finally
        {
          if (funcDescHandle != IntPtr.Zero)
            infoFromIdispatch.ReleaseFuncDesc(funcDescHandle);
        }
      }
      if (this._useTypeDescCache)
      {
        lock (IDispatchComObject._CacheComTypeDesc)
        {
          ComTypeDesc comTypeDesc2;
          if (IDispatchComObject._CacheComTypeDesc.TryGetValue(typeAttrForTypeInfo.guid, out comTypeDesc2))
          {
            this._comTypeDesc = comTypeDesc2;
          }
          else
          {
            this._comTypeDesc = comTypeDesc1;
            IDispatchComObject._CacheComTypeDesc.Add(typeAttrForTypeInfo.guid, this._comTypeDesc);
          }
          this._comTypeDesc.Funcs = hashtable1;
          this._comTypeDesc.Puts = hashtable2;
          this._comTypeDesc.PutRefs = hashtable3;
          this._comTypeDesc.EnsureGetItem(candidate1);
          this._comTypeDesc.EnsureSetItem(candidate2);
        }
      }
      else
      {
        comTypeDesc1.Funcs = hashtable1;
        comTypeDesc1.Puts = hashtable2;
        comTypeDesc1.PutRefs = hashtable3;
        comTypeDesc1.EnsureGetItem(candidate1);
        comTypeDesc1.EnsureSetItem(candidate2);
        this._comTypeDesc = comTypeDesc1;
      }
    }
  }

  internal bool TryGetPropertySetter(
    string name,
    out ComMethodDesc method,
    Type limitType,
    bool holdsNull)
  {
    this.EnsureScanDefinedMethods();
    return ComBinderHelpers.PreferPut(limitType, holdsNull) ? this._comTypeDesc.TryGetPut(name, out method) || this._comTypeDesc.TryGetPutRef(name, out method) : this._comTypeDesc.TryGetPutRef(name, out method) || this._comTypeDesc.TryGetPut(name, out method);
  }
}
