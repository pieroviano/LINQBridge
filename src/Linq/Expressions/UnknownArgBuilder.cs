// Type: System.Dynamic.UnknownArgBuilder
using System.Linq.Expressions;
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

internal class UnknownArgBuilder : SimpleArgBuilder
{
  private readonly bool _isWrapper;

  internal UnknownArgBuilder(Type parameterType)
    : base(parameterType)
  {
    this._isWrapper = parameterType == typeof (UnknownWrapper);
  }

  internal override Expression Marshal(Expression parameter)
  {
    parameter = base.Marshal(parameter);
    if (this._isWrapper)
      parameter = (Expression) Expression.Property(Helpers.Convert(parameter, typeof (UnknownWrapper)), typeof (UnknownWrapper).GetProperty("WrappedObject"));
    return Helpers.Convert(parameter, typeof (object));
  }

  internal override Expression MarshalToRef(Expression parameter)
  {
    parameter = this.Marshal(parameter);
    return (Expression) Expression.Condition((Expression) Expression.Equal(parameter, (Expression) Expression.Constant((object) null)), (Expression) Expression.Constant((object) IntPtr.Zero), (Expression) Expression.Call(typeof (System.Runtime.InteropServices.Marshal).GetMethod("GetIUnknownForObject"), parameter));
  }

  internal override Expression UnmarshalFromRef(Expression value)
  {
    Expression newValue = (Expression) Expression.Condition((Expression) Expression.Equal(value, (Expression) Expression.Constant((object) IntPtr.Zero)), (Expression) Expression.Constant((object) null), (Expression) Expression.Call(typeof (System.Runtime.InteropServices.Marshal).GetMethod("GetObjectForIUnknown"), value));
    if (this._isWrapper)
      newValue = (Expression) Expression.New(typeof (UnknownWrapper).GetConstructor(new Type[1]
      {
        typeof (object)
      }), newValue);
    return base.UnmarshalFromRef(newValue);
  }
}
