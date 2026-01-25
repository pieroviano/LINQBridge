// Type: System.Dynamic.DispatchArgBuilder
using System.Linq.Expressions;
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

internal class DispatchArgBuilder : SimpleArgBuilder
{
  private readonly bool _isWrapper;

  internal DispatchArgBuilder(Type parameterType)
    : base(parameterType)
  {
    this._isWrapper = parameterType == typeof (DispatchWrapper);
  }

  internal override Expression Marshal(Expression parameter)
  {
    parameter = base.Marshal(parameter);
    if (this._isWrapper)
      parameter = (Expression) Expression.Property(Helpers.Convert(parameter, typeof (DispatchWrapper)), typeof (DispatchWrapper).GetProperty("WrappedObject"));
    return Helpers.Convert(parameter, typeof (object));
  }

  internal override Expression MarshalToRef(Expression parameter)
  {
    parameter = this.Marshal(parameter);
    return (Expression) Expression.Condition((Expression) Expression.Equal(parameter, (Expression) Expression.Constant((object) null)), (Expression) Expression.Constant((object) IntPtr.Zero), (Expression) Expression.Call(typeof (System.Runtime.InteropServices.Marshal).GetMethod("GetIDispatchForObject"), parameter));
  }

  internal override Expression UnmarshalFromRef(Expression value)
  {
    Expression newValue = (Expression) Expression.Condition((Expression) Expression.Equal(value, (Expression) Expression.Constant((object) IntPtr.Zero)), (Expression) Expression.Constant((object) null), (Expression) Expression.Call(typeof (System.Runtime.InteropServices.Marshal).GetMethod("GetObjectForIUnknown"), value));
    if (this._isWrapper)
      newValue = (Expression) Expression.New(typeof (DispatchWrapper).GetConstructor(new Type[1]
      {
        typeof (object)
      }), newValue);
    return base.UnmarshalFromRef(newValue);
  }
}
