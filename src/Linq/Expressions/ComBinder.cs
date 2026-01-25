using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;

#nullable disable
namespace System.Linq.Expressions;

public class ComBinder
{
  public static bool IsComObject(object value) => ComObject.IsComObject(value);

  [SecuritySafeCritical]
  public static bool TryBindGetMember(
    GetMemberBinder binder,
    DynamicMetaObject instance,
    out DynamicMetaObject result,
    bool delayInvocation)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ContractUtils.RequiresNotNull((object) instance, nameof (instance));
    if (ComBinder.TryGetMetaObject(ref instance))
    {
      new PermissionSet(PermissionState.Unrestricted).Demand();
      ComBinder.ComGetMemberBinder binder1 = new ComBinder.ComGetMemberBinder(binder, delayInvocation);
      result = instance.BindGetMember((GetMemberBinder) binder1);
      if (result.Expression.Type.IsValueType)
        result = new DynamicMetaObject((Expression) Expression.Convert(result.Expression, typeof (object)), result.Restrictions);
      return true;
    }
    result = (DynamicMetaObject) null;
    return false;
  }

  public static bool TryBindGetMember(
    GetMemberBinder binder,
    DynamicMetaObject instance,
    out DynamicMetaObject result)
  {
    return ComBinder.TryBindGetMember(binder, instance, out result, false);
  }

  [SecuritySafeCritical]
  public static bool TryBindSetMember(
    SetMemberBinder binder,
    DynamicMetaObject instance,
    DynamicMetaObject value,
    out DynamicMetaObject result)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ContractUtils.RequiresNotNull((object) instance, nameof (instance));
    ContractUtils.RequiresNotNull((object) value, nameof (value));
    if (ComBinder.TryGetMetaObject(ref instance))
    {
      new PermissionSet(PermissionState.Unrestricted).Demand();
      result = instance.BindSetMember(binder, value);
      return true;
    }
    result = (DynamicMetaObject) null;
    return false;
  }

  [SecuritySafeCritical]
  public static bool TryBindInvoke(
    InvokeBinder binder,
    DynamicMetaObject instance,
    DynamicMetaObject[] args,
    out DynamicMetaObject result)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ContractUtils.RequiresNotNull((object) instance, nameof (instance));
    ContractUtils.RequiresNotNull((object) args, nameof (args));
    if (ComBinder.TryGetMetaObject(ref instance))
    {
      new PermissionSet(PermissionState.Unrestricted).Demand();
      result = instance.BindInvoke(binder, args);
      return true;
    }
    result = (DynamicMetaObject) null;
    return false;
  }

  [SecuritySafeCritical]
  public static bool TryBindInvokeMember(
    InvokeMemberBinder binder,
    DynamicMetaObject instance,
    DynamicMetaObject[] args,
    out DynamicMetaObject result)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ContractUtils.RequiresNotNull((object) instance, nameof (instance));
    ContractUtils.RequiresNotNull((object) args, nameof (args));
    if (ComBinder.TryGetMetaObject(ref instance))
    {
      new PermissionSet(PermissionState.Unrestricted).Demand();
      result = instance.BindInvokeMember(binder, args);
      return true;
    }
    result = (DynamicMetaObject) null;
    return false;
  }

  [SecuritySafeCritical]
  public static bool TryBindGetIndex(
    GetIndexBinder binder,
    DynamicMetaObject instance,
    DynamicMetaObject[] args,
    out DynamicMetaObject result)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ContractUtils.RequiresNotNull((object) instance, nameof (instance));
    ContractUtils.RequiresNotNull((object) args, nameof (args));
    if (ComBinder.TryGetMetaObject(ref instance))
    {
      new PermissionSet(PermissionState.Unrestricted).Demand();
      result = instance.BindGetIndex(binder, args);
      return true;
    }
    result = (DynamicMetaObject) null;
    return false;
  }

  [SecuritySafeCritical]
  public static bool TryBindSetIndex(
    SetIndexBinder binder,
    DynamicMetaObject instance,
    DynamicMetaObject[] args,
    DynamicMetaObject value,
    out DynamicMetaObject result)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ContractUtils.RequiresNotNull((object) instance, nameof (instance));
    ContractUtils.RequiresNotNull((object) args, nameof (args));
    ContractUtils.RequiresNotNull((object) value, nameof (value));
    if (ComBinder.TryGetMetaObject(ref instance))
    {
      new PermissionSet(PermissionState.Unrestricted).Demand();
      result = instance.BindSetIndex(binder, args, value);
      return true;
    }
    result = (DynamicMetaObject) null;
    return false;
  }

  [SecuritySafeCritical]
  public static bool TryConvert(
    ConvertBinder binder,
    DynamicMetaObject instance,
    out DynamicMetaObject result)
  {
    ContractUtils.RequiresNotNull((object) binder, nameof (binder));
    ContractUtils.RequiresNotNull((object) instance, nameof (instance));
    if (ComBinder.IsComObject(instance.Value))
    {
      new PermissionSet(PermissionState.Unrestricted).Demand();
      if (binder.Type.IsInterface)
      {
        result = new DynamicMetaObject((Expression) Expression.Convert(instance.Expression, binder.Type), BindingRestrictions.GetExpressionRestriction((Expression) Expression.Call(typeof (ComObject).GetMethod("IsComObject", BindingFlags.Static | BindingFlags.NonPublic), Helpers.Convert(instance.Expression, typeof (object)))));
        return true;
      }
    }
    result = (DynamicMetaObject) null;
    return false;
  }

  [SecuritySafeCritical]
  public static IEnumerable<string> GetDynamicMemberNames(object value)
  {
    ContractUtils.RequiresNotNull(value, nameof (value));
    ContractUtils.Requires(ComBinder.IsComObject(value), nameof (value), Strings.ComObjectExpected);
    new PermissionSet(PermissionState.Unrestricted).Demand();
    return (IEnumerable<string>) ComObject.ObjectToComObject(value).GetMemberNames(false);
  }

  [SecuritySafeCritical]
  public static IList<string> GetDynamicDataMemberNames(object value)
  {
    ContractUtils.RequiresNotNull(value, nameof (value));
    ContractUtils.Requires(ComBinder.IsComObject(value), nameof (value), Strings.ComObjectExpected);
    new PermissionSet(PermissionState.Unrestricted).Demand();
    return ComObject.ObjectToComObject(value).GetMemberNames(true);
  }

  [SecuritySafeCritical]
  public static IList<KeyValuePair<string, object>> GetDynamicDataMembers(
    object value,
    IEnumerable<string> names)
  {
    ContractUtils.RequiresNotNull(value, nameof (value));
    ContractUtils.Requires(ComBinder.IsComObject(value), nameof (value), Strings.ComObjectExpected);
    new PermissionSet(PermissionState.Unrestricted).Demand();
    return ComObject.ObjectToComObject(value).GetMembers(names);
  }

  private static bool TryGetMetaObject(ref DynamicMetaObject instance)
  {
    if (instance is ComUnwrappedMetaObject || !ComBinder.IsComObject(instance.Value))
      return false;
    instance = (DynamicMetaObject) new ComMetaObject(instance.Expression, instance.Restrictions, instance.Value);
    return true;
  }

  internal class ComGetMemberBinder : GetMemberBinder
  {
    private readonly GetMemberBinder _originalBinder;
    internal bool _CanReturnCallables;

    internal ComGetMemberBinder(GetMemberBinder originalBinder, bool CanReturnCallables)
      : base(originalBinder.Name, originalBinder.IgnoreCase)
    {
      this._originalBinder = originalBinder;
      this._CanReturnCallables = CanReturnCallables;
    }

    public override DynamicMetaObject FallbackGetMember(
      DynamicMetaObject target,
      DynamicMetaObject errorSuggestion)
    {
      return this._originalBinder.FallbackGetMember(target, errorSuggestion);
    }

    public override int GetHashCode()
    {
      return this._originalBinder.GetHashCode() ^ (this._CanReturnCallables ? 1 : 0);
    }

    public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
    {
        if (target == null) throw new System.ArgumentNullException(nameof(target));
        // Minimal COM fallback: if COM runtime-specific binding cannot be produced here,
        // produce a DynamicMetaObject that throws NotSupportedException at runtime.
        var ctor = typeof(NotSupportedException).GetConstructor(new Type[] { typeof(string) });
        Expression throwExpr = Expression.Throw(
            Expression.New(ctor, Expression.Constant((object)Strings.CannotCall)),
            typeof(object)
        );

        BindingRestrictions restrictions = target.Restrictions;
        if (args != null && args.Length > 0)
        {
            restrictions = restrictions.Merge(BindingRestrictions.Combine((IList<DynamicMetaObject>) args));
        }

        return new DynamicMetaObject(throwExpr, restrictions);
    }

    public override Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel)
    {
        if (args == null) throw new System.ArgumentNullException(nameof(args));
        if (parameters == null) throw new System.ArgumentNullException(nameof(parameters));
        if (returnLabel == null) throw new System.ArgumentNullException(nameof(returnLabel));

        // Minimal runtime behavior for COM get-member when no full COM binding generation is available:
        // produce an expression that throws NotSupportedException with message Strings.CannotCall.
        var ctor = typeof(NotSupportedException).GetConstructor(new Type[] { typeof(string) });
        Expression throwExpr = Expression.Throw(
            Expression.New(ctor, Expression.Constant((object)Strings.CannotCall)),
            typeof(object) // the dynamic site's expected return type is object
        );

        return Expression.Return(returnLabel, throwExpr);
    }

    public override bool Equals(object obj)
    {
      return obj is ComBinder.ComGetMemberBinder comGetMemberBinder && this._CanReturnCallables == comGetMemberBinder._CanReturnCallables && this._originalBinder.Equals((object) comGetMemberBinder._originalBinder);
    }
  }
}
