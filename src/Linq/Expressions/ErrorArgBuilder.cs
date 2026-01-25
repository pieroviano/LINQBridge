// Type: System.Dynamic.ErrorArgBuilder
using System.Linq.Expressions;
using System.Runtime.InteropServices;

#nullable disable
namespace System.Linq.Expressions;

internal class ErrorArgBuilder : SimpleArgBuilder
{
  internal ErrorArgBuilder(Type parameterType)
    : base(parameterType)
  {
  }

  internal override Expression Marshal(Expression parameter)
  {
    return (Expression) Expression.Property(Helpers.Convert(base.Marshal(parameter), typeof (ErrorWrapper)), "ErrorCode");
  }

  internal override Expression UnmarshalFromRef(Expression value)
  {
    return base.UnmarshalFromRef((Expression) Expression.New(typeof (ErrorWrapper).GetConstructor(new Type[1]
    {
      typeof (int)
    }), value));
  }
}
