// Type: System.Dynamic.ComTypeEnumDesc
using System.Globalization;
using System.Runtime.InteropServices.ComTypes;

#nullable disable
namespace System.Linq.Expressions;

internal sealed class ComTypeEnumDesc : ComTypeDesc
{
  public override string ToString()
  {
    return string.Format((IFormatProvider) CultureInfo.CurrentCulture, "<enum '{0}'>", new object[1]
    {
      (object) this.TypeName
    });
  }

  internal ComTypeEnumDesc(ITypeInfo typeInfo)
    : base(typeInfo)
  {
  }
}
