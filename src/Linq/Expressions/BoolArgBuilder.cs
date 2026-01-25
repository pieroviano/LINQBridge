// Type: System.Dynamic.BoolArgBuilder
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class BoolArgBuilder : SimpleArgBuilder
{
  internal BoolArgBuilder(Type parameterType)
    : base(parameterType)
  {
  }

  internal override Expression MarshalToRef(Expression parameter)
  {
    return (Expression) Expression.Condition(this.Marshal(parameter), (Expression) Expression.Constant((object) (short) -1), (Expression) Expression.Constant((object) (short) 0));
  }

  internal override Expression UnmarshalFromRef(Expression value)
  {
    return base.UnmarshalFromRef((Expression) Expression.NotEqual(value, (Expression) Expression.Constant((object) (short) 0)));
  }
}
