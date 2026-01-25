// Type: System.Dynamic.SimpleArgBuilder
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions;

internal class SimpleArgBuilder : ArgBuilder
{
  private readonly Type _parameterType;

  internal SimpleArgBuilder(Type parameterType) => this._parameterType = parameterType;

  internal Type ParameterType => this._parameterType;

  internal override Expression Marshal(Expression parameter)
  {
    return Helpers.Convert(parameter, this._parameterType);
  }

  internal override Expression UnmarshalFromRef(Expression newValue)
  {
    return base.UnmarshalFromRef(newValue);
  }
}
