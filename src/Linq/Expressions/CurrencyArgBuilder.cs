// Type: System.Dynamic.CurrencyArgBuilder
using System.Linq.Expressions;
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class CurrencyArgBuilder : SimpleArgBuilder
{
  internal CurrencyArgBuilder(Type parameterType)
    : base(parameterType)
  {
  }

  internal override Expression Marshal(Expression parameter)
  {
    return (Expression) Expression.Property(Helpers.Convert(base.Marshal(parameter), typeof (CurrencyWrapper)), "WrappedObject");
  }

  internal override Expression MarshalToRef(Expression parameter)
  {
    return (Expression) Expression.Call(typeof (Decimal).GetMethod("ToOACurrency"), this.Marshal(parameter));
  }

  internal override Expression UnmarshalFromRef(Expression value)
  {
    return base.UnmarshalFromRef((Expression) Expression.New(typeof (CurrencyWrapper).GetConstructor(new Type[1]
    {
      typeof (Decimal)
    }), (Expression) Expression.Call(typeof (Decimal).GetMethod("FromOACurrency"), value)));
  }
}
