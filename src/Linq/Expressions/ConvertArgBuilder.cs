// Type: System.Dynamic.ConvertArgBuilder
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions;

internal class ConvertArgBuilder : SimpleArgBuilder
{
  private readonly Type _marshalType;

  internal ConvertArgBuilder(Type parameterType, Type marshalType)
    : base(parameterType)
  {
    this._marshalType = marshalType;
  }

  internal override Expression Marshal(Expression parameter)
  {
    parameter = base.Marshal(parameter);
    return (Expression) Expression.Convert(parameter, this._marshalType);
  }

  internal override Expression UnmarshalFromRef(Expression newValue)
  {
    return base.UnmarshalFromRef((Expression) Expression.Convert(newValue, this.ParameterType));
  }
}
