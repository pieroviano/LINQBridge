// Type: System.Dynamic.ComTypeDesc
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using System.Threading;

#nullable disable
namespace System.Linq.Expressions;

internal class ComTypeDesc
{
  private string _typeName;
  private string _documentation;
  private Guid _guid;
  private Hashtable _funcs;
  private Hashtable _puts;
  private Hashtable _putRefs;
  private ComMethodDesc _getItem;
  private ComMethodDesc _setItem;
  private Dictionary<string, ComEventDesc> _events;
  private static readonly Dictionary<string, ComEventDesc> _EmptyEventsDict = new Dictionary<string, ComEventDesc>();

  internal ComTypeDesc(ITypeInfo typeInfo)
  {
    if (typeInfo == null)
      return;
    ComRuntimeHelpers.GetInfoFromType(typeInfo, out this._typeName, out this._documentation);
  }

  [SecurityCritical]
  internal static ComTypeDesc FromITypeInfo(ITypeInfo typeInfo, TYPEATTR typeAttr)
  {
    if (typeAttr.typekind == TYPEKIND.TKIND_COCLASS)
      return (ComTypeDesc) new ComTypeClassDesc(typeInfo);
    if (typeAttr.typekind == TYPEKIND.TKIND_ENUM)
      return (ComTypeDesc) new ComTypeEnumDesc(typeInfo);
    if (typeAttr.typekind == TYPEKIND.TKIND_DISPATCH || typeAttr.typekind == TYPEKIND.TKIND_INTERFACE)
      return new ComTypeDesc(typeInfo);
    throw Error.UnsupportedEnumType();
  }

  internal static ComTypeDesc CreateEmptyTypeDesc()
  {
    return new ComTypeDesc((ITypeInfo) null)
    {
      _funcs = new Hashtable(),
      _puts = new Hashtable(),
      _putRefs = new Hashtable(),
      _events = ComTypeDesc._EmptyEventsDict
    };
  }

  internal static Dictionary<string, ComEventDesc> EmptyEvents => ComTypeDesc._EmptyEventsDict;

  internal Hashtable Funcs
  {
    get => this._funcs;
    set => this._funcs = value;
  }

  internal Hashtable Puts
  {
    set => this._puts = value;
  }

  internal Hashtable PutRefs
  {
    set => this._putRefs = value;
  }

  internal Dictionary<string, ComEventDesc> Events
  {
    get => this._events;
    set => this._events = value;
  }

  internal bool TryGetFunc(string name, out ComMethodDesc method)
  {
    name = name.ToUpper(CultureInfo.InvariantCulture);
    if (this._funcs.ContainsKey((object) name))
    {
      method = this._funcs[(object) name] as ComMethodDesc;
      return true;
    }
    method = (ComMethodDesc) null;
    return false;
  }

  internal void AddFunc(string name, ComMethodDesc method)
  {
    name = name.ToUpper(CultureInfo.InvariantCulture);
    lock (this._funcs)
      this._funcs[(object) name] = (object) method;
  }

  internal bool TryGetPut(string name, out ComMethodDesc method)
  {
    name = name.ToUpper(CultureInfo.InvariantCulture);
    if (this._puts.ContainsKey((object) name))
    {
      method = this._puts[(object) name] as ComMethodDesc;
      return true;
    }
    method = (ComMethodDesc) null;
    return false;
  }

  internal void AddPut(string name, ComMethodDesc method)
  {
    name = name.ToUpper(CultureInfo.InvariantCulture);
    lock (this._puts)
      this._puts[(object) name] = (object) method;
  }

  internal bool TryGetPutRef(string name, out ComMethodDesc method)
  {
    name = name.ToUpper(CultureInfo.InvariantCulture);
    if (this._putRefs.ContainsKey((object) name))
    {
      method = this._putRefs[(object) name] as ComMethodDesc;
      return true;
    }
    method = (ComMethodDesc) null;
    return false;
  }

  internal void AddPutRef(string name, ComMethodDesc method)
  {
    name = name.ToUpper(CultureInfo.InvariantCulture);
    lock (this._putRefs)
      this._putRefs[(object) name] = (object) method;
  }

  internal bool TryGetEvent(string name, out ComEventDesc @event)
  {
    name = name.ToUpper(CultureInfo.InvariantCulture);
    return this._events.TryGetValue(name, out @event);
  }

  internal string[] GetMemberNames(bool dataOnly)
  {
    Dictionary<string, object> dictionary = new Dictionary<string, object>();
    lock (this._funcs)
    {
      foreach (ComMethodDesc comMethodDesc in (System.Collections.IEnumerable) this._funcs.Values)
      {
        if (!dataOnly || comMethodDesc.IsDataMember)
          dictionary.Add(comMethodDesc.Name, (object) null);
      }
    }
    if (!dataOnly)
    {
      lock (this._puts)
      {
        foreach (ComMethodDesc comMethodDesc in (System.Collections.IEnumerable) this._puts.Values)
        {
          if (!dictionary.ContainsKey(comMethodDesc.Name))
            dictionary.Add(comMethodDesc.Name, (object) null);
        }
      }
      lock (this._putRefs)
      {
        foreach (ComMethodDesc comMethodDesc in (System.Collections.IEnumerable) this._putRefs.Values)
        {
          if (!dictionary.ContainsKey(comMethodDesc.Name))
            dictionary.Add(comMethodDesc.Name, (object) null);
        }
      }
      if (this._events != null && this._events.Count > 0)
      {
        foreach (string key in this._events.Keys)
        {
          if (!dictionary.ContainsKey(key))
            dictionary.Add(key, (object) null);
        }
      }
    }
    string[] array = new string[dictionary.Keys.Count];
    dictionary.Keys.CopyTo(array, 0);
    return array;
  }

  internal string TypeName => this._typeName;

  internal Guid Guid
  {
    get => this._guid;
    set => this._guid = value;
  }

  internal ComMethodDesc GetItem => this._getItem;

  internal void EnsureGetItem(ComMethodDesc candidate)
  {
    Interlocked.CompareExchange<ComMethodDesc>(ref this._getItem, candidate, (ComMethodDesc) null);
  }

  internal ComMethodDesc SetItem => this._setItem;

  internal void EnsureSetItem(ComMethodDesc candidate)
  {
    Interlocked.CompareExchange<ComMethodDesc>(ref this._setItem, candidate, (ComMethodDesc) null);
  }
}
