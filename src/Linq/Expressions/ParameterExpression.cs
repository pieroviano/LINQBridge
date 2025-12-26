using System.Text;

namespace System.Linq.Expressions;

/// <summary>Represents a named parameter expression.</summary>
public sealed class ParameterExpression : Expression
{
    private readonly string name;

    internal ParameterExpression(Type type, string name)
        : base(ExpressionType.Parameter, type)
    {
        this.name = name;
    }

    /// <summary>Gets the name of the parameter.</summary>
    /// <returns>A <see cref="T:System.String" /> that contains the name of the parameter.</returns>
    public string Name => name;

    public bool IsByRef { get; set; }

    internal override void BuildString(StringBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (name != null)
            builder.Append(name);
        else
            builder.Append("<param>");
    }
}