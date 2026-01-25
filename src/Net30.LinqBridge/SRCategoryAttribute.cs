using System.ComponentModel;
using System.Linq.Expressions;

namespace System;

[AttributeUsage(AttributeTargets.All)]
internal sealed class SRCategoryAttribute : CategoryAttribute
{
    public SRCategoryAttribute(string category) : base(category)
    {
    }

    protected override string GetLocalizedString(string value)
    {
        return Strings.GetString(value);
    }
}