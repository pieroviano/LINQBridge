#nullable disable
namespace System.Linq.Expressions;

/// <summary>Used to represent the target of a <see cref="T:System.Linq.Expressions.GotoExpression" />.</summary>
public sealed class LabelTarget
{
    internal LabelTarget(Type type, string name)
    {
        Type = type;
        Name = name;
    }

    /// <summary>Gets the name of the label.</summary>
    /// <returns>The name of the label.</returns>
    public string Name { get; }

    /// <summary>
    ///     The type of value that is passed when jumping to the label (or <see cref="T:System.Void" /> if no value should
    ///     be passed).
    /// </summary>
    /// <returns>
    ///     The <see cref="T:System.Type" /> object representing the type of the value that is passed when jumping to the
    ///     label or <see cref="T:System.Void" /> if no value should be passed
    /// </returns>
    public Type Type { get; }

    /// <summary>Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.</summary>
    /// <returns>A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.</returns>
    public override string ToString()
    {
        return !string.IsNullOrEmpty(Name) ? Name : "UnamedLabel";
    }
}