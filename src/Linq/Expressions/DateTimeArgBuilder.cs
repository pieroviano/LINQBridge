// Type: System.Dynamic.DateTimeArgBuilder
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class DateTimeArgBuilder : SimpleArgBuilder
{
  internal DateTimeArgBuilder(Type parameterType)
    : base(parameterType)
  {
  }

  internal override Expression MarshalToRef(Expression parameter)
  {
    return (Expression) Expression.Call(this.Marshal(parameter), typeof (DateTime).GetMethod("ToOADate"));
  }

  internal override Expression UnmarshalFromRef(Expression value)
  {
    return base.UnmarshalFromRef((Expression) Expression.Call(typeof (DateTime).GetMethod("FromOADate"), value));
  }
}
