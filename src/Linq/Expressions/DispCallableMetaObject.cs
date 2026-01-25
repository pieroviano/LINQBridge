// Type: System.Dynamic.DispCallableMetaObject
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security;
using System.Security.Permissions;

#nullable disable
namespace System.Linq.Expressions;

internal class DispCallableMetaObject : DynamicMetaObject
{
  private readonly DispCallable _callable;

  internal DispCallableMetaObject(Expression expression, DispCallable callable)
    : base(expression, BindingRestrictions.Empty, (object) callable)
  {
    this._callable = callable;
  }

  public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
  {
    return this.BindGetOrInvoke(indexes, binder.CallInfo) ?? base.BindGetIndex(binder, indexes);
  }

  public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
  {
    return this.BindGetOrInvoke(args, binder.CallInfo) ?? base.BindInvoke(binder, args);
  }

  [SecuritySafeCritical]
  private DynamicMetaObject BindGetOrInvoke(DynamicMetaObject[] args, CallInfo callInfo)
  {
    new PermissionSet(PermissionState.Unrestricted).Demand();
    IDispatchComObject dispatchComObject = this._callable.DispatchComObject;
    string memberName = this._callable.MemberName;
    ComMethodDesc method;
    if (!dispatchComObject.TryGetMemberMethod(memberName, out method) && !dispatchComObject.TryGetMemberMethodExplicit(memberName, out method))
      return (DynamicMetaObject) null;
    bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(ref args);
    return this.BindComInvoke(method, args, callInfo, isByRef);
  }

  [SecuritySafeCritical]
  public override DynamicMetaObject BindSetIndex(
    SetIndexBinder binder,
    DynamicMetaObject[] indexes,
    DynamicMetaObject value)
  {
    new PermissionSet(PermissionState.Unrestricted).Demand();
    IDispatchComObject dispatchComObject = this._callable.DispatchComObject;
    string memberName = this._callable.MemberName;
    bool holdsNull = value.Value == null && value.HasValue;
    ComMethodDesc method;
    if (!dispatchComObject.TryGetPropertySetter(memberName, out method, value.LimitType, holdsNull) && !dispatchComObject.TryGetPropertySetterExplicit(memberName, out method, value.LimitType, holdsNull))
      return base.BindSetIndex(binder, indexes, value);
    bool[] isByRef = ((IList<bool>) ComBinderHelpers.ProcessArgumentsForCom(ref indexes)).AddLast<bool>(false);
    DynamicMetaObject dynamicMetaObject = this.BindComInvoke(method, ((IList<DynamicMetaObject>) indexes).AddLast<DynamicMetaObject>(value), binder.CallInfo, isByRef);
    return new DynamicMetaObject((Expression) Expression.Block(dynamicMetaObject.Expression, (Expression) Expression.Convert(value.Expression, typeof (object))), dynamicMetaObject.Restrictions);
  }

  [SecurityCritical]
  private DynamicMetaObject BindComInvoke(
    ComMethodDesc method,
    DynamicMetaObject[] indexes,
    CallInfo callInfo,
    bool[] isByRef)
  {
    Expression expression = Helpers.Convert(this.Expression, typeof (DispCallable));
    return new ComInvokeBinder(callInfo, indexes, isByRef, this.DispCallableRestrictions(), (Expression) Expression.Constant((object) method), (Expression) Expression.Property(expression, typeof (DispCallable).GetProperty("DispatchObject")), method).Invoke();
  }

  [SecurityCritical]
  private BindingRestrictions DispCallableRestrictions()
  {
    Expression expression1 = this.Expression;
    BindingRestrictions typeRestriction = BindingRestrictions.GetTypeRestriction(expression1, typeof (DispCallable));
    Expression expression2 = Helpers.Convert(expression1, typeof (DispCallable));
    MemberExpression expr = Expression.Property(expression2, typeof (DispCallable).GetProperty("DispatchComObject"));
    MemberExpression left = Expression.Property(expression2, typeof (DispCallable).GetProperty("DispId"));
    BindingRestrictions restrictions = IDispatchMetaObject.IDispatchRestriction((Expression) expr, this._callable.DispatchComObject.ComTypeDesc);
    BindingRestrictions expressionRestriction = BindingRestrictions.GetExpressionRestriction((Expression) Expression.Equal((Expression) left, (Expression) Expression.Constant((object) this._callable.DispId)));
    return typeRestriction.Merge(restrictions).Merge(expressionRestriction);
  }
}
