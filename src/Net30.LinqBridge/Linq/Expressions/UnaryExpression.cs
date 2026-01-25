#nullable disable
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

/// <summary>Represents an expression that has a unary operator.</summary>
[DebuggerTypeProxy(typeof(UnaryExpressionProxy))]
public sealed class UnaryExpression : Expression
{
    internal UnaryExpression(
        ExpressionType nodeType,
        Expression expression,
        Type type,
        MethodInfo method)
    {
        Operand = expression;
        Method = method;
        NodeType = nodeType;
        Type = type;
    }

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.UnaryExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type { get; }

    /// <summary>Returns the node type of this <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> that represents this expression.</returns>
    public override ExpressionType NodeType { get; }

    /// <summary>Gets the operand of the unary operation.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression" /> that represents the operand of the unary operation.</returns>
    public Expression Operand { get; }

    /// <summary>Gets the implementing method for the unary operation.</summary>
    /// <returns>The <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</returns>
    public MethodInfo Method { get; }

    /// <summary>Gets a value that indicates whether the expression tree node represents a lifted call to an operator.</summary>
    /// <returns>true if the node represents a lifted call; otherwise, false.</returns>
    public bool IsLifted
    {
        get
        {
            if (NodeType == ExpressionType.TypeAs || NodeType == ExpressionType.Quote ||
                NodeType == ExpressionType.Throw)
            {
                return false;
            }

            var flag1 = Operand.Type.IsNullableType();
            var flag2 = Type.IsNullableType();
            if (!(Method != null))
            {
                return flag1 | flag2;
            }

            if (flag1 && !TypeUtils.AreEquivalent(Method.GetParameters()[0].ParameterType, Operand.Type))
            {
                return true;
            }

            return flag2 && !TypeUtils.AreEquivalent(Method.ReturnType, Type);
        }
    }

    /// <summary>
    ///     Gets a value that indicates whether the expression tree node represents a lifted call to an operator whose
    ///     return type is lifted to a nullable type.
    /// </summary>
    /// <returns>true if the operator's return type is lifted to a nullable type; otherwise, false.</returns>
    public bool IsLiftedToNull => IsLifted && Type.IsNullableType();

    /// <summary>Gets a value that indicates whether the expression tree node can be reduced.</summary>
    /// <returns>True if a node can be reduced, otherwise false.</returns>
    public override bool CanReduce
    {
        get
        {
            switch (NodeType)
            {
                case ExpressionType.PreIncrementAssign:
                case ExpressionType.PreDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PostDecrementAssign:
                    return true;
                default:
                    return false;
            }
        }
    }

    private bool IsPrefix =>
        NodeType == ExpressionType.PreIncrementAssign || NodeType == ExpressionType.PreDecrementAssign;

    /// <summary>Reduces the expression node to a simpler expression. </summary>
    /// <returns>The reduced expression.</returns>
    public override Expression Reduce()
    {
        if (!CanReduce)
        {
            return this;
        }

        switch (Operand.NodeType)
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
    /// <param name="operand">The <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property of the result.</param>
    public UnaryExpression Update(Expression operand)
    {
        return operand == Operand ? this : MakeUnary(NodeType, operand, Type, Method);
    }

    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitUnary(this);
    }

    private UnaryExpression FunctionalOp(Expression operand)
    {
        return new UnaryExpression(
            NodeType == ExpressionType.PreIncrementAssign || NodeType == ExpressionType.PostIncrementAssign
                ? ExpressionType.Increment
                : ExpressionType.Decrement, operand, operand.Type, Method);
    }

    private Expression ReduceIndex()
    {
        var isPrefix = IsPrefix;
        var operand1 = (IndexExpression)Operand;
        var count = operand1.Arguments.Count;
        var list1 = new Expression[count + (isPrefix ? 2 : 4)];
        var list2 = new ParameterExpression[count + (isPrefix ? 1 : 2)];
        var list3 = new ParameterExpression[count];
        var index1 = 0;
        list2[index1] = Parameter(operand1.Object.Type, null);
        list1[index1] = Assign(list2[index1], operand1.Object);
        int index2;
        for (index2 = index1 + 1; index2 <= count; ++index2)
        {
            var right = operand1.Arguments[index2 - 1];
            list3[index2 - 1] = list2[index2] = Parameter(right.Type, null);
            list1[index2] = Assign(list2[index2], right);
        }

        var indexExpression = MakeIndex(list2[0], operand1.Indexer, new TrueReadOnlyCollection<Expression>(list3));
        int num1;
        if (!isPrefix)
        {
            var operand2 = list2[index2] = Parameter(indexExpression.Type, null);
            list1[index2] = Assign(list2[index2], indexExpression);
            var num2 = index2 + 1;
            var expressionArray1 = list1;
            var index3 = num2;
            var num3 = index3 + 1;
            var binaryExpression = Assign(indexExpression, FunctionalOp(operand2));
            expressionArray1[index3] = binaryExpression;
            var expressionArray2 = list1;
            var index4 = num3;
            num1 = index4 + 1;
            var parameterExpression = operand2;
            expressionArray2[index4] = parameterExpression;
        }
        else
        {
            var expressionArray = list1;
            var index5 = index2;
            num1 = index5 + 1;
            var binaryExpression = Assign(indexExpression, FunctionalOp(indexExpression));
            expressionArray[index5] = binaryExpression;
        }

        return Block(new TrueReadOnlyCollection<ParameterExpression>(list2),
            new TrueReadOnlyCollection<Expression>(list1));
    }

    private Expression ReduceMember()
    {
        var operand = (MemberExpression)Operand;
        if (operand.Expression == null)
        {
            return ReduceVariable();
        }

        var left = Parameter(operand.Expression.Type, null);
        var binaryExpression = Assign(left, operand.Expression);
        var memberExpression = MakeMemberAccess(left, operand.Member);
        if (IsPrefix)
        {
            return Block(new ParameterExpression[1]
            {
                left
            }, binaryExpression, Assign(memberExpression, FunctionalOp(memberExpression)));
        }

        var parameterExpression = Parameter(memberExpression.Type, null);
        return Block(new ParameterExpression[2]
            {
                left,
                parameterExpression
            }, binaryExpression, Assign(parameterExpression, memberExpression),
            Assign(memberExpression, FunctionalOp(parameterExpression)), parameterExpression);
    }

    private Expression ReduceVariable()
    {
        if (IsPrefix)
        {
            return Assign(Operand, FunctionalOp(Operand));
        }

        var parameterExpression = Parameter(Operand.Type, null);
        return Block(new ParameterExpression[1]
            {
                parameterExpression
            }, Assign(parameterExpression, Operand), Assign(Operand, FunctionalOp(parameterExpression)),
            parameterExpression);
    }
}