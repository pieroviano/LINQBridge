#nullable disable
using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions;

/// <summary>Represents an operation between an expression and a type.</summary>
[DebuggerTypeProxy(typeof(TypeBinaryExpressionProxy))]
public sealed class TypeBinaryExpression : Expression
{
    internal TypeBinaryExpression(Expression expression, Type typeOperand, ExpressionType nodeKind)
    {
        Expression = expression;
        TypeOperand = typeOperand;
        NodeType = nodeKind;
    }

    /// <summary>
    ///     Gets the static type of the expression that this
    ///     <see cref="P:System.Linq.Expressions.TypeBinaryExpression.Expression" /> represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.TypeBinaryExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type => typeof(bool);

    /// <summary>
    ///     Returns the node type of this Expression. Extension nodes should return
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.
    /// </summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> of the expression.</returns>
    public override ExpressionType NodeType { get; }

    /// <summary>Gets the expression operand of a type test operation.</summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> that represents the expression operand of a type test
    ///     operation.
    /// </returns>
    public Expression Expression { get; }

    /// <summary>Gets the type operand of a type test operation.</summary>
    /// <returns>A <see cref="T:System.Type" /> that represents the type operand of a type test operation.</returns>
    public Type TypeOperand { get; }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="expression">
    ///     The <see cref="P:System.Linq.Expressions.TypeBinaryExpression.Expression" /> property of the
    ///     result.
    /// </param>
    public TypeBinaryExpression Update(Expression expression)
    {
        if (expression == Expression)
        {
            return this;
        }

        return NodeType == ExpressionType.TypeIs ? TypeIs(expression, TypeOperand) : TypeEqual(expression, TypeOperand);
    }

    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitTypeBinary(this);
    }

    internal Expression ReduceTypeEqual()
    {
        var type = Expression.Type;
        if (type.IsValueType && !type.IsNullableType())
        {
            return Block(Expression, Constant(type == TypeOperand.GetNonNullableType()));
        }

        if (Expression.NodeType == ExpressionType.Constant)
        {
            return ReduceConstantTypeEqual();
        }

        if (type.IsSealed && type == TypeOperand)
        {
            return type.IsNullableType()
                ? NotEqual(Expression, Constant(null, Expression.Type))
                : (Expression)ReferenceNotEqual(Expression, Constant(null, Expression.Type));
        }

        if (Expression is ParameterExpression expression1 && !expression1.IsByRef)
        {
            return ByValParameterTypeEqual(expression1);
        }

        var left = Parameter(typeof(object));
        var expression2 = Expression;
        if (!TypeUtils.AreReferenceAssignable(typeof(object), expression2.Type))
        {
            expression2 = Convert(expression2, typeof(object));
        }

        return Block(new ParameterExpression[1]
        {
            left
        }, Assign(left, expression2), ByValParameterTypeEqual(left));
    }

    private Expression ByValParameterTypeEqual(ParameterExpression value)
    {
        var expression = (Expression)Call(value, typeof(object).GetMethod("GetType"));
        if (TypeOperand.IsInterface)
        {
            var left = Parameter(typeof(Type));
            expression = Block(new ParameterExpression[1]
            {
                left
            }, Assign(left, expression), left);
        }

        return AndAlso(ReferenceNotEqual(value, Constant(null)),
            ReferenceEqual(expression, Constant(TypeOperand.GetNonNullableType(), typeof(Type))));
    }

    private Expression ReduceConstantTypeEqual()
    {
        var expression = Expression as ConstantExpression;
        return expression.Value == null
            ? Constant(false)
            : (Expression)Constant(TypeOperand.GetNonNullableType() == expression.Value.GetType());
    }
}