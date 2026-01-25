#nullable disable
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions;

/// <summary>Represents an expression that has a binary operator.</summary>
[DebuggerTypeProxy(typeof(BinaryExpressionProxy))]
public class BinaryExpression : Expression
{
    internal BinaryExpression(Expression left, Expression right)
    {
        Left = left;
        Right = right;
    }

    /// <summary>Gets a value that indicates whether the expression tree node can be reduced.</summary>
    /// <returns>True if the expression tree node can be reduced, otherwise false.</returns>
    public override bool CanReduce => IsOpAssignment(NodeType);

    /// <summary>Gets the right operand of the binary operation.</summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> that represents the right operand of the binary
    ///     operation.
    /// </returns>
    public Expression Right { get; }

    /// <summary>Gets the left operand of the binary operation.</summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> that represents the left operand of the binary
    ///     operation.
    /// </returns>
    public Expression Left { get; }

    /// <summary>Gets the implementing method for the binary operation.</summary>
    /// <returns>The <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</returns>
    public MethodInfo Method => GetMethod();

    /// <summary>Gets the type conversion function that is used by a coalescing or compound assignment operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that represents a type conversion function.</returns>
    public LambdaExpression Conversion => GetConversion();

    /// <summary>Gets a value that indicates whether the expression tree node represents a lifted call to an operator.</summary>
    /// <returns>true if the node represents a lifted call; otherwise, false.</returns>
    public bool IsLifted
    {
        get
        {
            if (NodeType == ExpressionType.Coalesce || NodeType == ExpressionType.Assign || !Left.Type.IsNullableType())
            {
                return false;
            }

            var method = GetMethod();
            return method == null ||
                   !TypeUtils.AreEquivalent(method.GetParameters()[0].ParameterType.GetNonRefType(), Left.Type);
        }
    }

    /// <summary>
    ///     Gets a value that indicates whether the expression tree node represents a lifted call to an operator whose
    ///     return type is lifted to a nullable type.
    /// </summary>
    /// <returns>true if the operator's return type is lifted to a nullable type; otherwise, false.</returns>
    public bool IsLiftedToNull => IsLifted && Type.IsNullableType();

    internal bool IsLiftedLogical
    {
        get
        {
            var type1 = Left.Type;
            var type2 = Right.Type;
            var method = GetMethod();
            switch (NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                    if (TypeUtils.AreEquivalent(type2, type1) && type1.IsNullableType() && method != null)
                    {
                        return TypeUtils.AreEquivalent(method.ReturnType, type1.GetNonNullableType());
                    }

                    break;
            }

            return false;
        }
    }

    internal bool IsReferenceComparison
    {
        get
        {
            var type1 = Left.Type;
            var type2 = Right.Type;
            var method = GetMethod();
            switch (NodeType)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    if (method == null && !type1.IsValueType)
                    {
                        return !type2.IsValueType;
                    }

                    break;
            }

            return false;
        }
    }

    /// <summary>Reduces the binary expression node to a simpler expression.</summary>
    /// <returns>The reduced expression.</returns>
    public override Expression Reduce()
    {
        if (!IsOpAssignment(NodeType))
        {
            return this;
        }

        switch (Left.NodeType)
        {
            case ExpressionType.MemberAccess:
                return ReduceMember();
            case ExpressionType.Index:
                return ReduceIndex();
            default:
                return ReduceVariable();
        }
    }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="left">The <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property of the result. </param>
    /// <param name="conversion">
    ///     The <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property of the
    ///     result.
    /// </param>
    /// <param name="right">The <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property of the result. </param>
    public BinaryExpression Update(Expression left, LambdaExpression conversion, Expression right)
    {
        if (left == Left && right == Right && conversion == Conversion)
        {
            return this;
        }

        if (!IsReferenceComparison)
        {
            return MakeBinary(NodeType, left, right, IsLiftedToNull, Method, conversion);
        }

        return NodeType == ExpressionType.Equal ? ReferenceEqual(left, right) : ReferenceNotEqual(left, right);
    }

    /// <summary>
    ///     Dispatches to the specific visit method for this node type. For example,
    ///     <see cref="T:System.Linq.Expressions.MethodCallExpression" /> calls the
    ///     <see
    ///         cref="M:System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />
    ///     .
    /// </summary>
    /// <returns>The result of visiting this node.</returns>
    /// <param name="visitor">The visitor to visit this node with.</param>
    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitBinary(this);
    }

    internal static Expression Create(
        ExpressionType nodeType,
        Expression left,
        Expression right,
        Type type,
        MethodInfo method,
        LambdaExpression conversion)
    {
        if (nodeType == ExpressionType.Assign)
        {
            return new AssignBinaryExpression(left, right);
        }

        if (conversion != null)
        {
            return new CoalesceConversionBinaryExpression(left, right, conversion);
        }

        if (method != null)
        {
            return new MethodBinaryExpression(nodeType, left, right, type, method);
        }

        return type == typeof(bool)
            ? new LogicalBinaryExpression(nodeType, left, right)
            : new SimpleBinaryExpression(nodeType, left, right, type);
    }

    internal virtual LambdaExpression GetConversion()
    {
        return null;
    }

    internal virtual MethodInfo GetMethod()
    {
        return null;
    }

    internal Expression ReduceUserdefinedLifted()
    {
        var parameterExpression1 = Parameter(Left.Type, "left");
        var parameterExpression2 = Parameter(Right.Type, "right");
        var booleanOperator = TypeUtils.GetBooleanOperator(Method.DeclaringType,
            NodeType == ExpressionType.AndAlso ? "op_False" : "op_True");
        return Block(new ParameterExpression[1]
        {
            parameterExpression1
        }, Assign(parameterExpression1, Left), Condition(Property(parameterExpression1, "HasValue"), Condition(
                Call(booleanOperator, Call(parameterExpression1, "GetValueOrDefault", null)), parameterExpression1,
                Block(
                    new ParameterExpression[1]
                    {
                        parameterExpression2
                    }, Assign(parameterExpression2, Right),
                    Condition(Property(parameterExpression2, "HasValue"),
                        Convert(
                            Call(Method, Call(parameterExpression1, "GetValueOrDefault", null),
                                Call(parameterExpression2, "GetValueOrDefault", null)), Type), Constant(null, Type)))),
            Constant(null, Type)));
    }

    private static ExpressionType GetBinaryOpFromAssignmentOp(ExpressionType op)
    {
        switch (op)
        {
            case ExpressionType.AddAssign:
                return ExpressionType.Add;
            case ExpressionType.AndAssign:
                return ExpressionType.And;
            case ExpressionType.DivideAssign:
                return ExpressionType.Divide;
            case ExpressionType.ExclusiveOrAssign:
                return ExpressionType.ExclusiveOr;
            case ExpressionType.LeftShiftAssign:
                return ExpressionType.LeftShift;
            case ExpressionType.ModuloAssign:
                return ExpressionType.Modulo;
            case ExpressionType.MultiplyAssign:
                return ExpressionType.Multiply;
            case ExpressionType.OrAssign:
                return ExpressionType.Or;
            case ExpressionType.PowerAssign:
                return ExpressionType.Power;
            case ExpressionType.RightShiftAssign:
                return ExpressionType.RightShift;
            case ExpressionType.SubtractAssign:
                return ExpressionType.Subtract;
            case ExpressionType.AddAssignChecked:
                return ExpressionType.AddChecked;
            case ExpressionType.MultiplyAssignChecked:
                return ExpressionType.MultiplyChecked;
            case ExpressionType.SubtractAssignChecked:
                return ExpressionType.SubtractChecked;
            default:
                throw Error.InvalidOperation(nameof(op));
        }
    }

    private static bool IsOpAssignment(ExpressionType op)
    {
        return (uint)(op - 63 /*0x3F*/) <= 13U;
    }

    private Expression ReduceIndex()
    {
        var left1 = (IndexExpression)Left;
        var variables = new List<ParameterExpression>(left1.Arguments.Count + 2);
        var expressionList = new List<Expression>(left1.Arguments.Count + 3);
        var parameterExpression1 = Variable(left1.Object.Type, "tempObj");
        variables.Add(parameterExpression1);
        expressionList.Add(Assign(parameterExpression1, left1.Object));
        var arguments = new List<Expression>(left1.Arguments.Count);
        foreach (var right in left1.Arguments)
        {
            var left2 = Variable(right.Type, "tempArg" + arguments.Count);
            variables.Add(left2);
            arguments.Add(left2);
            expressionList.Add(Assign(left2, right));
        }

        var left3 = MakeIndex(parameterExpression1, left1.Indexer, arguments);
        var right1 = (Expression)MakeBinary(GetBinaryOpFromAssignmentOp(NodeType), left3, Right, false, Method);
        var conversion = GetConversion();
        if (conversion != null)
        {
            right1 = Invoke(conversion, right1);
        }

        var parameterExpression2 = Variable(right1.Type, "tempValue");
        variables.Add(parameterExpression2);
        expressionList.Add(Assign(parameterExpression2, right1));
        expressionList.Add(Assign(left3, parameterExpression2));
        return Block(variables, expressionList);
    }

    private Expression ReduceMember()
    {
        var left1 = (MemberExpression)Left;
        if (left1.Expression == null)
        {
            return ReduceVariable();
        }

        var left2 = Variable(left1.Expression.Type, "temp1");
        var expression1 = (Expression)Assign(left2, left1.Expression);
        var right = (Expression)MakeBinary(GetBinaryOpFromAssignmentOp(NodeType), MakeMemberAccess(left2, left1.Member),
            Right, false, Method);
        var conversion = GetConversion();
        if (conversion != null)
        {
            right = Invoke(conversion, right);
        }

        var parameterExpression = Variable(right.Type, "temp2");
        var expression2 = (Expression)Assign(parameterExpression, right);
        var expression3 = (Expression)Assign(MakeMemberAccess(left2, left1.Member), parameterExpression);
        var expression4 = (Expression)parameterExpression;
        return Block(new ParameterExpression[2]
        {
            left2,
            parameterExpression
        }, expression1, expression2, expression3, expression4);
    }

    private Expression ReduceVariable()
    {
        var right = (Expression)MakeBinary(GetBinaryOpFromAssignmentOp(NodeType), Left, Right, false, Method);
        var conversion = GetConversion();
        if (conversion != null)
        {
            right = Invoke(conversion, right);
        }

        return Assign(Left, right);
    }
}