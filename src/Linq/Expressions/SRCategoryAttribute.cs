// Type: System.Dynamic.SRCategoryAttribute
using System.ComponentModel;

#nullable disable
namespace System.Linq.Expressions;

[AttributeUsage(AttributeTargets.All)]
internal sealed class SRCategoryAttribute(string category) : CategoryAttribute(category)
{
  protected override string GetLocalizedString(string value) => SR.GetString(value);
}
