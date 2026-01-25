// Type: System.Dynamic.ComObject
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

#nullable disable
namespace System.Linq.Expressions;

internal class ComObject : IDynamicMetaObjectProvider
{
  private readonly object _rcw;
  private static readonly object _ComObjectInfoKey = new object();
  private static readonly Type ComObjectType = typeof (object).Assembly.GetType("System.__ComObject");

  internal ComObject(object rcw) => this._rcw = rcw;

  internal object RuntimeCallableWrapper
  {
    get => this._rcw;
  }

  [SecurityCritical]
    public static ComObject ObjectToComObject(object rcw)
  {
    object comObjectData1 = Marshal.GetComObjectData(rcw, ComObject._ComObjectInfoKey);
    if (comObjectData1 != null)
      return (ComObject) comObjectData1;
    lock (ComObject._ComObjectInfoKey)
    {
      object comObjectData2 = Marshal.GetComObjectData(rcw, ComObject._ComObjectInfoKey);
      if (comObjectData2 != null)
        return (ComObject) comObjectData2;
      ComObject comObject = ComObject.CreateComObject(rcw);
      if (!Marshal.SetComObjectData(rcw, ComObject._ComObjectInfoKey, (object) comObject))
        throw Error.SetComObjectDataFailed();
      return comObject;
    }
  }

  internal static MemberExpression RcwFromComObject(Expression comObject)
  {
    return Expression.Property(Helpers.Convert(comObject, typeof (ComObject)), typeof (ComObject).GetProperty("RuntimeCallableWrapper", BindingFlags.Instance | BindingFlags.NonPublic));
  }

  internal static MethodCallExpression RcwToComObject(Expression rcw)
  {
    return Expression.Call(typeof (ComObject).GetMethod("ObjectToComObject"), Helpers.Convert(rcw, typeof (object)));
  }

  private static ComObject CreateComObject(object rcw)
  {
    return rcw is IDispatch rcw1 ? (ComObject) new IDispatchComObject(rcw1) : new ComObject(rcw);
  }

  internal virtual IList<string> GetMemberNames(bool dataOnly) => (IList<string>) new string[0];

  internal virtual IList<KeyValuePair<string, object>> GetMembers(IEnumerable<string> names)
  {
    return (IList<KeyValuePair<string, object>>) new KeyValuePair<string, object>[0];
  }

  DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
  {
    return (DynamicMetaObject) new ComFallbackMetaObject(parameter, BindingRestrictions.Empty, (object) this);
  }

    internal static bool IsComObject(object obj)
  {
    return obj != null && ComObject.ComObjectType.IsAssignableFrom(obj.GetType());
  }
}
