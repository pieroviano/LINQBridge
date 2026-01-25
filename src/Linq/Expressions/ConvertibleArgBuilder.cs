// Type: System.Dynamic.ConvertibleArgBuilder
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions;

internal class ConvertibleArgBuilder : ArgBuilder
{
  internal ConvertibleArgBuilder()
  {
  }

  internal override Expression Marshal(Expression parameter)
  {
    return Helpers.Convert(parameter, typeof (IConvertible));
  }

  internal override Expression MarshalToRef(Expression parameter) => throw Assert.Unreachable;
}
