using System.Collections.ObjectModel;
using System.Text;

namespace System.Linq.Expressions;

/// <summary>Represents calling a constructor and initializing one or more members of the new object.</summary>
public sealed class MemberInitExpression : Expression
{
    private readonly NewExpression newExpression;
    private readonly ReadOnlyCollection<MemberBinding> bindings;

    internal MemberInitExpression(
        NewExpression newExpression,
        ReadOnlyCollection<MemberBinding> bindings)
        : base(ExpressionType.MemberInit, newExpression.Type)
    {
        this.newExpression = newExpression;
        this.bindings = bindings;
    }

    /// <summary>Gets the expression that represents the constructor call.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewExpression" /> that represents the constructor call.</returns>
    public NewExpression NewExpression => newExpression;

    /// <summary>Gets the bindings that describe how to initialize the members of the newly created object.</summary>
    /// <returns>A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of <see cref="T:System.Linq.Expressions.MemberBinding" /> objects which describe how to initialize the members.</returns>
    public ReadOnlyCollection<MemberBinding> Bindings => bindings;

    internal override void BuildString(StringBuilder builder)
    {
        if (newExpression.Arguments.Count == 0 && newExpression.Type.Name.Contains("<"))
            builder.Append("new");
        else
            newExpression.BuildString(builder);
        builder.Append(" {");
        var index = 0;
        for (var count = bindings.Count; index < count; ++index)
        {
            var binding = bindings[index];
            if (index > 0)
                builder.Append(", ");
            binding.BuildString(builder);
        }
        builder.Append("}");
    }
}