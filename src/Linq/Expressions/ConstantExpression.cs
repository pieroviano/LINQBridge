using System.Text;

namespace System.Linq.Expressions;

/// <summary>Represents an expression that has a constant value.</summary>
public sealed class ConstantExpression : Expression
{
    private readonly object value;

    internal ConstantExpression(object value, Type type)
        : base(ExpressionType.Constant, type)
    {
        this.value = value;
    }

    /// <summary>Gets the value of the constant expression.</summary>
    /// <returns>An <see cref="T:System.Object" /> equal to the value of the represented expression.</returns>
    public object Value => value;

    internal override void BuildString(StringBuilder builder)
    {
        if (builder == null)
            throw Error.ArgumentNull(nameof(builder));
        if (value != null)
        {
            if (value is string)
            {
                builder.Append("\"");
                builder.Append(value);
                builder.Append("\"");
            }
            else if (value.ToString() == value.GetType().ToString())
            {
                builder.Append("value(");
                builder.Append(value);
                builder.Append(")");
            }
            else
                builder.Append(value);
        }
        else
            builder.Append("null");
    }
}