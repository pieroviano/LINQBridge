// Type: System.Dynamic.SRDescriptionAttribute
using System.ComponentModel;

#nullable disable
namespace System.Linq.Expressions;

[AttributeUsage(AttributeTargets.All)]
internal sealed class SRDescriptionAttribute(string description) : DescriptionAttribute(description)
{
  private bool replaced;

  public override string Description
  {
    get
    {
      if (!this.replaced)
      {
        this.replaced = true;
        this.DescriptionValue = SR.GetString(base.Description);
      }
      return base.Description;
    }
  }
}
