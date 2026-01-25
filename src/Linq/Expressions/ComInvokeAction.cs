// Type: System.Dynamic.ComInvokeAction
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class ComInvokeAction : InvokeBinder
{
  internal ComInvokeAction(CallInfo callInfo)
    : base(callInfo)
  {
  }

  public override int GetHashCode() => base.GetHashCode();

  public override Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel)
  {
      if (args == null) throw new ArgumentNullException(nameof(args));
      if (parameters == null) throw new ArgumentNullException(nameof(parameters));
      if (returnLabel == null) throw new ArgumentNullException(nameof(returnLabel));

      // Minimal fallback binding for COM invoke sites when no runtime COM infrastructure is available.
      // Produce code that throws NotSupportedException("CannotCall") as the dynamic site result.
      var ctor = typeof(NotSupportedException).GetConstructor(new Type[] { typeof(string) });
      Expression throwExpr = Expression.Throw(
          Expression.New(ctor, Expression.Constant((object)Strings.CannotCall)),
          typeof(object) // the dynamic site's expected return type is object
      );

      // returnLabel is used by the call-site generation to return the computed value.
      return Expression.Return(returnLabel, throwExpr);
  }

  public override bool Equals(object obj) => base.Equals((object) (obj as ComInvokeAction));

  public override DynamicMetaObject FallbackInvoke(
    DynamicMetaObject target,
    DynamicMetaObject[] args,
    DynamicMetaObject errorSuggestion)
  {
    DynamicMetaObject dynamicMetaObject = errorSuggestion;
    if (dynamicMetaObject != null)
      return dynamicMetaObject;
    return new DynamicMetaObject((Expression) Expression.Throw((Expression) Expression.New(typeof (NotSupportedException).GetConstructor(new Type[1]
    {
      typeof (string)
    }), (Expression) Expression.Constant((object) Strings.CannotCall))), target.Restrictions.Merge(BindingRestrictions.Combine((IList<DynamicMetaObject>) args)));
  }
}
