// Type: System.Dynamic.StringArgBuilder
using System.Linq.Expressions;
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

internal class StringArgBuilder : SimpleArgBuilder
{
  private readonly bool _isWrapper;

  internal StringArgBuilder(Type parameterType)
    : base(parameterType)
  {
    this._isWrapper = parameterType == typeof (BStrWrapper);
  }

  internal override Expression Marshal(Expression parameter)
  {
    parameter = base.Marshal(parameter);
    if (this._isWrapper)
      parameter = (Expression) Expression.Property(Helpers.Convert(parameter, typeof (BStrWrapper)), typeof (BStrWrapper).GetProperty("WrappedObject"));
    return parameter;
  }

  internal override Expression MarshalToRef(Expression parameter)
  {
    parameter = this.Marshal(parameter);
    return (Expression) Expression.Call(typeof (System.Runtime.InteropServices.Marshal).GetMethod("StringToBSTR"), parameter);
  }

  internal override Expression UnmarshalFromRef(Expression value)
  {
    Expression newValue = (Expression) Expression.Condition((Expression) Expression.Equal(value, (Expression) Expression.Constant((object) IntPtr.Zero)), (Expression) Expression.Constant((object) null, typeof (string)), (Expression) Expression.Call(typeof (System.Runtime.InteropServices.Marshal).GetMethod("PtrToStringBSTR"), value));
    if (this._isWrapper)
      newValue = (Expression) Expression.New(typeof (BStrWrapper).GetConstructor(new Type[1]
      {
        typeof (string)
      }), newValue);
    return base.UnmarshalFromRef(newValue);
  }
}
