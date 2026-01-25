// Type: System.Dynamic.ArgBuilder
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions;

internal abstract class ArgBuilder
{
  internal abstract Expression Marshal(Expression parameter);

  internal virtual Expression MarshalToRef(Expression parameter) => this.Marshal(parameter);

  internal virtual Expression UnmarshalFromRef(Expression newValue) => newValue;
}
