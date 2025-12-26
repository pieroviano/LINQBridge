using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace System.Linq.Expressions;

/// <summary>Represents a constructor call.</summary>
public sealed class NewExpression : Expression
{
    private readonly ConstructorInfo constructor;
    private readonly ReadOnlyCollection<Expression> arguments;
    private readonly ReadOnlyCollection<MemberInfo> members;

    internal NewExpression(
        Type type,
        ConstructorInfo constructor,
        ReadOnlyCollection<Expression> arguments)
        : base(ExpressionType.New, type)
    {
        this.constructor = constructor;
        this.arguments = arguments;
    }

    internal NewExpression(
        Type type,
        ConstructorInfo constructor,
        ReadOnlyCollection<Expression> arguments,
        ReadOnlyCollection<MemberInfo> members)
        : base(ExpressionType.New, type)
    {
        this.constructor = constructor;
        this.arguments = arguments;
        this.members = members;
    }

    /// <summary>Gets the called constructor.</summary>
    /// <returns>The <see cref="T:System.Reflection.ConstructorInfo" /> that represents the called constructor.</returns>
    public ConstructorInfo Constructor => constructor;

    /// <summary>Gets the arguments to the constructor.</summary>
    /// <returns>A collection of <see cref="T:System.Linq.Expressions.Expression" /> objects that represent the arguments to the constructor.</returns>
    public ReadOnlyCollection<Expression> Arguments => arguments;

    /// <summary>Gets the members that can retrieve the values of the fields that were initialized with constructor arguments.</summary>
    /// <returns>A collection of <see cref="T:System.Reflection.MemberInfo" /> objects that represent the members that can retrieve the values of the fields that were initialized with constructor arguments.</returns>
    public ReadOnlyCollection<MemberInfo> Members => members;

    private static PropertyInfo GetPropertyNoThrow(MethodInfo method)
    {
        if (method == null)
            return null;
        foreach (var property in method.DeclaringType.GetProperties((BindingFlags)(48 | (method.IsStatic ? 8 : 4))))
        {
            if (property.CanRead && method == property.GetGetMethod(true) || property.CanWrite && method == property.GetSetMethod(true))
                return property;
        }
        return null;
    }

    internal override void BuildString(StringBuilder builder)
    {
        Type type1;
        if (constructor != null)
        {
            type1 = constructor.DeclaringType;
        }
        else
        {
            var type2 = type1 = Type;
        }
        var type3 = type1;
        builder.Append("new ");
        var count = arguments.Count;
        builder.Append(type3.Name);
        builder.Append("(");
        if (count > 0)
        {
            for (var index = 0; index < count; ++index)
            {
                if (index > 0)
                    builder.Append(", ");
                if (members != null)
                {
                    PropertyInfo propertyNoThrow;
                    if (members[index].MemberType == MemberTypes.Method && (propertyNoThrow = GetPropertyNoThrow((MethodInfo)members[index])) != null)
                        builder.Append(propertyNoThrow.Name);
                    else
                        builder.Append(members[index].Name);
                    builder.Append(" = ");
                }
                arguments[index].BuildString(builder);
            }
        }
        builder.Append(")");
    }
}