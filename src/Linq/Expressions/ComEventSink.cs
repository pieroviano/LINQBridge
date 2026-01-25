// Type: System.Dynamic.ComEventSink
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class ComEventSink : MarshalByRefObject, System.Reflection.IReflect, IDisposable
{
  private Guid _sourceIid;
  private IConnectionPoint _connectionPoint;
  private int _adviseCookie;
  private List<ComEventSink.ComEventSinkMethod> _comEventSinkMethods;
  private object _lockObject = new object();

  [SecurityCritical]
  private ComEventSink(object rcw, Guid sourceIid) => this.Initialize(rcw, sourceIid);

  [SecurityCritical]
  private void Initialize(object rcw, Guid sourceIid)
  {
    this._sourceIid = sourceIid;
    this._adviseCookie = -1;
    if (!(rcw is IConnectionPointContainer connectionPointContainer))
      throw Error.COMObjectDoesNotSupportEvents();
    connectionPointContainer.FindConnectionPoint(ref this._sourceIid, out this._connectionPoint);
    if (this._connectionPoint == null)
      throw Error.COMObjectDoesNotSupportSourceInterface();
    this._connectionPoint.Advise(new ComEventSinkProxy(this, this._sourceIid).GetTransparentProxy(), out this._adviseCookie);
  }

  [SecurityCritical]
  public static ComEventSink FromRuntimeCallableWrapper(
    object rcw,
    Guid sourceIid,
    bool createIfNotFound)
  {
    List<ComEventSink> comEventSinkList = (List<ComEventSink>) ComEventSinksContainer.FromRuntimeCallableWrapper(rcw, createIfNotFound);
    if (comEventSinkList == null)
      return (ComEventSink) null;
    ComEventSink comEventSink1 = (ComEventSink) null;
    lock (comEventSinkList)
    {
      foreach (ComEventSink comEventSink2 in comEventSinkList)
      {
        if (comEventSink2._sourceIid == sourceIid)
        {
          comEventSink1 = comEventSink2;
          break;
        }
        if (comEventSink2._sourceIid == Guid.Empty)
        {
          comEventSink2.Initialize(rcw, sourceIid);
          comEventSink1 = comEventSink2;
        }
      }
      if (comEventSink1 == null)
      {
        if (createIfNotFound)
        {
          comEventSink1 = new ComEventSink(rcw, sourceIid);
          comEventSinkList.Add(comEventSink1);
        }
      }
    }
    return comEventSink1;
  }

  public void AddHandler(int dispid, object func)
  {
    string name = string.Format((IFormatProvider) CultureInfo.InvariantCulture, "[DISPID={0}]", new object[1]
    {
      (object) dispid
    });
    lock (this._lockObject)
    {
      ComEventSink.ComEventSinkMethod comEventSinkMethod = this.FindSinkMethod(name);
      if (comEventSinkMethod == null)
      {
        if (this._comEventSinkMethods == null)
          this._comEventSinkMethods = new List<ComEventSink.ComEventSinkMethod>();
        comEventSinkMethod = new ComEventSink.ComEventSinkMethod();
        comEventSinkMethod._name = name;
        this._comEventSinkMethods.Add(comEventSinkMethod);
      }
      comEventSinkMethod._handlers += new Func<object[], object>(new SplatCallSite(func).Invoke);
    }
  }

  [SecurityCritical]
  public void RemoveHandler(int dispid, object func)
  {
    string name = string.Format((IFormatProvider) CultureInfo.InvariantCulture, "[DISPID={0}]", new object[1]
    {
      (object) dispid
    });
    lock (this._lockObject)
    {
      ComEventSink.ComEventSinkMethod sinkMethod = this.FindSinkMethod(name);
      if (sinkMethod == null)
        return;
      foreach (Delegate invocation in sinkMethod._handlers.GetInvocationList())
      {
        if (invocation.Target is SplatCallSite target && target._callable.Equals(func))
        {
          sinkMethod._handlers -= invocation as Func<object[], object>;
          break;
        }
      }
      if (sinkMethod._handlers == null)
        this._comEventSinkMethods.Remove(sinkMethod);
      if (this._comEventSinkMethods.Count != 0)
        return;
      this.Dispose();
    }
  }

  public object ExecuteHandler(string name, object[] args)
  {
    ComEventSink.ComEventSinkMethod sinkMethod = this.FindSinkMethod(name);
    return sinkMethod != null && sinkMethod._handlers != null ? sinkMethod._handlers(args) : (object) null;
  }

  public FieldInfo GetField(string name, BindingFlags bindingAttr) => (FieldInfo) null;

  public FieldInfo[] GetFields(BindingFlags bindingAttr) => new FieldInfo[0];

  public MemberInfo[] GetMember(string name, BindingFlags bindingAttr) => new MemberInfo[0];

  public MemberInfo[] GetMembers(BindingFlags bindingAttr) => new MemberInfo[0];

  public MethodInfo GetMethod(string name, BindingFlags bindingAttr) => (MethodInfo) null;

  public MethodInfo GetMethod(
    string name,
    BindingFlags bindingAttr,
    Binder binder,
    Type[] types,
    ParameterModifier[] modifiers)
  {
    return (MethodInfo) null;
  }

  public MethodInfo[] GetMethods(BindingFlags bindingAttr) => new MethodInfo[0];

  public PropertyInfo GetProperty(
    string name,
    BindingFlags bindingAttr,
    Binder binder,
    Type returnType,
    Type[] types,
    ParameterModifier[] modifiers)
  {
    return (PropertyInfo) null;
  }

  public PropertyInfo GetProperty(string name, BindingFlags bindingAttr) => (PropertyInfo) null;

  public PropertyInfo[] GetProperties(BindingFlags bindingAttr) => new PropertyInfo[0];

  public Type UnderlyingSystemType => typeof (object);

  public object InvokeMember(
    string name,
    BindingFlags invokeAttr,
    Binder binder,
    object target,
    object[] args,
    ParameterModifier[] modifiers,
    CultureInfo culture,
    string[] namedParameters)
  {
    return this.ExecuteHandler(name, args);
  }

  [SecuritySafeCritical]
  public void Dispose()
  {
    this.DisposeAll();
    GC.SuppressFinalize((object) this);
  }

  [SecuritySafeCritical]
  ~ComEventSink() => this.DisposeAll();

  [SecurityCritical]
  private void DisposeAll()
  {
    if (this._connectionPoint == null)
      return;
    if (this._adviseCookie == -1)
      return;
    try
    {
      this._connectionPoint.Unadvise(this._adviseCookie);
      Marshal.ReleaseComObject((object) this._connectionPoint);
    }
    catch (Exception ex)
    {
      if (!(ex is COMException comException) || comException.ErrorCode != -2147220992 /*0x80040200*/)
        return;
      throw;
    }
    finally
    {
      this._connectionPoint = (IConnectionPoint) null;
      this._adviseCookie = -1;
      this._sourceIid = Guid.Empty;
    }
  }

  private ComEventSink.ComEventSinkMethod FindSinkMethod(string name)
  {
    return this._comEventSinkMethods == null ? (ComEventSink.ComEventSinkMethod) null : this._comEventSinkMethods.Find((Predicate<ComEventSink.ComEventSinkMethod>) (element => element._name == name));
  }

  private class ComEventSinkMethod
  {
    public string _name;
    public Func<object[], object> _handlers;
  }
}
