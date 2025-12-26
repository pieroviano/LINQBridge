using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Linq.Expressions;

/// <summary>Represents calling a method.</summary>
public sealed class MethodCallExpression : Expression
{
    private readonly MethodInfo method;
    private readonly Expression obj;
    private readonly ReadOnlyCollection<Expression> arguments;

    internal MethodCallExpression(
        ExpressionType type,
        MethodInfo method,
        Expression obj,
        ReadOnlyCollection<Expression> arguments)
        : base(type, method.ReturnType)
    {
        this.obj = obj;
        this.method = method;
        this.arguments = arguments;
    }

    /// <summary>Gets the called method.</summary>
    /// <returns>The <see cref="T:System.Reflection.MethodInfo" /> that represents the called method.</returns>
    public MethodInfo Method => method;

    /// <summary>Gets the receiving object of the method.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression" /> that represents the receiving object of the method.</returns>
    public Expression Object => obj;

    /// <summary>Gets the arguments to the called method.</summary>
    /// <returns>A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of <see cref="T:System.Linq.Expressions.Expression" /> objects which represent the arguments to the called method.</returns>
    public ReadOnlyCollection<Expression> Arguments => arguments;

    internal override void BuildString(StringBuilder builder)
    {
        if (builder == null)
            throw Error.ArgumentNull(nameof(builder));
        var num = 0;
        var expression = obj;
        if (Attribute.GetCustomAttribute(method, typeof(ExtensionAttribute)) != null)
        {
            num = 1;
            expression = arguments[0];
        }
        if (expression != null)
        {
            expression.BuildString(builder);
            builder.Append(".");
        }
        builder.Append(method.Name);
        builder.Append("(");
        var index = num;
        for (var count = arguments.Count; index < count; ++index)
        {
            if (index > num)
                builder.Append(", ");
            arguments[index].BuildString(builder);
        }
        builder.Append(")");
    }
}