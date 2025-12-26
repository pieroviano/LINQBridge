using System.Collections.ObjectModel;
using System.Text;

namespace System.Linq.Expressions;

/// <summary>Represents a constructor call that has a collection initializer.</summary>
public sealed class ListInitExpression : Expression
{
    private readonly NewExpression newExpression;
    private readonly ReadOnlyCollection<ElementInit> initializers;

    internal ListInitExpression(
        NewExpression newExpression,
        ReadOnlyCollection<ElementInit> initializers)
        : base(ExpressionType.ListInit, newExpression.Type)
    {
        this.newExpression = newExpression;
        this.initializers = initializers;
    }

    /// <summary>Gets the expression that contains a call to the constructor of a collection type.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewExpression" /> that represents the call to the constructor of a collection type.</returns>
    public NewExpression NewExpression => newExpression;

    /// <summary>Gets the element initializers that are used to initialize a collection.</summary>
    /// <returns>A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of <see cref="T:System.Linq.Expressions.ElementInit" /> objects which represent the elements that are used to initialize the collection.</returns>
    public ReadOnlyCollection<ElementInit> Initializers => initializers;

    internal override void BuildString(StringBuilder builder)
    {
        newExpression.BuildString(builder);
        builder.Append(" {");
        var index = 0;
        for (var count = initializers.Count; index < count; ++index)
        {
            if (index > 0)
                builder.Append(", ");
            initializers[index].BuildString(builder);
        }
        builder.Append("}");
    }
}