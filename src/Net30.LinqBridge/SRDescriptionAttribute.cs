using System.ComponentModel;
using System.Linq.Expressions;

namespace System;

[AttributeUsage(AttributeTargets.All)]
internal sealed class SRDescriptionAttribute : DescriptionAttribute
{
    private bool replaced;

    public SRDescriptionAttribute(string description) : base(description)
    {
    }

    public override string Description
    {
        get
        {
            if (!replaced)
            {
                replaced = true;
                DescriptionValue = Strings.GetString(base.Description);
            }

            return base.Description;
        }
    }
}