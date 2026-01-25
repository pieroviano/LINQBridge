using System.Properties;

namespace System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
#pragma warning disable CS3009
public class RequiredAttribute : ValidationAttribute
#pragma warning restore CS3009
{
    public RequiredAttribute() : base(() => DataAnnotationsResources.RequiredAttribute_ValidationError)
    {
    }

    public override bool IsValid(object value)
    {
        if (value == null)
        {
            return false;
        }

        var str = value as string;
        if (str == null)
        {
            return true;
        }

        return str.Trim().Length != 0;
    }
}