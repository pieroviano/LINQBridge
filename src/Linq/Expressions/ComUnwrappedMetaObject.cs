// Type: System.Dynamic.ComUnwrappedMetaObject
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class ComUnwrappedMetaObject : DynamicMetaObject
{
  internal ComUnwrappedMetaObject(
    Expression expression,
    BindingRestrictions restrictions,
    object value)
    : base(expression, restrictions, value)
  {
  }
}
