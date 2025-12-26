using System.Reflection;
using System.Text;

namespace System.Linq.Expressions;

/// <summary>Represents an expression that has a unary operator.</summary>
public sealed class UnaryExpression : Expression
{
    private readonly Expression operand;
    private readonly MethodInfo method;

    internal UnaryExpression(ExpressionType nt, Expression operand, Type type)
        : this(nt, operand, null, type)
    {
    }

    internal UnaryExpression(ExpressionType nt, Expression operand, MethodInfo method, Type type)
        : base(nt, type)
    {
        this.operand = operand;
        this.method = method;
    }

    /// <summary>Gets the operand of the unary operation.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression" /> that represents the operand of the unary operation.</returns>
    public Expression Operand => operand;

    /// <summary>Gets the implementing method for the unary operation.</summary>
    /// <returns>The <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</returns>
    public MethodInfo Method => method;

    /// <summary>Gets a value that indicates whether the expression tree node represents a lifted call to an operator.</summary>
    /// <returns>true if the node represents a lifted call; otherwise, false.</returns>
    public bool IsLifted
    {
        get
        {
            if (NodeType == ExpressionType.TypeAs || NodeType == ExpressionType.Quote)
                return false;
            var flag1 = IsNullableType(operand.Type);
            var flag2 = IsNullableType(Type);
            if (method != null)
            {
                if (flag1 && method.GetParameters()[0].ParameterType != operand.Type)
                    return true;
                return flag2 && method.ReturnType != Type;
            }
            return flag1 || flag2;
        }
    }

    /// <summary>Gets a value that indicates whether the expression tree node represents a lifted call to an operator whose return type is lifted to a nullable type.</summary>
    /// <returns>true if the operator's return type is lifted to a nullable type; otherwise, false.</returns>
    public bool IsLiftedToNull => IsLifted && IsNullableType(Type);

    internal override void BuildString(StringBuilder builder)
    {
        if (builder == null)
            throw Error.ArgumentNull(nameof(builder));
        switch (NodeType)
        {
            case ExpressionType.Negate:
            case ExpressionType.NegateChecked:
                builder.Append("-");
                operand.BuildString(builder);
                break;
            case ExpressionType.UnaryPlus:
                builder.Append("+");
                operand.BuildString(builder);
                break;
            case ExpressionType.Not:
                builder.Append("Not");
                builder.Append("(");
                operand.BuildString(builder);
                builder.Append(")");
                break;
            case ExpressionType.Quote:
                operand.BuildString(builder);
                break;
            case ExpressionType.TypeAs:
                builder.Append("(");
                operand.BuildString(builder);
                builder.Append(" As ");
                builder.Append(Type.Name);
                builder.Append(")");
                break;
            default:
                builder.Append(NodeType);
                builder.Append("(");
                operand.BuildString(builder);
                builder.Append(")");
                break;
        }
    }
}