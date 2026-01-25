// Type: System.Dynamic.ConversionArgBuilder
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions;

internal class ConversionArgBuilder : ArgBuilder
{
  private SimpleArgBuilder _innerBuilder;
  private Type _parameterType;

  internal ConversionArgBuilder(Type parameterType, SimpleArgBuilder innerBuilder)
  {
    this._parameterType = parameterType;
    this._innerBuilder = innerBuilder;
  }

  internal override Expression Marshal(Expression parameter)
  {
    return this._innerBuilder.Marshal(Helpers.Convert(parameter, this._parameterType));
  }

  internal override Expression MarshalToRef(Expression parameter) => throw Assert.Unreachable;
}
