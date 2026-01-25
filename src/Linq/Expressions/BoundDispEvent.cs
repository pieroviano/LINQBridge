// Type: System.Dynamic.BoundDispEvent

using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class BoundDispEvent : DynamicObject
{
  private object _rcw;
  private Guid _sourceIid;
  private int _dispid;

  internal BoundDispEvent(object rcw, Guid sourceIid, int dispid)
  {
    this._rcw = rcw;
    this._sourceIid = sourceIid;
    this._dispid = dispid;
  }

  public override bool TryBinaryOperation(
    BinaryOperationBinder binder,
    object handler,
    out object result)
  {
    if (binder.Operation == ExpressionType.AddAssign)
    {
      result = this.InPlaceAdd(handler);
      return true;
    }
    if (binder.Operation == ExpressionType.SubtractAssign)
    {
      result = this.InPlaceSubtract(handler);
      return true;
    }
    result = (object) null;
    return false;
  }

  private static void VerifyHandler(object handler)
  {
    if (((object) (handler as Delegate) == null || !(handler.GetType() != typeof (Delegate))) && !(handler is IDynamicMetaObjectProvider))
      throw Error.UnsupportedHandlerType();
  }

  [SecuritySafeCritical]
  private object InPlaceAdd(object handler)
  {
    ContractUtils.RequiresNotNull(handler, nameof (handler));
    BoundDispEvent.VerifyHandler(handler);
    new PermissionSet(PermissionState.Unrestricted).Demand();
    ComEventSink.FromRuntimeCallableWrapper(this._rcw, this._sourceIid, true).AddHandler(this._dispid, handler);
    return (object) this;
  }

  [SecuritySafeCritical]
  private object InPlaceSubtract(object handler)
  {
    ContractUtils.RequiresNotNull(handler, nameof (handler));
    BoundDispEvent.VerifyHandler(handler);
    new PermissionSet(PermissionState.Unrestricted).Demand();
    ComEventSink.FromRuntimeCallableWrapper(this._rcw, this._sourceIid, false)?.RemoveHandler(this._dispid, handler);
    return (object) this;
  }
}
