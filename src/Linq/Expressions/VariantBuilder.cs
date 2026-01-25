// Type: System.Dynamic.VariantBuilder
using System.Linq.Expressions;
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

internal class VariantBuilder
{
  private MemberExpression _variant;
  private readonly ArgBuilder _argBuilder;
  private readonly VarEnum _targetComType;

  internal ParameterExpression TempVariable { get; private set; }

  internal VariantBuilder(VarEnum targetComType, ArgBuilder builder)
  {
    this._targetComType = targetComType;
    this._argBuilder = builder;
  }

  internal bool IsByRef => (this._targetComType & VarEnum.VT_BYREF) != 0;

  internal Expression InitializeArgumentVariant(MemberExpression variant, Expression parameter)
  {
    this._variant = variant;
    if (this.IsByRef)
    {
      Expression right = this._argBuilder.MarshalToRef(parameter);
      this.TempVariable = Expression.Variable(right.Type, (string) null);
      return (Expression) Expression.Block((Expression) Expression.Assign((Expression) this.TempVariable, right), (Expression) Expression.Call((Expression) variant, Variant.GetByrefSetter(this._targetComType & ~VarEnum.VT_BYREF), (Expression) this.TempVariable));
    }
    Expression right1 = this._argBuilder.Marshal(parameter);
    if (this._argBuilder is ConvertibleArgBuilder)
      return (Expression) Expression.Call((Expression) variant, typeof (Variant).GetMethod("SetAsIConvertible"), right1);
    if (Variant.IsPrimitiveType(this._targetComType) || this._targetComType == VarEnum.VT_DISPATCH || this._targetComType == VarEnum.VT_UNKNOWN || this._targetComType == VarEnum.VT_VARIANT || this._targetComType == VarEnum.VT_RECORD || this._targetComType == VarEnum.VT_ARRAY)
      return (Expression) Expression.Assign((Expression) Expression.Property((Expression) variant, Variant.GetAccessor(this._targetComType)), right1);
    switch (this._targetComType)
    {
      case VarEnum.VT_EMPTY:
        return (Expression) null;
      case VarEnum.VT_NULL:
        return (Expression) Expression.Call((Expression) variant, typeof (Variant).GetMethod("SetAsNull"));
      default:
        return (Expression) null;
    }
  }

  private static Expression Release(Expression pUnk)
  {
    return (Expression) Expression.Call(typeof (UnsafeMethods).GetMethod("IUnknownReleaseNotZero"), pUnk);
  }

  internal Expression Clear()
  {
    if (this.IsByRef)
    {
      if (this._argBuilder is StringArgBuilder)
        return (Expression) Expression.Call(typeof (Marshal).GetMethod("FreeBSTR"), (Expression) this.TempVariable);
      if (this._argBuilder is DispatchArgBuilder)
        return VariantBuilder.Release((Expression) this.TempVariable);
      if (this._argBuilder is UnknownArgBuilder)
        return VariantBuilder.Release((Expression) this.TempVariable);
      return this._argBuilder is VariantArgBuilder ? (Expression) Expression.Call((Expression) this.TempVariable, typeof (Variant).GetMethod(nameof (Clear))) : (Expression) null;
    }
    switch (this._targetComType)
    {
      case VarEnum.VT_EMPTY:
      case VarEnum.VT_NULL:
        return (Expression) null;
      case VarEnum.VT_BSTR:
      case VarEnum.VT_DISPATCH:
      case VarEnum.VT_VARIANT:
      case VarEnum.VT_UNKNOWN:
      case VarEnum.VT_RECORD:
      case VarEnum.VT_ARRAY:
        return (Expression) Expression.Call((Expression) this._variant, typeof (Variant).GetMethod(nameof (Clear)));
      default:
        return (Expression) null;
    }
  }

  internal Expression UpdateFromReturn(Expression parameter)
  {
    return this.TempVariable == null ? (Expression) null : (Expression) Expression.Assign(parameter, Helpers.Convert(this._argBuilder.UnmarshalFromRef((Expression) this.TempVariable), parameter.Type));
  }
}
