// Type: System.Dynamic.NullArgBuilder
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class NullArgBuilder : ArgBuilder
{
  internal NullArgBuilder()
  {
  }

  internal override Expression Marshal(Expression parameter)
  {
    return (Expression) Expression.Constant((object) null);
  }
}
