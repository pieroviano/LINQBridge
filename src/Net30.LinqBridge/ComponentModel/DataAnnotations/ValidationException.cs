using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.ComponentModel.DataAnnotations;

[Serializable]
public class ValidationException : Exception
{
    public ValidationException(string errorMessage, ValidationAttribute validatingAttribute, object value) :
        base(errorMessage)
    {
        Value = value;
        ValidationAttribute = validatingAttribute;
    }

    public ValidationException()
    {
    }

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

#pragma warning disable CS3003
    public ValidationAttribute? ValidationAttribute { get; private set; }
#pragma warning restore CS3003

    public object? Value { get; private set; }

    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
    }
}