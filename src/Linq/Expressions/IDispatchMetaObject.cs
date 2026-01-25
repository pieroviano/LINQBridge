// Type: System.Dynamic.IDispatchMetaObject
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class IDispatchMetaObject : ComFallbackMetaObject
{
  private readonly IDispatchComObject _self;

  internal IDispatchMetaObject(Expression expression, IDispatchComObject self)
    : base(expression, BindingRestrictions.Empty, (object) self)
  {
    this._self = self;
  }

  public override DynamicMetaObject BindInvokeMember(
    InvokeMemberBinder binder,
    DynamicMetaObject[] args)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ComMethodDesc method;
    if (!this._self.TryGetMemberMethod(binder.Name, out method) && !this._self.TryGetMemberMethodExplicit(binder.Name, out method))
      return base.BindInvokeMember(binder, args);
    bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(ref args);
    return this.BindComInvoke(args, method, binder.CallInfo, isByRef);
  }

  public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ComMethodDesc method;
    if (!this._self.TryGetGetItem(out method))
      return base.BindInvoke(binder, args);
    bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(ref args);
    return this.BindComInvoke(args, method, binder.CallInfo, isByRef);
  }

  private DynamicMetaObject BindComInvoke(
    DynamicMetaObject[] args,
    ComMethodDesc method,
    CallInfo callInfo,
    bool[] isByRef)
  {
    return new ComInvokeBinder(callInfo, args, isByRef, this.IDispatchRestriction(), (Expression) Expression.Constant((object) method), (Expression) Expression.Property(Helpers.Convert(this.Expression, typeof (IDispatchComObject)), typeof (IDispatchComObject).GetProperty("DispatchObject")), method).Invoke();
  }

  public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
  {
    bool canReturnCallables = binder is ComBinder.ComGetMemberBinder comGetMemberBinder && comGetMemberBinder._CanReturnCallables;
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ComMethodDesc method;
    if (this._self.TryGetMemberMethod(binder.Name, out method))
      return this.BindGetMember(method, canReturnCallables);
    ComEventDesc @event;
    if (this._self.TryGetMemberEvent(binder.Name, out @event))
      return this.BindEvent(@event);
    return this._self.TryGetMemberMethodExplicit(binder.Name, out method) ? this.BindGetMember(method, canReturnCallables) : base.BindGetMember(binder);
  }

  private DynamicMetaObject BindGetMember(ComMethodDesc method, bool canReturnCallables)
  {
    if (method.IsDataMember && method.ParamCount == 0)
      return this.BindComInvoke(DynamicMetaObject.EmptyMetaObjects, method, new CallInfo(0, new string[0]), new bool[0]);
    return !canReturnCallables ? this.BindComInvoke(DynamicMetaObject.EmptyMetaObjects, method, new CallInfo(0, new string[0]), new bool[0]) : new DynamicMetaObject((Expression) Expression.Call(typeof (ComRuntimeHelpers).GetMethod("CreateDispCallable"), Helpers.Convert(this.Expression, typeof (IDispatchComObject)), (Expression) Expression.Constant((object) method)), this.IDispatchRestriction());
  }

  private DynamicMetaObject BindEvent(ComEventDesc @event)
  {
    return new DynamicMetaObject((Expression) Expression.Call(typeof (ComRuntimeHelpers).GetMethod("CreateComEvent"), (Expression) ComObject.RcwFromComObject(this.Expression), (Expression) Expression.Constant((object) @event.sourceIID), (Expression) Expression.Constant((object) @event.dispid)), this.IDispatchRestriction());
  }

  public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ComMethodDesc method;
    if (!this._self.TryGetGetItem(out method))
      return base.BindGetIndex(binder, indexes);
    bool[] isByRef = ComBinderHelpers.ProcessArgumentsForCom(ref indexes);
    return this.BindComInvoke(indexes, method, binder.CallInfo, isByRef);
  }

  public override DynamicMetaObject BindSetIndex(
    SetIndexBinder binder,
    DynamicMetaObject[] indexes,
    DynamicMetaObject value)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ComMethodDesc method;
    if (!this._self.TryGetSetItem(out method))
      return base.BindSetIndex(binder, indexes, value);
    bool[] isByRef = ((IList<bool>) ComBinderHelpers.ProcessArgumentsForCom(ref indexes)).AddLast<bool>(false);
    DynamicMetaObject dynamicMetaObject = this.BindComInvoke(((IList<DynamicMetaObject>) indexes).AddLast<DynamicMetaObject>(value), method, binder.CallInfo, isByRef);
    return new DynamicMetaObject((Expression) Expression.Block(dynamicMetaObject.Expression, (Expression) Expression.Convert(value.Expression, typeof (object))), dynamicMetaObject.Restrictions);
  }

  public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    return this.TryPropertyPut(binder, value) ?? this.TryEventHandlerNoop(binder, value) ?? base.BindSetMember(binder, value);
  }

  private DynamicMetaObject TryPropertyPut(SetMemberBinder binder, DynamicMetaObject value)
  {
    bool holdsNull = value.Value == null && value.HasValue;
    ComMethodDesc method;
    if (!this._self.TryGetPropertySetter(binder.Name, out method, value.LimitType, holdsNull) && !this._self.TryGetPropertySetterExplicit(binder.Name, out method, value.LimitType, holdsNull))
      return (DynamicMetaObject) null;
    BindingRestrictions restrictions = this.IDispatchRestriction();
    Expression dispatch = (Expression) Expression.Property(Helpers.Convert(this.Expression, typeof (IDispatchComObject)), typeof (IDispatchComObject).GetProperty("DispatchObject"));
    DynamicMetaObject dynamicMetaObject = new ComInvokeBinder(new CallInfo(1, new string[0]), new DynamicMetaObject[1]
    {
      value
    }, new bool[1], restrictions, (Expression) Expression.Constant((object) method), dispatch, method).Invoke();
    return new DynamicMetaObject((Expression) Expression.Block(dynamicMetaObject.Expression, (Expression) Expression.Convert(value.Expression, typeof (object))), dynamicMetaObject.Restrictions);
  }

  private DynamicMetaObject TryEventHandlerNoop(SetMemberBinder binder, DynamicMetaObject value)
  {
    return this._self.TryGetMemberEvent(binder.Name, out ComEventDesc _) && value.LimitType == typeof (BoundDispEvent) ? new DynamicMetaObject((Expression) Expression.Constant((object) null), value.Restrictions.Merge(this.IDispatchRestriction()).Merge(BindingRestrictions.GetTypeRestriction(value.Expression, typeof (BoundDispEvent)))) : (DynamicMetaObject) null;
  }

  private BindingRestrictions IDispatchRestriction()
  {
    return IDispatchMetaObject.IDispatchRestriction(this.Expression, this._self.ComTypeDesc);
  }

  internal static BindingRestrictions IDispatchRestriction(Expression expr, ComTypeDesc typeDesc)
  {
    return BindingRestrictions.GetTypeRestriction(expr, typeof (IDispatchComObject)).Merge(BindingRestrictions.GetExpressionRestriction((Expression) Expression.Equal((Expression) Expression.Property(Helpers.Convert(expr, typeof (IDispatchComObject)), typeof (IDispatchComObject).GetProperty("ComTypeDesc")), (Expression) Expression.Constant((object) typeDesc))));
  }

  protected override ComUnwrappedMetaObject UnwrapSelf()
  {
    return new ComUnwrappedMetaObject((Expression) ComObject.RcwFromComObject(this.Expression), this.IDispatchRestriction(), this._self.RuntimeCallableWrapper);
  }
}
