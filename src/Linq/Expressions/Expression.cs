using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Linq.Expressions;

/// <summary>Represents a strongly typed lambda expression as a data structure in the form of an expression tree. This class cannot be inherited.</summary>
/// <typeparam name="TDelegate">The type of the delegate that the <see cref="T:System.Linq.Expressions.Expression`1" /> represents.</typeparam>
public sealed class Expression<TDelegate> : LambdaExpression
{
    internal Expression(Expression body, ReadOnlyCollection<ParameterExpression> parameters)
        : base(body, typeof(TDelegate), parameters)
    {
    }

    /// <summary>Compiles the lambda expression described by the expression tree into executable code.</summary>
    /// <returns>A delegate of type <paramref name="TDelegate" /> that represents the lambda expression described by the <see cref="T:System.Linq.Expressions.Expression`1" />.</returns>
    public new TDelegate Compile()
    {
        return (TDelegate)((object)base.Compile());
    }
}

/// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains static (Shared in Visual Basic) factory methods to create the various node types. This is an abstract class.</summary>
public abstract class Expression
{
    private readonly ExpressionType nodeType;
    private readonly Type type;
    private static readonly Type[] lambdaTypes = new Type[2]
    {
        typeof (Expression),
        typeof (IEnumerable<ParameterExpression>)
    };
    private static readonly Type[] funcTypes = new Type[5]
    {
        typeof (Func<>),
        typeof (Func<,>),
        typeof (Func<,,>),
        typeof (Func<,,,>),
        typeof (Func<,,,,>)
    };
    private static readonly Type[] actionTypes = new Type[5]
    {
        typeof (Action),
        typeof (Action<>),
        typeof (Action<,>),
        typeof (Action<,,>),
        typeof (Action<,,,>)
    };

    /// <summary>Initializes a new instance of the <see cref="T:System.Linq.Expressions.Expression" /> class.</summary>
    /// <param name="nodeType">The <see cref="T:System.Linq.Expressions.ExpressionType" /> to set as the node type.</param>
    /// <param name="type">The <see cref="T:System.Type" /> to set as the type of the expression that this <see cref="T:System.Linq.Expressions.Expression" /> represents.</param>
    protected Expression(ExpressionType nodeType, Type type)
    {
        this.nodeType = nodeType;
        this.type = type;
    }

    [__DynamicallyInvokable]
    protected Expression()
    {
    }

    /// <summary>Gets the node type of this <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>One of the <see cref="T:System.Linq.Expressions.ExpressionType" /> values.</returns>
    public virtual ExpressionType NodeType => nodeType;

    /// <summary>Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" /> represents.</summary>
    /// <returns>The <see cref="T:System.Type" /> that represents the static type of the expression.</returns>
    public virtual Type Type => type;

    public static BinaryExpression Assign(Expression left, Expression right)
    {
        RequiresCanWrite(left, "left");
        RequiresCanRead(right, "right");
        TypeUtils.ValidateType(left.Type);
        TypeUtils.ValidateType(right.Type);
        if (!TypeUtils.AreReferenceAssignable(left.Type, right.Type))
        {
            throw new ArgumentException($"{right.Type}!={left.Type}");
        }
        return new AssignBinaryExpression(left, right);
    }

    private static void RequiresCanRead(Expression expression, string paramName)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(paramName);
        }
        var nodeType = expression.NodeType;
        if (nodeType == ExpressionType.MemberAccess)
        {
            var member = ((MemberExpression)expression).Member;
            if (member.MemberType == MemberTypes.Property && !((PropertyInfo)member).CanRead)
            {
                throw new ArgumentException(paramName);
            }
        }
        else if (nodeType == ExpressionType.Index)
        {
            var indexExpression = (IndexExpression)expression;
            if (indexExpression.Indexer != null && !indexExpression.Indexer.CanRead)
            {
                throw new ArgumentException(paramName);
            }
        }
    }

    private static void RequiresCanRead(IEnumerable<Expression> items, string paramName)
    {
        if (items != null)
        {
            var expressions = items as IList<Expression>;
            if (expressions != null)
            {
                for (var i = 0; i < expressions.Count; i++)
                {
                    RequiresCanRead(expressions[i], paramName);
                }
                return;
            }
            foreach (var item in items)
            {
                RequiresCanRead(item, paramName);
            }
        }
    }

    private static void RequiresCanWrite(Expression expression, string paramName)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(paramName);
        }
        var canWrite = false;
        var nodeType = expression.NodeType;
        if (nodeType == ExpressionType.MemberAccess)
        {
            var memberExpression = (MemberExpression)expression;
            var memberType = memberExpression.Member.MemberType;
            if (memberType == MemberTypes.Field)
            {
                var member = (FieldInfo)memberExpression.Member;
                canWrite = (member.IsInitOnly ? false : !member.IsLiteral);
            }
            else if (memberType == MemberTypes.Property)
            {
                canWrite = ((PropertyInfo)memberExpression.Member).CanWrite;
            }
        }
        else if (nodeType == ExpressionType.Parameter)
        {
            canWrite = true;
        }
        else if (nodeType == ExpressionType.Index)
        {
            var indexExpression = (IndexExpression)expression;
            canWrite = (indexExpression.Indexer == null ? true : indexExpression.Indexer.CanWrite);
        }
        if (!canWrite)
        {
            throw new ArgumentException(paramName);
        }
    }
    /// <summary>Returns a textual representation of the <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>A textual representation of the <see cref="T:System.Linq.Expressions.Expression" />.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder();
        BuildString(builder);
        return builder.ToString();
    }

    internal virtual void BuildString(StringBuilder builder)
    {
        if (builder == null)
            throw Error.ArgumentNull(nameof(builder));
        builder.Append("[");
        builder.Append(nodeType.ToString());
        builder.Append("]");
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition operation that does not have overflow checking.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Add" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The addition operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Add(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return left.Type == right.Type && IsArithmetic(left.Type) ? new BinaryExpression(ExpressionType.Add, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Add, "op_Addition", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition operation that does not have overflow checking. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Add" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the addition operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Add(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? Add(left, right) : GetMethodBasedBinaryOperator(ExpressionType.Add, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition operation that has overflow checking.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.AddChecked" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The addition operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression AddChecked(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return left.Type == right.Type && IsArithmetic(left.Type) ? new BinaryExpression(ExpressionType.AddChecked, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.AddChecked, "op_Addition", left, right, false);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition operation that has overflow checking. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.AddChecked" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the addition operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression AddChecked(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? AddChecked(left, right) : GetMethodBasedBinaryOperator(ExpressionType.AddChecked, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise AND operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.And" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The bitwise AND operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression And(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return left.Type == right.Type && IsIntegerOrBool(left.Type) ? new BinaryExpression(ExpressionType.And, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.And, "op_BitwiseAnd", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise AND operation. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.And" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the bitwise AND operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression And(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? And(left, right) : GetMethodBasedBinaryOperator(ExpressionType.And, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a conditional AND operation that evaluates the second operand only if it has to.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.AndAlso" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The bitwise AND operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.-or-<paramref name="left" />.Type and <paramref name="right" />.Type are not the same Boolean type.</exception>
    public static BinaryExpression AndAlso(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        if (left.Type == right.Type && IsBool(left.Type))
            return new BinaryExpression(ExpressionType.AndAlso, left, right, left.Type);
        var definedBinaryOperator = GetUserDefinedBinaryOperator(ExpressionType.AndAlso, left.Type, right.Type, "op_BitwiseAnd");
        if (definedBinaryOperator == null)
            throw Error.BinaryOperatorNotDefined(ExpressionType.AndAlso, left.Type, right.Type);
        ValidateUserDefinedConditionalLogicOperator(ExpressionType.AndAlso, left.Type, right.Type, definedBinaryOperator);
        var type = !IsNullableType(left.Type) || definedBinaryOperator.ReturnType != GetNonNullableType(left.Type) ? definedBinaryOperator.ReturnType : left.Type;
        return new BinaryExpression(ExpressionType.AndAlso, left, right, definedBinaryOperator, type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a conditional AND operation that evaluates the second operand only if it has to. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.AndAlso" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the bitwise AND operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.-or-<paramref name="method" /> is null and <paramref name="left" />.Type and <paramref name="right" />.Type are not the same Boolean type.</exception>
    public static BinaryExpression AndAlso(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        if (method == null)
            return AndAlso(left, right);
        ValidateUserDefinedConditionalLogicOperator(ExpressionType.AndAlso, left.Type, right.Type, method);
        var type = !IsNullableType(left.Type) || method.ReturnType != GetNonNullableType(left.Type) ? method.ReturnType : left.Type;
        return new BinaryExpression(ExpressionType.AndAlso, left, right, method, type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents applying an array index operator to an array of rank one.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ArrayIndex" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="array">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="index">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="array" /> or <paramref name="index" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="array" />.Type does not represent an array type.-or-<paramref name="array" />.Type represents an array type whose rank is not 1.-or-<paramref name="index" />.Type does not represent the <see cref="T:System.Int32" /> type.</exception>
    public static BinaryExpression ArrayIndex(Expression array, Expression index)
    {
        if (array == null)
            throw Error.ArgumentNull(nameof(array));
        if (index == null)
            throw Error.ArgumentNull(nameof(index));
        if (index.Type != typeof(int))
            throw Error.ArgumentMustBeArrayIndexType();
        if (!array.Type.IsArray)
            throw Error.ArgumentMustBeArray();
        if (array.Type.GetArrayRank() != 1)
            throw Error.IncorrectNumberOfIndexes();
        return new BinaryExpression(ExpressionType.ArrayIndex, array, index, array.Type.GetElementType());
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents applying an array index operator to an array of rank more than one.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
    /// <param name="array">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to.</param>
    /// <param name="indexes">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="array" /> or <paramref name="indexes" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="array" />.Type does not represent an array type.-or-The rank of <paramref name="array" />.Type does not match the number of elements in <paramref name="indexes" />.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="indexes" /> does not represent the <see cref="T:System.Int32" /> type.</exception>
    public static MethodCallExpression ArrayIndex(
        Expression array,
        params Expression[] indexes)
    {
        return ArrayIndex(array, (IEnumerable<Expression>)indexes);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents applying an array index operator to an array of rank more than one.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
    /// <param name="array">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to.</param>
    /// <param name="indexes">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="array" /> or <paramref name="indexes" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="array" />.Type does not represent an array type.-or-The rank of <paramref name="array" />.Type does not match the number of elements in <paramref name="indexes" />.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="indexes" /> does not represent the <see cref="T:System.Int32" /> type.</exception>
    public static MethodCallExpression ArrayIndex(
        Expression array,
        IEnumerable<Expression> indexes)
    {
        if (array == null)
            throw Error.ArgumentNull(nameof(array));
        if (indexes == null)
            throw Error.ArgumentNull(nameof(indexes));
        if (!array.Type.IsArray)
            throw Error.ArgumentMustBeArray();
        var readOnlyCollection = indexes.ToReadOnlyCollection<Expression>();
        if (array.Type.GetArrayRank() != readOnlyCollection.Count)
            throw Error.IncorrectNumberOfIndexes();
        foreach (var expression in readOnlyCollection)
        {
            if (expression.Type != typeof(int))
                throw Error.ArgumentMustBeArrayIndexType();
        }
        var method = array.Type.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public);
        return Call(array, method, readOnlyCollection);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents getting the length of a one-dimensional array.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ArrayLength" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to <paramref name="array" />.</returns>
    /// <param name="array">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="array" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="array" />.Type does not represent an array type.</exception>
    public static UnaryExpression ArrayLength(Expression array)
    {
        if (array == null)
            throw Error.ArgumentNull(nameof(array));
        if (!array.Type.IsArray || !AreAssignable(typeof(Array), array.Type))
            throw Error.ArgumentMustBeArray();
        return array.Type.GetArrayRank() == 1 ? new UnaryExpression(ExpressionType.ArrayLength, array, typeof(int)) : throw Error.ArgumentMustBeSingleDimensionalArrayType();
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberAssignment" /> that represents the initialization of a field or property.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberAssignment" /> that has <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> equal to <see cref="F:System.Linq.Expressions.MemberBindingType.Assignment" /> and the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and <see cref="P:System.Linq.Expressions.MemberAssignment.Expression" /> properties set to the specified values.</returns>
    /// <param name="member">A <see cref="T:System.Reflection.MemberInfo" /> to set the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MemberAssignment.Expression" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="member" /> or <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="member" /> does not represent a field or property.-or-The property represented by <paramref name="member" /> does not have a set accessor.-or-<paramref name="expression" />.Type is not assignable to the type of the field or property that <paramref name="member" /> represents.</exception>
    public static MemberAssignment Bind(MemberInfo member, Expression expression)
    {
        if (member == null)
            throw Error.ArgumentNull(nameof(member));
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        Type memberType;
        ValidateSettableFieldOrPropertyMember(member, out memberType);
        if (!AreAssignable(memberType, expression.Type))
            throw Error.ArgumentTypesMustMatch();
        return new MemberAssignment(member, expression);
    }

    private static PropertyInfo GetProperty(MethodInfo mi)
    {
        foreach (var property in mi.DeclaringType.GetProperties((BindingFlags)(48 | (mi.IsStatic ? 8 : 4))))
        {
            if (property.CanRead && CheckMethod(mi, property.GetGetMethod(true)) || property.CanWrite && CheckMethod(mi, property.GetSetMethod(true)))
                return property;
        }
        throw Error.MethodNotPropertyAccessor(mi.DeclaringType, mi.Name);
    }

    private static bool CheckMethod(MethodInfo method, MethodInfo propertyMethod)
    {
        if (method == propertyMethod)
            return true;
        var declaringType = method.DeclaringType;
        return declaringType.IsInterface && method.Name == propertyMethod.Name && declaringType.GetMethod(method.Name) == propertyMethod;
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberAssignment" /> that represents the initialization of a member by using a property accessor method.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberAssignment" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.Assignment" />, the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="T:System.Reflection.PropertyInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and the <see cref="P:System.Linq.Expressions.MemberAssignment.Expression" /> property set to <paramref name="expression" />.</returns>
    /// <param name="propertyAccessor">A <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MemberAssignment.Expression" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="propertyAccessor" /> or <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The property accessed by <paramref name="propertyAccessor" /> does not have a set accessor.-or-<paramref name="expression" />.Type is not assignable to the type of the field or property that <paramref name="member" /> represents.</exception>
    public static MemberAssignment Bind(
        MethodInfo propertyAccessor,
        Expression expression)
    {
        if (propertyAccessor == null)
            throw Error.ArgumentNull(nameof(propertyAccessor));
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        ValidateMethodInfo(propertyAccessor);
        return Bind(GetProperty(propertyAccessor), expression);
    }

    [__DynamicallyInvokable]
    public static BlockExpression Block(Expression arg0, Expression arg1)
    {
        RequiresCanRead(arg0, "arg0");
        RequiresCanRead(arg1, "arg1");
        return new Block2(arg0, arg1);
    }

    internal static T ReturnObject<T>(object collectionOrT)
        where T : class
    {
        var t = (T)(collectionOrT as T);
        if (t != null)
        {
            return t;
        }
        return ((ReadOnlyCollection<T>)collectionOrT)[0];
    }

    internal static ReadOnlyCollection<Expression> ReturnReadOnly(IArgumentProvider provider, ref object collection)
    {
        var expression = collection as Expression;
        if (expression != null)
        {
            Threading.Net20Interlocked.CompareExchange(ref collection, new ReadOnlyCollection<Expression>(new ListArgumentProvider(provider, expression)), expression);
        }
        return (ReadOnlyCollection<Expression>)collection;
    }


    [__DynamicallyInvokable]
    public static BlockExpression Block(Expression arg0, Expression arg1, Expression arg2)
    {
        RequiresCanRead(arg0, "arg0");
        RequiresCanRead(arg1, "arg1");
        RequiresCanRead(arg2, "arg2");
        return new Block3(arg0, arg1, arg2);
    }

    [__DynamicallyInvokable]
    public static BlockExpression Block(Expression arg0, Expression arg1, Expression arg2, Expression arg3)
    {
        RequiresCanRead(arg0, "arg0");
        RequiresCanRead(arg1, "arg1");
        RequiresCanRead(arg2, "arg2");
        RequiresCanRead(arg3, "arg3");
        return new Block4(arg0, arg1, arg2, arg3);
    }

    [__DynamicallyInvokable]
    public static BlockExpression Block(Expression arg0, Expression arg1, Expression arg2, Expression arg3, Expression arg4)
    {
        RequiresCanRead(arg0, "arg0");
        RequiresCanRead(arg1, "arg1");
        RequiresCanRead(arg2, "arg2");
        RequiresCanRead(arg3, "arg3");
        RequiresCanRead(arg4, "arg4");
        return new Block5(arg0, arg1, arg2, arg3, arg4);
    }

    [__DynamicallyInvokable]
    public static BlockExpression Block(params Expression[] expressions)
    {
        ContractUtils.RequiresNotNull(expressions, "expressions");
        switch (expressions.Length)
        {
            case 2:
            {
                return Block(expressions[0], expressions[1]);
            }
            case 3:
            {
                return Block(expressions[0], expressions[1], expressions[2]);
            }
            case 4:
            {
                return Block(expressions[0], expressions[1], expressions[2], expressions[3]);
            }
            case 5:
            {
                return Block(expressions[0], expressions[1], expressions[2], expressions[3], expressions[4]);
            }
        }
        ContractUtils.RequiresNotEmpty<Expression>(expressions, "expressions");
        RequiresCanRead(expressions, "expressions");
        return new BlockN(expressions.Copy<Expression>());
    }

    [__DynamicallyInvokable]
    public static BlockExpression Block(IEnumerable<Expression> expressions)
    {
        return Block(EmptyReadOnlyCollection<ParameterExpression>.Instance, expressions);
    }

    [__DynamicallyInvokable]
    public static BlockExpression Block(Type type, params Expression[] expressions)
    {
        ContractUtils.RequiresNotNull(expressions, "expressions");
        return Block(type, (IEnumerable<Expression>)expressions);
    }

    [__DynamicallyInvokable]
    public static BlockExpression Block(Type type, IEnumerable<Expression> expressions)
    {
        return Block(type, EmptyReadOnlyCollection<ParameterExpression>.Instance, expressions);
    }

    [__DynamicallyInvokable]
    public static BlockExpression Block(IEnumerable<ParameterExpression> variables, params Expression[] expressions)
    {
        return Block(variables, (IEnumerable<Expression>)expressions);
    }

    [__DynamicallyInvokable]
    public static BlockExpression Block(Type type, IEnumerable<ParameterExpression> variables, params Expression[] expressions)
    {
        return Block(type, variables, (IEnumerable<Expression>)expressions);
    }

    [__DynamicallyInvokable]
    public static BlockExpression Block(IEnumerable<ParameterExpression> variables, IEnumerable<Expression> expressions)
    {
        ContractUtils.RequiresNotNull(expressions, "expressions");
        var readOnly = expressions.ToReadOnly<Expression>();
        ContractUtils.RequiresNotEmpty<Expression>(readOnly, "expressions");
        RequiresCanRead(readOnly, "expressions");
        return Block(readOnly.Last<Expression>().Type, variables, readOnly);
    }

    [__DynamicallyInvokable]
    public static BlockExpression Block(Type type, IEnumerable<ParameterExpression> variables, IEnumerable<Expression> expressions)
    {
        ContractUtils.RequiresNotNull(type, "type");
        ContractUtils.RequiresNotNull(expressions, "expressions");
        var readOnly = expressions.ToReadOnly<Expression>();
        var parameterExpressions = variables.ToReadOnly<ParameterExpression>();
        ContractUtils.RequiresNotEmpty<Expression>(readOnly, "expressions");
        RequiresCanRead(readOnly, "expressions");
        ValidateVariables(parameterExpressions, "variables");
        var expression = readOnly.Last<Expression>();
        if (type != typeof(void) && !TypeUtils.AreReferenceAssignable(type, expression.Type))
        {
            throw Error.ArgumentTypesMustMatch();
        }
        if (!TypeUtils.AreEquivalent(type, expression.Type))
        {
            return new ScopeWithType(parameterExpressions, readOnly, type);
        }
        if (readOnly.Count != 1)
        {
            return new ScopeN(parameterExpressions, readOnly);
        }
        return new Scope1(parameterExpressions, readOnly[0]);
    }

    internal static void ValidateVariables(ReadOnlyCollection<ParameterExpression> varList, string collectionName)
    {
        if (varList.Count == 0)
        {
            return;
        }
        var count = varList.Count;
        var parameterExpressions = new List<ParameterExpression>(count);
        for (var i = 0; i < count; i++)
        {
            var item = varList[i];
            if (item == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, "{0}[{1}]", new object[] { collectionName, parameterExpressions.Count }));
            }
            if (item.IsByRef)
            {
                throw new ArgumentException($"{item.Name} cannot be by ref");
            }
            if (parameterExpressions.Contains(item))
            {
                throw new ArgumentException($"DuplicateVariable {item}");
            }
            parameterExpressions.Add(item);
        }
    }


    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static (Shared in Visual Basic) method.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that represents a static (Shared in Visual Basic) method to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
    /// <param name="arguments">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="method" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The number of elements in <paramref name="arguments" /> does not equal the number of parameters for the method represented by <paramref name="method" />.-or-One or more of the elements of <paramref name="arguments" /> is not assignable to the corresponding parameter for the method represented by <paramref name="method" />.</exception>
    public static MethodCallExpression Call(
        MethodInfo method,
        params Expression[] arguments)
    {
        return Call(null, method, arguments.ToReadOnlyCollection<Expression>());
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method that takes arguments.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" />, <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" />, and <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
    /// <param name="instance">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to (pass null for a static (Shared in Visual Basic) method).</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
    /// <param name="arguments">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="method" /> is null.-or-<paramref name="instance" /> is null and <paramref name="method" /> represents an instance method.-or-<paramref name="arguments" /> is not null and one or more of its elements is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="instance" />.Type is not assignable to the declaring type of the method represented by <paramref name="method" />.-or-The number of elements in <paramref name="arguments" /> does not equal the number of parameters for the method represented by <paramref name="method" />.-or-One or more of the elements of <paramref name="arguments" /> is not assignable to the corresponding parameter for the method represented by <paramref name="method" />.</exception>
    public static MethodCallExpression Call(
        Expression instance,
        MethodInfo method,
        params Expression[] arguments)
    {
        return Call(instance, method, arguments.ToReadOnlyCollection<Expression>());
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method that takes arguments.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" />, <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" />, and <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.</returns>
    /// <param name="instance">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to (pass null for a static (Shared in Visual Basic) method).</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
    /// <param name="arguments">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="method" /> is null.-or-<paramref name="instance" /> is null and <paramref name="method" /> represents an instance method.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="instance" />.Type is not assignable to the declaring type of the method represented by <paramref name="method" />.-or-The number of elements in <paramref name="arguments" /> does not equal the number of parameters for the method represented by <paramref name="method" />.-or-One or more of the elements of <paramref name="arguments" /> is not assignable to the corresponding parameter for the method represented by <paramref name="method" />.</exception>
    public static MethodCallExpression Call(
        Expression instance,
        MethodInfo method,
        IEnumerable<Expression> arguments)
    {
        var readOnlyCollection = arguments.ToReadOnlyCollection<Expression>();
        ValidateCallArgs(instance, method, ref readOnlyCollection);
        return new MethodCallExpression(ExpressionType.Call, method, instance, readOnlyCollection);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method that takes no arguments.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="instance">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to (pass null for a static (Shared in Visual Basic) method).</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="method" /> is null.-or-<paramref name="instance" /> is null and <paramref name="method" /> represents an instance method.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="instance" />.Type is not assignable to the declaring type of the method represented by <paramref name="method" />.</exception>
    public static MethodCallExpression Call(
        Expression instance,
        MethodInfo method)
    {
        return Call(instance, method, null);
    }

    private static void ValidateCallArgs(
        Expression instance,
        MethodInfo method,
        ref ReadOnlyCollection<Expression> arguments)
    {
        if (method == null)
            throw Error.ArgumentNull(nameof(method));
        if (arguments == null)
            throw Error.ArgumentNull(nameof(arguments));
        ValidateMethodInfo(method);
        if (!method.IsStatic)
        {
            if (instance == null)
                throw Error.ArgumentNull(nameof(instance));
            ValidateCallInstanceType(instance.Type, method);
        }
        ValidateArgumentTypes(method, ref arguments);
    }

    private static void ValidateCallInstanceType(Type instanceType, MethodInfo method)
    {
        if (!AreReferenceAssignable(method.DeclaringType, instanceType))
        {
            if (instanceType.IsValueType)
            {
                if (AreReferenceAssignable(method.DeclaringType, typeof(object)) || AreReferenceAssignable(method.DeclaringType, typeof(ValueType)) || instanceType.IsEnum && AreReferenceAssignable(method.DeclaringType, typeof(Enum)))
                    return;
                if (method.DeclaringType.IsInterface)
                {
                    foreach (var src in instanceType.GetInterfaces())
                    {
                        if (AreReferenceAssignable(method.DeclaringType, src))
                            return;
                    }
                }
            }
            throw Error.MethodNotDefinedForType(method, instanceType);
        }
    }

    private static void ValidateArgumentTypes(
        MethodInfo method,
        ref ReadOnlyCollection<Expression> arguments)
    {
        var parameters = method.GetParameters();
        if (parameters.Length > 0)
        {
            if (parameters.Length != arguments.Count)
                throw Error.IncorrectNumberOfMethodCallArguments(method);
            List<Expression> sequence = null;
            var index1 = 0;
            for (var length = parameters.Length; index1 < length; ++index1)
            {
                var expression = arguments[index1];
                var parameterInfo = parameters[index1];
                if (expression == null)
                    throw Error.ArgumentNull(nameof(arguments));
                var type = parameterInfo.ParameterType;
                if (type.IsByRef)
                    type = type.GetElementType();
                ValidateType(type);
                if (!AreReferenceAssignable(type, expression.Type))
                    expression = IsSameOrSubclass(typeof(Expression), type) && AreAssignable(type, expression.GetType()) ? (Expression)Quote(expression) : throw Error.ExpressionTypeDoesNotMatchMethodParameter(expression.Type, type, method);
                if (sequence == null && expression != arguments[index1])
                {
                    sequence = new List<Expression>(arguments.Count);
                    for (var index2 = 0; index2 < index1; ++index2)
                        sequence.Add(arguments[index2]);
                }
                sequence?.Add(expression);
            }
            if (sequence == null)
                return;
            arguments = sequence.ToReadOnlyCollection<Expression>();
        }
        else if (arguments.Count > 0)
            throw Error.IncorrectNumberOfMethodCallArguments(method);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to an instance method by calling the appropriate factory method.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" />, the <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to <paramref name="instance" />, <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> set to the <see cref="T:System.Reflection.MethodInfo" /> that represents the specified instance method, and <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> set to the specified arguments.</returns>
    /// <param name="instance">An <see cref="T:System.Linq.Expressions.Expression" /> whose <see cref="P:System.Linq.Expressions.Expression.Type" /> property value will be searched for a specific method.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="typeArguments">An array of <see cref="T:System.Type" /> objects that specify the type parameters of the method.</param>
    /// <param name="arguments">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects that represents the arguments to the method.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="instance" /> or <paramref name="methodName" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">No method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="instance" />.Type or its base types.-or-More than one method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="instance" />.Type or its base types.</exception>
    public static MethodCallExpression Call(
        Expression instance,
        string methodName,
        Type[] typeArguments,
        params Expression[] arguments)
    {
        if (instance == null)
            throw Error.ArgumentNull(nameof(instance));
        if (methodName == null)
            throw Error.ArgumentNull(nameof(methodName));
        if (arguments == null)
            arguments = new Expression[0];
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        return Call(instance, FindMethod(instance.Type, methodName, typeArguments, arguments, flags), arguments);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static (Shared in Visual Basic) method by calling the appropriate factory method.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Call" />, the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property set to the <see cref="T:System.Reflection.MethodInfo" /> that represents the specified static (Shared in Visual Basic) method, and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> property set to the specified arguments.</returns>
    /// <param name="type">The <see cref="T:System.Type" /> that specifies the type that contains the specified static (Shared in Visual Basic) method.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="typeArguments">An array of <see cref="T:System.Type" /> objects that specify the type parameters of the method.</param>
    /// <param name="arguments">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects that represent the arguments to the method.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="type" /> or <paramref name="methodName" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">No method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="type" /> or its base types.-or-More than one method whose name is <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" /> is found in <paramref name="type" /> or its base types.</exception>
    public static MethodCallExpression Call(
        Type type,
        string methodName,
        Type[] typeArguments,
        params Expression[] arguments)
    {
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        if (methodName == null)
            throw Error.ArgumentNull(nameof(methodName));
        if (arguments == null)
            arguments = new Expression[0];
        var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        return Call(null, FindMethod(type, methodName, typeArguments, arguments, flags), arguments);
    }

    private static MethodInfo FindMethod(
        Type type,
        string methodName,
        Type[] typeArgs,
        Expression[] args,
        BindingFlags flags)
    {
        MemberInfo[] members = type.FindMembers(MemberTypes.Method, flags, Type.FilterNameIgnoreCase, methodName);
        if (members == null || members.Length == 0)
            throw Error.MethodDoesNotExistOnType(methodName, type);
        MethodInfo method;
        var bestMethod = FindBestMethod(members.Cast<MethodInfo>(), typeArgs, args, out method);
        if (bestMethod == 0)
            throw Error.MethodWithArgsDoesNotExistOnType(methodName, type);
        if (bestMethod > 1)
            throw Error.MethodWithMoreThanOneMatch(methodName, type);
        return method;
    }

    private static int FindBestMethod(
        IEnumerable<MethodInfo> methods,
        Type[] typeArgs,
        Expression[] args,
        out MethodInfo method)
    {
        var bestMethod = 0;
        method = null;
        foreach (var method1 in methods)
        {
            var m = ApplyTypeArgs(method1, typeArgs);
            if (m != null && IsCompatible(m, args))
            {
                if (method == null || !method.IsPublic && m.IsPublic)
                {
                    method = m;
                    bestMethod = 1;
                }
                else if (method.IsPublic == m.IsPublic)
                    ++bestMethod;
            }
        }
        return bestMethod;
    }

    private static MethodInfo ApplyTypeArgs(MethodInfo m, Type[] typeArgs)
    {
        if (typeArgs == null || typeArgs.Length == 0)
        {
            if (!m.IsGenericMethodDefinition)
                return m;
        }
        else if (m.IsGenericMethodDefinition && m.GetGenericArguments().Length == typeArgs.Length)
            return m.MakeGenericMethod(typeArgs);
        return null;
    }

    private static bool IsCompatible(MethodInfo m, Expression[] args)
    {
        var parameters = m.GetParameters();
        if (parameters.Length != args.Length)
            return false;
        for (var index = 0; index < args.Length; ++index)
        {
            var expression = args[index];
            var src = expression != null ? expression.Type : throw Error.ArgumentNull("argument");
            var type = parameters[index].ParameterType;
            if (type.IsByRef)
                type = type.GetElementType();
            if (!AreReferenceAssignable(type, src) && (!IsSameOrSubclass(typeof(Expression), type) || !AreAssignable(type, expression.GetType())))
                return false;
        }
        return true;
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a coalescing operation, given a conversion function.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Coalesce" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="conversion">A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="left" />.Type and <paramref name="right" />.Type are not convertible to each other.-or-<paramref name="conversion" /> is not null and <paramref name="conversion" />.Type is a delegate type that does not take exactly one argument.</exception>
    /// <exception cref="T:System.InvalidOperationException">The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of <paramref name="left" /> does not represent a reference type or a nullable value type.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of <paramref name="left" /> represents a type that is not assignable to the parameter type of the delegate type <paramref name="conversion" />.Type.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of <paramref name="right" /> is not equal to the return type of the delegate type <paramref name="conversion" />.Type.</exception>
    public static BinaryExpression Coalesce(
        Expression left,
        Expression right,
        LambdaExpression conversion)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        if (conversion == null)
            return Coalesce(left, right);
        if (left.Type.IsValueType && !IsNullableType(left.Type))
            throw Error.CoalesceUsedOnNonNullType();
        var method = conversion.Type.GetMethod("Invoke");
        var parameterInfoArray = method.ReturnType != typeof(void) ? method.GetParameters() : throw Error.UserDefinedOperatorMustNotBeVoid(conversion);
        if (parameterInfoArray.Length != 1)
            throw Error.IncorrectNumberOfMethodCallArguments(conversion);
        if (method.ReturnType != right.Type)
            throw Error.OperandTypesDoNotMatchParameters(ExpressionType.Coalesce, conversion.ToString());
        if (!ParameterIsAssignable(parameterInfoArray[0], GetNonNullableType(left.Type)) && !ParameterIsAssignable(parameterInfoArray[0], left.Type))
            throw Error.OperandTypesDoNotMatchParameters(ExpressionType.Coalesce, conversion.ToString());
        return new BinaryExpression(ExpressionType.Coalesce, left, right, conversion, right.Type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a coalescing operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Coalesce" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of <paramref name="left" /> does not represent a reference type or a nullable value type.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="left" />.Type and <paramref name="right" />.Type are not convertible to each other.</exception>
    public static BinaryExpression Coalesce(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        var type = ValidateCoalesceArgTypes(left.Type, right.Type);
        return new BinaryExpression(ExpressionType.Coalesce, left, right, type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.ConditionalExpression" />.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ConditionalExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Conditional" /> and the <see cref="P:System.Linq.Expressions.ConditionalExpression.Test" />, <see cref="P:System.Linq.Expressions.ConditionalExpression.IfTrue" />, and <see cref="P:System.Linq.Expressions.ConditionalExpression.IfFalse" /> properties set to the specified values.</returns>
    /// <param name="test">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.ConditionalExpression.Test" /> property equal to.</param>
    /// <param name="ifTrue">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.ConditionalExpression.IfTrue" /> property equal to.</param>
    /// <param name="ifFalse">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.ConditionalExpression.IfFalse" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="test" /> or <paramref name="ifTrue" /> or <paramref name="ifFalse" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="test" />.Type is not <see cref="T:System.Boolean" />.-or-<paramref name="ifTrue" />.Type is not equal to <paramref name="ifFalse" />.Type.</exception>
    public static ConditionalExpression Condition(
        Expression test,
        Expression ifTrue,
        Expression ifFalse)
    {
        if (test == null)
            throw Error.ArgumentNull(nameof(test));
        if (ifTrue == null)
            throw Error.ArgumentNull(nameof(ifTrue));
        if (ifFalse == null)
            throw Error.ArgumentNull(nameof(ifFalse));
        if (test.Type != typeof(bool))
            throw Error.ArgumentMustBeBoolean();
        ValidateSameArgTypes(ifTrue.Type, ifFalse.Type);
        return new ConditionalExpression(test, ifTrue, ifFalse, ifTrue.Type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.ConstantExpression" /> that has the <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> property set to the specified value.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ConstantExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Constant" /> and the <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> property set to the specified value.</returns>
    /// <param name="value">An <see cref="T:System.Object" /> to set the <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> property equal to.</param>
    public static ConstantExpression Constant(object value)
    {
        var type = value != null ? value.GetType() : typeof(object);
        return Constant(value, type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.ConstantExpression" /> that has the <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> and <see cref="P:System.Linq.Expressions.Expression.Type" /> properties set to the specified values.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ConstantExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Constant" /> and the <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> and <see cref="P:System.Linq.Expressions.Expression.Type" /> properties set to the specified values.</returns>
    /// <param name="value">An <see cref="T:System.Object" /> to set the <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> property equal to.</param>
    /// <param name="type">A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="type" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="value" /> is not null and <paramref name="type" /> is not assignable from the dynamic type of <paramref name="value" />.</exception>
    public static ConstantExpression Constant(object value, Type type)
    {
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        if (value == null && type.IsValueType && !IsNullableType(type))
            throw Error.ArgumentTypesMustMatch();
        return value == null || AreAssignable(type, value.GetType()) ? new ConstantExpression(value, type) : throw Error.ArgumentTypesMustMatch();
    }

    private static bool HasIdentityPrimitiveOrNullableConversion(Type source, Type dest) => source == dest || IsNullableType(source) && dest == GetNonNullableType(source) || IsNullableType(dest) && source == GetNonNullableType(dest) || IsConvertible(source) && IsConvertible(dest) && GetNonNullableType(dest) != typeof(bool);

    private static bool HasReferenceConversion(Type source, Type dest)
    {
        var nonNullableType1 = GetNonNullableType(source);
        var nonNullableType2 = GetNonNullableType(dest);
        return AreAssignable(nonNullableType1, nonNullableType2) || AreAssignable(nonNullableType2, nonNullableType1) || source.IsInterface || dest.IsInterface || source == typeof(object) || dest == typeof(object);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a conversion operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Convert" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="P:System.Linq.Expressions.Expression.Type" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <param name="type">A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> or <paramref name="type" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">No conversion operator is defined between <paramref name="expression" />.Type and <paramref name="type" />.</exception>
    public static UnaryExpression Convert(Expression expression, Type type)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        return HasIdentityPrimitiveOrNullableConversion(expression.Type, type) || HasReferenceConversion(expression.Type, type) ? new UnaryExpression(ExpressionType.Convert, expression, type) : GetUserDefinedCoercionOrThrow(ExpressionType.Convert, expression, type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a conversion operation for which the implementing method is specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Convert" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" />, <see cref="P:System.Linq.Expressions.Expression.Type" />, and <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <param name="type">A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> or <paramref name="type" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
    /// <exception cref="T:System.Reflection.AmbiguousMatchException">More than one method that matches the <paramref name="method" /> description was found.</exception>
    /// <exception cref="T:System.InvalidOperationException">No conversion operator is defined between <paramref name="expression" />.Type and <paramref name="type" />.-or-<paramref name="expression" />.Type is not assignable to the argument type of the method represented by <paramref name="method" />.-or-The return type of the method represented by <paramref name="method" /> is not assignable to <paramref name="type" />.-or-<paramref name="expression" />.Type or <paramref name="type" /> is a nullable value type and the corresponding non-nullable value type does not equal the argument type or the return type, respectively, of the method represented by <paramref name="method" />.</exception>
    public static UnaryExpression Convert(
        Expression expression,
        Type type,
        MethodInfo method)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return method == null ? Convert(expression, type) : GetMethodBasedCoercionOperator(ExpressionType.Convert, expression, type, method);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a conversion operation that throws an exception if the target type is overflowed.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ConvertChecked" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="P:System.Linq.Expressions.Expression.Type" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <param name="type">A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> or <paramref name="type" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">No conversion operator is defined between <paramref name="expression" />.Type and <paramref name="type" />.</exception>
    public static UnaryExpression ConvertChecked(Expression expression, Type type)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        if (HasIdentityPrimitiveOrNullableConversion(expression.Type, type))
            return new UnaryExpression(ExpressionType.ConvertChecked, expression, type);
        return HasReferenceConversion(expression.Type, type) ? new UnaryExpression(ExpressionType.Convert, expression, type) : GetUserDefinedCoercionOrThrow(ExpressionType.ConvertChecked, expression, type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a conversion operation that throws an exception if the target type is overflowed and for which the implementing method is specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ConvertChecked" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" />, <see cref="P:System.Linq.Expressions.Expression.Type" />, and <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <param name="type">A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> or <paramref name="type" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
    /// <exception cref="T:System.Reflection.AmbiguousMatchException">More than one method that matches the <paramref name="method" /> description was found.</exception>
    /// <exception cref="T:System.InvalidOperationException">No conversion operator is defined between <paramref name="expression" />.Type and <paramref name="type" />.-or-<paramref name="expression" />.Type is not assignable to the argument type of the method represented by <paramref name="method" />.-or-The return type of the method represented by <paramref name="method" /> is not assignable to <paramref name="type" />.-or-<paramref name="expression" />.Type or <paramref name="type" /> is a nullable value type and the corresponding non-nullable value type does not equal the argument type or the return type, respectively, of the method represented by <paramref name="method" />.</exception>
    public static UnaryExpression ConvertChecked(
        Expression expression,
        Type type,
        MethodInfo method)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return method == null ? ConvertChecked(expression, type) : GetMethodBasedCoercionOperator(ExpressionType.ConvertChecked, expression, type, method);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic division operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Divide" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The division operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Divide(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return left.Type == right.Type && IsArithmetic(left.Type) ? new BinaryExpression(ExpressionType.Divide, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Divide, "op_Division", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic division operation. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Divide" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the division operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Divide(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? Divide(left, right) : GetMethodBasedBinaryOperator(ExpressionType.Divide, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an equality comparison.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Equal" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The equality operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Equal(Expression left, Expression right) => Equal(left, right, false, null);

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an equality comparison. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Equal" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="liftToNull">true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true; false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the equality operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Equal(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? GetEqualityComparisonOperator(ExpressionType.Equal, "op_Equality", left, right, liftToNull) : GetMethodBasedBinaryOperator(ExpressionType.Equal, left, right, method, liftToNull);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise XOR operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ExclusiveOr" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The XOR operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression ExclusiveOr(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return left.Type == right.Type && IsIntegerOrBool(left.Type) ? new BinaryExpression(ExpressionType.ExclusiveOr, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.ExclusiveOr, "op_ExclusiveOr", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise XOR operation. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ExclusiveOr" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the XOR operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression ExclusiveOr(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? ExclusiveOr(left, right) : GetMethodBasedBinaryOperator(ExpressionType.ExclusiveOr, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a field.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" /> and the <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> and <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property equal to.</param>
    /// <param name="field">The <see cref="T:System.Reflection.FieldInfo" /> to set the <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="field" /> is null.-or-The field represented by <paramref name="field" /> is not static (Shared in Visual Basic) and <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="expression" />.Type is not assignable to the declaring type of the field represented by <paramref name="field" />.</exception>
    public static MemberExpression Field(Expression expression, FieldInfo field)
    {
        if (field == null)
            throw Error.ArgumentNull(nameof(field));
        if (!field.IsStatic)
        {
            if (expression == null)
                throw Error.ArgumentNull(nameof(expression));
            if (!AreReferenceAssignable(field.DeclaringType, expression.Type))
                throw Error.FieldNotDefinedForType(field, expression.Type);
        }
        return new MemberExpression(expression, field, field.FieldType);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a field given the name of the field.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" />, the <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" />, and the <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property set to the <see cref="T:System.Reflection.FieldInfo" /> that represents the field denoted by <paramref name="fieldName" />.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> whose <see cref="P:System.Linq.Expressions.Expression.Type" /> contains a field named <paramref name="fieldName" />.</param>
    /// <param name="fieldName">The name of a field.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> or <paramref name="fieldName" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">No field named <paramref name="fieldName" /> is defined in <paramref name="expression" />.Type or its base types.</exception>
    public static MemberExpression Field(Expression expression, string fieldName)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return Field(expression, (expression.Type.GetField(fieldName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy) ?? expression.Type.GetField(fieldName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)) ?? throw Error.FieldNotDefinedForType(fieldName, expression.Type));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "greater than or equal" numeric comparison.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.GreaterThanOrEqual" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The "greater than or equal" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression GreaterThanOrEqual(
        Expression left,
        Expression right)
    {
        return GreaterThanOrEqual(left, right, false, null);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "greater than or equal" numeric comparison. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.GreaterThanOrEqual" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="liftToNull">true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true; false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the "greater than or equal" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression GreaterThanOrEqual(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? GetComparisonOperator(ExpressionType.GreaterThanOrEqual, "op_GreaterThanOrEqual", left, right, liftToNull) : GetMethodBasedBinaryOperator(ExpressionType.GreaterThanOrEqual, left, right, method, liftToNull);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "greater than" numeric comparison.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.GreaterThan" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The "greater than" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression GreaterThan(Expression left, Expression right) => GreaterThan(left, right, false, null);

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "greater than" numeric comparison. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.GreaterThan" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="liftToNull">true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true; false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the "greater than" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression GreaterThan(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? GetComparisonOperator(ExpressionType.GreaterThan, "op_GreaterThan", left, right, liftToNull) : GetMethodBasedBinaryOperator(ExpressionType.GreaterThan, left, right, method, liftToNull);
    }

    /// <summary>Creates an <see cref="T:System.Linq.Expressions.InvocationExpression" />.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.InvocationExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Invoke" /> and the <see cref="P:System.Linq.Expressions.InvocationExpression.Expression" /> and <see cref="P:System.Linq.Expressions.InvocationExpression.Arguments" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.InvocationExpression.Expression" /> equal to</param>
    /// <param name="arguments">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.InvocationExpression.Arguments" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="expression" />.Type does not represent a delegate type or an <see cref="T:System.Linq.Expressions.Expression`1" />.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the delegate represented by <paramref name="expression" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="arguments" /> does not contain the same number of elements as the list of parameters for the delegate represented by <paramref name="expression" />.</exception>
    public static InvocationExpression Invoke(
        Expression expression,
        params Expression[] arguments)
    {
        return Invoke(expression, arguments.ToReadOnlyCollection<Expression>());
    }

    /// <summary>Creates an <see cref="T:System.Linq.Expressions.InvocationExpression" />.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.InvocationExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Invoke" /> and the <see cref="P:System.Linq.Expressions.InvocationExpression.Expression" /> and <see cref="P:System.Linq.Expressions.InvocationExpression.Arguments" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.InvocationExpression.Expression" /> equal to</param>
    /// <param name="arguments">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.InvocationExpression.Arguments" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="expression" />.Type does not represent a delegate type or an <see cref="T:System.Linq.Expressions.Expression`1" />.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the delegate represented by <paramref name="expression" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="arguments" /> does not contain the same number of elements as the list of parameters for the delegate represented by <paramref name="expression" />.</exception>
    public static InvocationExpression Invoke(
        Expression expression,
        IEnumerable<Expression> arguments)
    {
        var p0 = expression != null ? expression.Type : throw Error.ArgumentNull(nameof(expression));
        if (p0 == typeof(Delegate))
            throw Error.ExpressionTypeNotInvocable(p0);
        if (!AreAssignable(typeof(Delegate), expression.Type))
            p0 = (TypeHelper.FindGenericType(typeof(Expression<>), expression.Type) ?? throw Error.ExpressionTypeNotInvocable(expression.Type)).GetGenericArguments()[0];
        var method = p0.GetMethod(nameof(Invoke));
        var parameters = method.GetParameters();
        var readOnlyCollection = arguments.ToReadOnlyCollection<Expression>();
        if (parameters.Length > 0)
        {
            if (readOnlyCollection.Count != parameters.Length)
                throw Error.IncorrectNumberOfLambdaArguments();
            List<Expression> sequence = null;
            var index1 = 0;
            for (var count = readOnlyCollection.Count; index1 < count; ++index1)
            {
                var expression1 = readOnlyCollection[index1];
                var parameterInfo = parameters[index1];
                if (expression1 == null)
                    throw Error.ArgumentNull(nameof(arguments));
                var type = parameterInfo.ParameterType;
                if (type.IsByRef)
                    type = type.GetElementType();
                if (!AreReferenceAssignable(type, expression1.Type))
                    expression1 = IsSameOrSubclass(typeof(Expression), type) && AreAssignable(type, expression1.GetType()) ? (Expression)Quote(expression1) : throw Error.ExpressionTypeDoesNotMatchParameter(expression1.Type, type);
                if (sequence == null && expression1 != readOnlyCollection[index1])
                {
                    sequence = new List<Expression>(readOnlyCollection.Count);
                    for (var index2 = 0; index2 < index1; ++index2)
                        sequence.Add(readOnlyCollection[index2]);
                }
                sequence?.Add(expression1);
            }
            if (sequence != null)
                readOnlyCollection = sequence.ToReadOnlyCollection<Expression>();
        }
        else if (readOnlyCollection.Count > 0)
            throw Error.IncorrectNumberOfLambdaArguments();
        return new InvocationExpression(expression, method.ReturnType, readOnlyCollection);
    }

    /// <summary>Creates an <see cref="T:System.Linq.Expressions.Expression`1" /> where the delegate type is known at compile time.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression`1" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
    /// <param name="body">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
    /// <param name="parameters">An array of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
    /// <typeparam name="TDelegate">A delegate type.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="body" /> is null.-or-One or more elements in <paramref name="parameters" /> are null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="TDelegate" /> is not a delegate type.-or-<paramref name="body" />.Type represents a type that is not assignable to the return type of <paramref name="TDelegate" />.-or-<paramref name="parameters" /> does not contain the same number of elements as the list of parameters for <paramref name="TDelegate" />.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" /> is not assignable from the type of the corresponding parameter type of <paramref name="TDelegate" />.</exception>
    public static Expression<TDelegate> Lambda<TDelegate>(
        Expression body,
        params ParameterExpression[] parameters)
    {
        return Lambda<TDelegate>(body, parameters.ToReadOnlyCollection<ParameterExpression>());
    }

    /// <summary>Creates an <see cref="T:System.Linq.Expressions.Expression`1" /> where the delegate type is known at compile time.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression`1" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
    /// <param name="body">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
    /// <param name="parameters">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
    /// <typeparam name="TDelegate">A delegate type.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="body" /> is null.-or-One or more elements in <paramref name="parameters" /> are null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="TDelegate" /> is not a delegate type.-or-<paramref name="body" />.Type represents a type that is not assignable to the return type of <paramref name="TDelegate" />.-or-<paramref name="parameters" /> does not contain the same number of elements as the list of parameters for <paramref name="TDelegate" />.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" /> is not assignable from the type of the corresponding parameter type of <paramref name="TDelegate" />.</exception>
    public static Expression<TDelegate> Lambda<TDelegate>(
        Expression body,
        IEnumerable<ParameterExpression> parameters)
    {
        if (body == null)
            throw Error.ArgumentNull(nameof(body));
        var readOnlyCollection = parameters.ToReadOnlyCollection<ParameterExpression>();
        ValidateLambdaArgs(typeof(TDelegate), ref body, readOnlyCollection);
        return new Expression<TDelegate>(body, readOnlyCollection);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.LambdaExpression" /> and can be used when the delegate type is not known at compile time.</summary>
    /// <returns>An object that represents a lambda expression which has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
    /// <param name="delegateType">A <see cref="T:System.Type" /> that represents a delegate type.</param>
    /// <param name="body">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
    /// <param name="parameters">An array of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="delegateType" /> or <paramref name="body" /> is null.-or-One or more elements in <paramref name="parameters" /> are null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="delegateType" /> does not represent a delegate type.-or-<paramref name="body" />.Type represents a type that is not assignable to the return type of the delegate type represented by <paramref name="delegateType" />.-or-<paramref name="parameters" /> does not contain the same number of elements as the list of parameters for the delegate type represented by <paramref name="delegateType" />.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" /> is not assignable from the type of the corresponding parameter type of the delegate type represented by <paramref name="delegateType" />.</exception>
    public static LambdaExpression Lambda(
        Type delegateType,
        Expression body,
        params ParameterExpression[] parameters)
    {
        return Lambda(delegateType, body, parameters.ToReadOnlyCollection<ParameterExpression>());
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.LambdaExpression" /> and can be used when the delegate type is not known at compile time.</summary>
    /// <returns>An object that represents a lambda expression which has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
    /// <param name="delegateType">A <see cref="T:System.Type" /> that represents a delegate type.</param>
    /// <param name="body">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
    /// <param name="parameters">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="delegateType" /> or <paramref name="body" /> is null.-or-One or more elements in <paramref name="parameters" /> are null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="delegateType" /> does not represent a delegate type.-or-<paramref name="body" />.Type represents a type that is not assignable to the return type of the delegate type represented by <paramref name="delegateType" />.-or-<paramref name="parameters" /> does not contain the same number of elements as the list of parameters for the delegate type represented by <paramref name="delegateType" />.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" /> is not assignable from the type of the corresponding parameter type of the delegate type represented by <paramref name="delegateType" />.</exception>
    public static LambdaExpression Lambda(
        Type delegateType,
        Expression body,
        IEnumerable<ParameterExpression> parameters)
    {
        if (delegateType == null)
            throw Error.ArgumentNull(nameof(delegateType));
        if (body == null)
            throw Error.ArgumentNull(nameof(body));
        var readOnlyCollection = parameters.ToReadOnlyCollection<ParameterExpression>();
        ValidateLambdaArgs(delegateType, ref body, readOnlyCollection);
        return (LambdaExpression)typeof(Expression).GetMethod(nameof(Lambda), BindingFlags.Static | BindingFlags.Public, null, lambdaTypes, null).MakeGenericMethod(delegateType).Invoke(null, new object[2]
        {
            body,
            readOnlyCollection
        });
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.LambdaExpression" /> by first constructing a delegate type.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Lambda" /> and the <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.</returns>
    /// <param name="body">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.</param>
    /// <param name="parameters">An array of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="body" /> is null.-or-One or more elements of <paramref name="parameters" /> are null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="parameters" /> contains more than four elements.</exception>
    public static LambdaExpression Lambda(
        Expression body,
        params ParameterExpression[] parameters)
    {
        if (body == null)
            throw Error.ArgumentNull(nameof(body));
        var flag = body.Type == typeof(void);
        var index1 = parameters == null ? 0 : parameters.Length;
        var typeArray = new Type[index1 + (flag ? 0 : 1)];
        for (var index2 = 0; index2 < index1; ++index2)
        {
            if (parameters[index2] == null)
                throw Error.ArgumentNull("parameter");
            typeArray[index2] = parameters[index2].Type;
        }
        Type delegateType;
        if (flag)
        {
            delegateType = GetActionType(typeArray);
        }
        else
        {
            typeArray[index1] = body.Type;
            delegateType = GetFuncType(typeArray);
        }
        return Lambda(delegateType, body, parameters);
    }

    /// <summary>Creates a <see cref="T:System.Type" /> object that represents a generic System.Func delegate type that has specific type arguments.</summary>
    /// <returns>The type of a System.Func delegate that has the specified type arguments.</returns>
    /// <param name="typeArgs">An array of one to five <see cref="T:System.Type" /> objects that specify the type arguments for the System.Func delegate type.</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="typeArgs" /> contains less than one or more than five elements.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="typeArgs" /> is null.</exception>
    public static Type GetFuncType(params Type[] typeArgs)
    {
        if (typeArgs == null)
            throw Error.ArgumentNull(nameof(typeArgs));
        if (typeArgs.Length < 1 || typeArgs.Length > 5)
            throw Error.IncorrectNumberOfTypeArgsForFunc();
        return funcTypes[typeArgs.Length - 1].MakeGenericType(typeArgs);
    }

    /// <summary>Creates a <see cref="T:System.Type" /> object that represents a generic System.Action delegate type that has specific type arguments.</summary>
    /// <returns>The type of a System.Action delegate that has the specified type arguments.</returns>
    /// <param name="typeArgs">An array of zero to four <see cref="T:System.Type" /> objects that specify the type arguments for the System.Action delegate type.</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="typeArgs" /> contains more than four elements.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="typeArgs" /> is null.</exception>
    public static Type GetActionType(params Type[] typeArgs)
    {
        if (typeArgs == null)
            throw Error.ArgumentNull(nameof(typeArgs));
        if (typeArgs.Length >= actionTypes.Length)
            throw Error.IncorrectNumberOfTypeArgsForAction();
        return typeArgs.Length == 0 ? actionTypes[typeArgs.Length] : actionTypes[typeArgs.Length].MakeGenericType(typeArgs);
    }

    private static void ValidateLambdaArgs(
        Type delegateType,
        ref Expression body,
        ReadOnlyCollection<ParameterExpression> parameters)
    {
        if (delegateType == null)
            throw Error.ArgumentNull(nameof(delegateType));
        if (body == null)
            throw Error.ArgumentNull(nameof(body));
        var methodInfo = AreAssignable(typeof(Delegate), delegateType) && delegateType != typeof(Delegate) ? delegateType.GetMethod("Invoke") : throw Error.LambdaTypeMustBeDerivedFromSystemDelegate();
        var parameters1 = methodInfo.GetParameters();
        if (parameters1.Length > 0)
        {
            if (parameters1.Length != parameters.Count)
                throw Error.IncorrectNumberOfLambdaDeclarationParameters();
            var index = 0;
            for (var length = parameters1.Length; index < length; ++index)
            {
                Expression parameter = parameters[index];
                var parameterInfo = parameters1[index];
                if (parameter == null)
                    throw Error.ArgumentNull(nameof(parameters));
                var parameterType = parameterInfo.ParameterType;
                if (parameterType.IsByRef || parameter.Type.IsByRef)
                    throw Error.ExpressionMayNotContainByrefParameters();
                if (!AreReferenceAssignable(parameter.Type, parameterType))
                    throw Error.ParameterExpressionNotValidAsDelegate(parameter.Type, parameterType);
            }
        }
        else if (parameters.Count > 0)
            throw Error.IncorrectNumberOfLambdaDeclarationParameters();
        if (methodInfo.ReturnType == typeof(void) || AreReferenceAssignable(methodInfo.ReturnType, body.Type))
            return;
        if (!IsSameOrSubclass(typeof(Expression), methodInfo.ReturnType) || !AreAssignable(methodInfo.ReturnType, body.GetType()))
            throw Error.ExpressionTypeDoesNotMatchReturn(body.Type, methodInfo.ReturnType);
        body = Quote(body);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.LeftShift" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The left-shift operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression LeftShift(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return IsInteger(left.Type) && GetNonNullableType(right.Type) == typeof(int) ? new BinaryExpression(ExpressionType.LeftShift, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.LeftShift, "op_LeftShift", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift operation. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.LeftShift" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the left-shift operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression LeftShift(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? LeftShift(left, right) : GetMethodBasedBinaryOperator(ExpressionType.LeftShift, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "less than" numeric comparison.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.LessThan" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The "less than" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression LessThan(Expression left, Expression right) => LessThan(left, right, false, null);

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "less than" numeric comparison. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.LessThan" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="liftToNull">true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true; false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the "less than" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression LessThan(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? GetComparisonOperator(ExpressionType.LessThan, "op_LessThan", left, right, liftToNull) : GetMethodBasedBinaryOperator(ExpressionType.LessThan, left, right, method, liftToNull);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a " less than or equal" numeric comparison.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.LessThanOrEqual" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The "less than or equal" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression LessThanOrEqual(
        Expression left,
        Expression right)
    {
        return LessThanOrEqual(left, right, false, null);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a " less than or equal" numeric comparison. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.LessThanOrEqual" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="liftToNull">true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true; false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the "less than or equal" operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression LessThanOrEqual(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? GetComparisonOperator(ExpressionType.LessThanOrEqual, "op_LessThanOrEqual", left, right, liftToNull) : GetMethodBasedBinaryOperator(ExpressionType.LessThanOrEqual, left, right, method, liftToNull);
    }

    internal static void ValidateLift(
        IEnumerable<ParameterExpression> parameters,
        IEnumerable<Expression> arguments)
    {
        var readOnlyCollection1 = parameters.ToReadOnlyCollection<ParameterExpression>();
        var readOnlyCollection2 = arguments.ToReadOnlyCollection<Expression>();
        if (readOnlyCollection1.Count != readOnlyCollection2.Count)
            throw Error.IncorrectNumberOfIndexes();
        var index = 0;
        for (var count = readOnlyCollection1.Count; index < count; ++index)
        {
            if (!AreReferenceAssignable(readOnlyCollection1[index].Type, GetNonNullableType(readOnlyCollection2[index].Type)))
                throw Error.ArgumentTypesMustMatch();
        }
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses a method named "Add" to add elements to a collection.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.</returns>
    /// <param name="newExpression">A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
    /// <param name="initializers">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of <paramref name="initializers" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">There is no instance method named "Add" (case insensitive) declared in <paramref name="newExpression" />.Type or its base type.-or-The add method on <paramref name="newExpression" />.Type or its base type does not take exactly one argument.-or-The type represented by the <see cref="P:System.Linq.Expressions.Expression.Type" /> property of the first element of <paramref name="initializers" /> is not assignable to the argument type of the add method on <paramref name="newExpression" />.Type or its base type.-or-More than one argument-compatible method named "Add" (case-insensitive) exists on <paramref name="newExpression" />.Type and/or its base type.</exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        params Expression[] initializers)
    {
        if (newExpression == null)
            throw Error.ArgumentNull(nameof(newExpression));
        return initializers != null ? ListInit(newExpression, (IEnumerable<Expression>)initializers) : throw Error.ArgumentNull(nameof(initializers));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses a method named "Add" to add elements to a collection.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.</returns>
    /// <param name="newExpression">A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
    /// <param name="initializers">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of <paramref name="initializers" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">There is no instance method named "Add" (case insensitive) declared in <paramref name="newExpression" />.Type or its base type.-or-The add method on <paramref name="newExpression" />.Type or its base type does not take exactly one argument.-or-The type represented by the <see cref="P:System.Linq.Expressions.Expression.Type" /> property of the first element of <paramref name="initializers" /> is not assignable to the argument type of the add method on <paramref name="newExpression" />.Type or its base type.-or-More than one argument-compatible method named "Add" (case-insensitive) exists on <paramref name="newExpression" />.Type and/or its base type.</exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        IEnumerable<Expression> initializers)
    {
        if (newExpression == null)
            throw Error.ArgumentNull(nameof(newExpression));
        if (initializers == null)
            throw Error.ArgumentNull(nameof(initializers));
        if (!initializers.Any<Expression>())
            throw Error.ListInitializerWithZeroMembers();
        var method = FindMethod(newExpression.Type, "Add", null, new Expression[1]
        {
            initializers.First<Expression>()
        }, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return ListInit(newExpression, method, initializers);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses a specified method to add elements to a collection.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.</returns>
    /// <param name="newExpression">A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
    /// <param name="addMethod">A <see cref="T:System.Reflection.MethodInfo" /> that represents an instance method that takes one argument, that adds an element to a collection.</param>
    /// <param name="initializers">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of <paramref name="initializers" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.-or-<paramref name="addMethod" /> is not null and it does not represent an instance method named "Add" (case insensitive) that takes exactly one argument.-or-<paramref name="addMethod" /> is not null and the type represented by the <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="initializers" /> is not assignable to the argument type of the method that <paramref name="addMethod" /> represents.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="addMethod" /> is null and no instance method named "Add" that takes one type-compatible argument exists on <paramref name="newExpression" />.Type or its base type.</exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        MethodInfo addMethod,
        params Expression[] initializers)
    {
        if (newExpression == null)
            throw Error.ArgumentNull(nameof(newExpression));
        if (initializers == null)
            throw Error.ArgumentNull(nameof(initializers));
        return addMethod == null ? ListInit(newExpression, (IEnumerable<Expression>)initializers) : ListInit(newExpression, addMethod, (IEnumerable<Expression>)initializers);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses a specified method to add elements to a collection.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.</returns>
    /// <param name="newExpression">A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
    /// <param name="addMethod">A <see cref="T:System.Reflection.MethodInfo" /> that represents an instance method named "Add" (case insensitive), that adds an element to a collection.</param>
    /// <param name="initializers">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of <paramref name="initializers" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.-or-<paramref name="addMethod" /> is not null and it does not represent an instance method named "Add" (case insensitive) that takes exactly one argument.-or-<paramref name="addMethod" /> is not null and the type represented by the <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="initializers" /> is not assignable to the argument type of the method that <paramref name="addMethod" /> represents.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="addMethod" /> is null and no instance method named "Add" that takes one type-compatible argument exists on <paramref name="newExpression" />.Type or its base type.</exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        MethodInfo addMethod,
        IEnumerable<Expression> initializers)
    {
        if (newExpression == null)
            throw Error.ArgumentNull(nameof(newExpression));
        if (initializers == null)
            throw Error.ArgumentNull(nameof(initializers));
        if (!initializers.Any<Expression>())
            throw Error.ListInitializerWithZeroMembers();
        if (addMethod == null)
            return ListInit(newExpression, initializers);
        var initializers1 = new List<ElementInit>();
        foreach (var initializer in initializers)
            initializers1.Add(ElementInit(addMethod, initializer));
        return ListInit(newExpression, initializers1);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses specified <see cref="T:System.Linq.Expressions.ElementInit" /> objects to initialize a collection.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> and <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> properties set to the specified values.</returns>
    /// <param name="newExpression">A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
    /// <param name="initializers">An array of <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of <paramref name="initializers" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        params ElementInit[] initializers)
    {
        if (newExpression == null)
            throw Error.ArgumentNull(nameof(newExpression));
        return initializers != null ? ListInit(newExpression, initializers.ToReadOnlyCollection<ElementInit>()) : throw Error.ArgumentNull(nameof(initializers));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses specified <see cref="T:System.Linq.Expressions.ElementInit" /> objects to initialize a collection.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> and <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> properties set to the specified values.</returns>
    /// <param name="newExpression">A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.</param>
    /// <param name="initializers">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of <paramref name="initializers" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        IEnumerable<ElementInit> initializers)
    {
        if (newExpression == null)
            throw Error.ArgumentNull(nameof(newExpression));
        if (initializers == null)
            throw Error.ArgumentNull(nameof(initializers));
        var initializers1 = initializers.Any<ElementInit>() ? initializers.ToReadOnlyCollection<ElementInit>() : throw Error.ListInitializerWithZeroMembers();
        ValidateListInitArgs(newExpression.Type, initializers1);
        return new ListInitExpression(newExpression, initializers1);
    }

    /// <summary>Creates an <see cref="T:System.Linq.Expressions.ElementInit" />, given an array of values as the second argument.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.ElementInit" /> that has the <see cref="P:System.Linq.Expressions.ElementInit.AddMethod" /> and <see cref="P:System.Linq.Expressions.ElementInit.Arguments" /> properties set to the specified values.</returns>
    /// <param name="addMethod">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.ElementInit.AddMethod" /> property equal to.</param>
    /// <param name="arguments">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to set the <see cref="P:System.Linq.Expressions.ElementInit.Arguments" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="addMethod" /> or <paramref name="arguments" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The method that <paramref name="addMethod" /> represents is not named "Add" (case insensitive).-or-The method that <paramref name="addMethod" /> represents is not an instance method.-or-<paramref name="arguments" /> does not contain the same number of elements as the number of parameters for the method that <paramref name="addMethod" /> represents.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the method that <paramref name="addMethod" /> represents.</exception>
    public static ElementInit ElementInit(
        MethodInfo addMethod,
        params Expression[] arguments)
    {
        return ElementInit(addMethod, (IEnumerable<Expression>)arguments);
    }

    /// <summary>Creates an <see cref="T:System.Linq.Expressions.ElementInit" />, given an <see cref="T:System.Collections.Generic.IEnumerable`1" /> as the second argument.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.ElementInit" /> that has the <see cref="P:System.Linq.Expressions.ElementInit.AddMethod" /> and <see cref="P:System.Linq.Expressions.ElementInit.Arguments" /> properties set to the specified values.</returns>
    /// <param name="addMethod">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.ElementInit.AddMethod" /> property equal to.</param>
    /// <param name="arguments">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to set the <see cref="P:System.Linq.Expressions.ElementInit.Arguments" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="addMethod" /> or <paramref name="arguments" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The method that <paramref name="addMethod" /> represents is not named "Add" (case insensitive).-or-The method that <paramref name="addMethod" /> represents is not an instance method.-or-<paramref name="arguments" /> does not contain the same number of elements as the number of parameters for the method that <paramref name="addMethod" /> represents.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the method that <paramref name="addMethod" /> represents.</exception>
    public static ElementInit ElementInit(
        MethodInfo addMethod,
        IEnumerable<Expression> arguments)
    {
        if (addMethod == null)
            throw Error.ArgumentNull(nameof(addMethod));
        if (arguments == null)
            throw Error.ArgumentNull(nameof(arguments));
        ValidateElementInitAddMethodInfo(addMethod);
        var readOnlyCollection = arguments.ToReadOnlyCollection<Expression>();
        ValidateArgumentTypes(addMethod, ref readOnlyCollection);
        return new ElementInit(addMethod, readOnlyCollection);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> where the member is a field or property.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" /> and the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> properties set to the specified values.</returns>
    /// <param name="member">A <see cref="T:System.Reflection.MemberInfo" /> that represents a field or property to set the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
    /// <param name="initializers">An array of <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="member" /> is null. -or-One or more elements of <paramref name="initializers" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="member" /> does not represent a field or property.-or-The <see cref="P:System.Reflection.FieldInfo.FieldType" /> or <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the field or property that <paramref name="member" /> represents does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>
    public static MemberListBinding ListBind(
        MemberInfo member,
        params ElementInit[] initializers)
    {
        if (member == null)
            throw Error.ArgumentNull(nameof(member));
        return initializers != null ? ListBind(member, initializers.ToReadOnlyCollection<ElementInit>()) : throw Error.ArgumentNull(nameof(initializers));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> where the member is a field or property.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" /> and the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> properties set to the specified values.</returns>
    /// <param name="member">A <see cref="T:System.Reflection.MemberInfo" /> that represents a field or property to set the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
    /// <param name="initializers">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="member" /> is null. -or-One or more elements of <paramref name="initializers" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="member" /> does not represent a field or property.-or-The <see cref="P:System.Reflection.FieldInfo.FieldType" /> or <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the field or property that <paramref name="member" /> represents does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>
    public static MemberListBinding ListBind(
        MemberInfo member,
        IEnumerable<ElementInit> initializers)
    {
        if (member == null)
            throw Error.ArgumentNull(nameof(member));
        if (initializers == null)
            throw Error.ArgumentNull(nameof(initializers));
        Type memberType;
        ValidateGettableFieldOrPropertyMember(member, out memberType);
        var readOnlyCollection = initializers.ToReadOnlyCollection<ElementInit>();
        ValidateListInitArgs(memberType, readOnlyCollection);
        return new MemberListBinding(member, readOnlyCollection);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> object based on a specified property accessor method.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" />, the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="T:System.Reflection.MemberInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> populated with the elements of <paramref name="initializers" />.</returns>
    /// <param name="propertyAccessor">A <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
    /// <param name="initializers">An array of <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="propertyAccessor" /> is null. -or-One or more elements of <paramref name="initializers" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the property that the method represented by <paramref name="propertyAccessor" /> accesses does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>
    public static MemberListBinding ListBind(
        MethodInfo propertyAccessor,
        params ElementInit[] initializers)
    {
        if (propertyAccessor == null)
            throw Error.ArgumentNull(nameof(propertyAccessor));
        return initializers != null ? ListBind(propertyAccessor, initializers.ToReadOnlyCollection<ElementInit>()) : throw Error.ArgumentNull(nameof(initializers));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> based on a specified property accessor method.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" />, the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="T:System.Reflection.MemberInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> populated with the elements of <paramref name="initializers" />.</returns>
    /// <param name="propertyAccessor">A <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
    /// <param name="initializers">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="propertyAccessor" /> is null. -or-One or more elements of <paramref name="initializers" /> are null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the property that the method represented by <paramref name="propertyAccessor" /> accesses does not implement <see cref="T:System.Collections.IEnumerable" />.</exception>
    public static MemberListBinding ListBind(
        MethodInfo propertyAccessor,
        IEnumerable<ElementInit> initializers)
    {
        if (propertyAccessor == null)
            throw Error.ArgumentNull(nameof(propertyAccessor));
        return initializers != null ? ListBind(GetProperty(propertyAccessor), initializers) : throw Error.ArgumentNull(nameof(initializers));
    }

    private static void ValidateListInitArgs(
        Type listType,
        ReadOnlyCollection<ElementInit> initializers)
    {
        if (!AreAssignable(typeof(IEnumerable), listType))
            throw Error.TypeNotIEnumerable(listType);
        var index = 0;
        for (var count = initializers.Count; index < count; ++index)
            ValidateCallInstanceType(listType, (initializers[index] ?? throw Error.ArgumentNull(nameof(initializers))).AddMethod);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberInitExpression" />.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberInitExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.MemberInit" /> and the <see cref="P:System.Linq.Expressions.MemberInitExpression.NewExpression" /> and <see cref="P:System.Linq.Expressions.MemberInitExpression.Bindings" /> properties set to the specified values.</returns>
    /// <param name="newExpression">A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the <see cref="P:System.Linq.Expressions.MemberInitExpression.NewExpression" /> property equal to.</param>
    /// <param name="bindings">An array of <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberInitExpression.Bindings" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="newExpression" /> or <paramref name="bindings" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type that <paramref name="newExpression" />.Type represents.</exception>
    public static MemberInitExpression MemberInit(
        NewExpression newExpression,
        params MemberBinding[] bindings)
    {
        if (newExpression == null)
            throw Error.ArgumentNull(nameof(newExpression));
        return bindings != null ? MemberInit(newExpression, bindings.ToReadOnlyCollection<MemberBinding>()) : throw Error.ArgumentNull(nameof(bindings));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberInitExpression" />.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberInitExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.MemberInit" /> and the <see cref="P:System.Linq.Expressions.MemberInitExpression.NewExpression" /> and <see cref="P:System.Linq.Expressions.MemberInitExpression.Bindings" /> properties set to the specified values.</returns>
    /// <param name="newExpression">A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the <see cref="P:System.Linq.Expressions.MemberInitExpression.NewExpression" /> property equal to.</param>
    /// <param name="bindings">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberInitExpression.Bindings" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="newExpression" /> or <paramref name="bindings" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type that <paramref name="newExpression" />.Type represents.</exception>
    public static MemberInitExpression MemberInit(
        NewExpression newExpression,
        IEnumerable<MemberBinding> bindings)
    {
        if (newExpression == null)
            throw Error.ArgumentNull(nameof(newExpression));
        var bindings1 = bindings != null ? bindings.ToReadOnlyCollection<MemberBinding>() : throw Error.ArgumentNull(nameof(bindings));
        ValidateMemberInitArgs(newExpression.Type, bindings1);
        return new MemberInitExpression(newExpression, bindings1);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive initialization of members of a field or property.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.MemberBinding" /> and the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> properties set to the specified values.</returns>
    /// <param name="member">The <see cref="T:System.Reflection.MemberInfo" /> to set the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
    /// <param name="bindings">An array of <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="member" /> or <paramref name="bindings" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="member" /> does not represent a field or property.-or-The <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type of the field or property that <paramref name="member" /> represents.</exception>
    public static MemberMemberBinding MemberBind(
        MemberInfo member,
        params MemberBinding[] bindings)
    {
        if (member == null)
            throw Error.ArgumentNull(nameof(member));
        return bindings != null ? MemberBind(member, bindings.ToReadOnlyCollection<MemberBinding>()) : throw Error.ArgumentNull(nameof(bindings));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive initialization of members of a field or property.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.MemberBinding" /> and the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> properties set to the specified values.</returns>
    /// <param name="member">The <see cref="T:System.Reflection.MemberInfo" /> to set the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.</param>
    /// <param name="bindings">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="member" /> or <paramref name="bindings" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="member" /> does not represent a field or property.-or-The <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type of the field or property that <paramref name="member" /> represents.</exception>
    public static MemberMemberBinding MemberBind(
        MemberInfo member,
        IEnumerable<MemberBinding> bindings)
    {
        if (member == null)
            throw Error.ArgumentNull(nameof(member));
        var bindings1 = bindings != null ? bindings.ToReadOnlyCollection<MemberBinding>() : throw Error.ArgumentNull(nameof(bindings));
        Type memberType;
        ValidateGettableFieldOrPropertyMember(member, out memberType);
        ValidateMemberInitArgs(memberType, bindings1);
        return new MemberMemberBinding(member, bindings1);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive initialization of members of a member that is accessed by using a property accessor method.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.MemberBinding" />, the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="T:System.Reflection.PropertyInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> properties set to the specified values.</returns>
    /// <param name="propertyAccessor">The <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
    /// <param name="bindings">An array of <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="propertyAccessor" /> or <paramref name="bindings" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type of the property accessed by the method that <paramref name="propertyAccessor" /> represents.</exception>
    public static MemberMemberBinding MemberBind(
        MethodInfo propertyAccessor,
        params MemberBinding[] bindings)
    {
        return propertyAccessor != null ? MemberBind(GetProperty(propertyAccessor), bindings) : throw Error.ArgumentNull(nameof(propertyAccessor));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive initialization of members of a member that is accessed by using a property accessor method.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that has the <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to <see cref="F:System.Linq.Expressions.MemberBindingType.MemberBinding" />, the <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the <see cref="T:System.Reflection.PropertyInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />, and <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> properties set to the specified values.</returns>
    /// <param name="propertyAccessor">The <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
    /// <param name="bindings">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate the <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="propertyAccessor" /> or <paramref name="bindings" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property of an element of <paramref name="bindings" /> does not represent a member of the type of the property accessed by the method that <paramref name="propertyAccessor" /> represents.</exception>
    public static MemberMemberBinding MemberBind(
        MethodInfo propertyAccessor,
        IEnumerable<MemberBinding> bindings)
    {
        return propertyAccessor != null ? MemberBind(GetProperty(propertyAccessor), bindings) : throw Error.ArgumentNull(nameof(propertyAccessor));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic remainder operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Modulo" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The modulus operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Modulo(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return left.Type == right.Type && IsArithmetic(left.Type) ? new BinaryExpression(ExpressionType.Modulo, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Modulo, "op_Modulus", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic remainder operation. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Modulo" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the modulus operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Modulo(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? Modulo(left, right) : GetMethodBasedBinaryOperator(ExpressionType.Modulo, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic multiplication operation that does not have overflow checking.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Multiply" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The multiplication operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Multiply(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return left.Type == right.Type && IsArithmetic(left.Type) ? new BinaryExpression(ExpressionType.Multiply, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Multiply, "op_Multiply", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic multiplication operation that does not have overflow checking and for which the implementing method is specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Multiply" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the multiplication operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Multiply(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? Multiply(left, right) : GetMethodBasedBinaryOperator(ExpressionType.Multiply, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic multiplication operation that has overflow checking.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.MultiplyChecked" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The multiplication operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression MultiplyChecked(
        Expression left,
        Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return left.Type == right.Type && IsArithmetic(left.Type) ? new BinaryExpression(ExpressionType.MultiplyChecked, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.MultiplyChecked, "op_Multiply", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic multiplication operation that has overflow checking. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.MultiplyChecked" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the multiplication operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression MultiplyChecked(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? MultiplyChecked(left, right) : GetMethodBasedBinaryOperator(ExpressionType.MultiplyChecked, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a unary plus operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.UnaryPlus" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The unary plus operator is not defined for <paramref name="expression" />.Type.</exception>
    public static UnaryExpression UnaryPlus(Expression expression)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return IsArithmetic(expression.Type) ? new UnaryExpression(ExpressionType.UnaryPlus, expression, expression.Type) : GetUserDefinedUnaryOperatorOrThrow(ExpressionType.UnaryPlus, "op_UnaryPlus", expression);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a unary plus operation. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.UnaryPlus" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the unary plus operator is not defined for <paramref name="expression" />.Type.-or-<paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by <paramref name="method" />.</exception>
    public static UnaryExpression UnaryPlus(Expression expression, MethodInfo method)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return method == null ? UnaryPlus(expression) : GetMethodBasedUnaryOperator(ExpressionType.UnaryPlus, expression, method);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Negate" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The unary minus operator is not defined for <paramref name="expression" />.Type.</exception>
    public static UnaryExpression Negate(Expression expression)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return IsArithmetic(expression.Type) && !IsUnSigned(expression.Type) ? new UnaryExpression(ExpressionType.Negate, expression, expression.Type) : GetUserDefinedUnaryOperatorOrThrow(ExpressionType.Negate, "op_UnaryNegation", expression);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation operation. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Negate" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the unary minus operator is not defined for <paramref name="expression" />.Type.-or-<paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by <paramref name="method" />.</exception>
    public static UnaryExpression Negate(Expression expression, MethodInfo method)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return method == null ? Negate(expression) : GetMethodBasedUnaryOperator(ExpressionType.Negate, expression, method);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation operation that has overflow checking.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.NegateChecked" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The unary minus operator is not defined for <paramref name="expression" />.Type.</exception>
    public static UnaryExpression NegateChecked(Expression expression)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return IsArithmetic(expression.Type) && !IsUnSigned(expression.Type) ? new UnaryExpression(ExpressionType.NegateChecked, expression, expression.Type) : GetUserDefinedUnaryOperatorOrThrow(ExpressionType.NegateChecked, "op_UnaryNegation", expression);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation operation that has overflow checking. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.NegateChecked" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the unary minus operator is not defined for <paramref name="expression" />.Type.-or-<paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by <paramref name="method" />.</exception>
    public static UnaryExpression NegateChecked(
        Expression expression,
        MethodInfo method)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return method == null ? NegateChecked(expression) : GetMethodBasedUnaryOperator(ExpressionType.NegateChecked, expression, method);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an inequality comparison.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.NotEqual" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The inequality operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression NotEqual(Expression left, Expression right) => NotEqual(left, right, false, null);

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an inequality comparison. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.NotEqual" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="liftToNull">true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true; false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the inequality operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression NotEqual(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? GetEqualityComparisonOperator(ExpressionType.NotEqual, "op_Inequality", left, right, liftToNull) : GetMethodBasedBinaryOperator(ExpressionType.NotEqual, left, right, method, liftToNull);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the specified constructor with the specified arguments.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> and <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> properties set to the specified values.</returns>
    /// <param name="constructor">The <see cref="T:System.Reflection.ConstructorInfo" /> to set the <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property equal to.</param>
    /// <param name="arguments">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="constructor" /> is null.-or-An element of <paramref name="arguments" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The length of <paramref name="arguments" /> does match the number of parameters for the constructor that <paramref name="constructor" /> represents.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the constructor that <paramref name="constructor" /> represents.</exception>
    public static NewExpression New(
        ConstructorInfo constructor,
        params Expression[] arguments)
    {
        return New(constructor, arguments.ToReadOnlyCollection<Expression>());
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the specified constructor with the specified arguments.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> and <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> properties set to the specified values.</returns>
    /// <param name="constructor">The <see cref="T:System.Reflection.ConstructorInfo" /> to set the <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property equal to.</param>
    /// <param name="arguments">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="constructor" /> is null.-or-An element of <paramref name="arguments" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The <paramref name="arguments" /> parameter does not contain the same number of elements as the number of parameters for the constructor that <paramref name="constructor" /> represents.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the constructor that <paramref name="constructor" /> represents.</exception>
    public static NewExpression New(
        ConstructorInfo constructor,
        IEnumerable<Expression> arguments)
    {
        if (constructor == null)
            throw Error.ArgumentNull(nameof(constructor));
        var readOnlyCollection = arguments.ToReadOnlyCollection<Expression>();
        ValidateNewArgs(constructor.DeclaringType, constructor, ref readOnlyCollection);
        return new NewExpression(constructor.DeclaringType, constructor, readOnlyCollection);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the specified constructor with the specified arguments. The members that access the constructor initialized fields are specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the <see cref="P:System.Linq.Expressions.NewExpression.Constructor" />, <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> and <see cref="P:System.Linq.Expressions.NewExpression.Members" /> properties set to the specified values.</returns>
    /// <param name="constructor">The <see cref="T:System.Reflection.ConstructorInfo" /> to set the <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property equal to.</param>
    /// <param name="arguments">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> collection.</param>
    /// <param name="members">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Reflection.MemberInfo" /> objects to use to populate the <see cref="P:System.Linq.Expressions.NewExpression.Members" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="constructor" /> is null.-or-An element of <paramref name="arguments" /> is null.-or-An element of <paramref name="members" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The <paramref name="arguments" /> parameter does not contain the same number of elements as the number of parameters for the constructor that <paramref name="constructor" /> represents.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the constructor that <paramref name="constructor" /> represents.-or-The <paramref name="members" /> parameter does not have the same number of elements as <paramref name="arguments" />.-or-An element of <paramref name="arguments" /> has a <see cref="P:System.Linq.Expressions.Expression.Type" /> property that represents a type that is not assignable to the type of the member that is represented by the corresponding element of <paramref name="members" />.-or-An element of <paramref name="members" /> represents a property that does not have a get accessor.</exception>
    public static NewExpression New(
        ConstructorInfo constructor,
        IEnumerable<Expression> arguments,
        IEnumerable<MemberInfo> members)
    {
        if (constructor == null)
            throw Error.ArgumentNull(nameof(constructor));
        var readOnlyCollection1 = members.ToReadOnlyCollection<MemberInfo>();
        var readOnlyCollection2 = arguments.ToReadOnlyCollection<Expression>();
        ValidateNewArgs(constructor, ref readOnlyCollection2, readOnlyCollection1);
        return new NewExpression(constructor.DeclaringType, constructor, readOnlyCollection2, readOnlyCollection1);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the specified constructor with the specified arguments. The members that access the constructor initialized fields are specified as an array.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the <see cref="P:System.Linq.Expressions.NewExpression.Constructor" />, <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> and <see cref="P:System.Linq.Expressions.NewExpression.Members" /> properties set to the specified values.</returns>
    /// <param name="constructor">The <see cref="T:System.Reflection.ConstructorInfo" /> to set the <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property equal to.</param>
    /// <param name="arguments">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> collection.</param>
    /// <param name="members">An array of <see cref="T:System.Reflection.MemberInfo" /> objects to use to populate the <see cref="P:System.Linq.Expressions.NewExpression.Members" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="constructor" /> is null.-or-An element of <paramref name="arguments" /> is null.-or-An element of <paramref name="members" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The <paramref name="arguments" /> parameter does not contain the same number of elements as the number of parameters for the constructor that <paramref name="constructor" /> represents.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the constructor that <paramref name="constructor" /> represents.-or-The <paramref name="members" /> parameter does not have the same number of elements as <paramref name="arguments" />.-or-An element of <paramref name="arguments" /> has a <see cref="P:System.Linq.Expressions.Expression.Type" /> property that represents a type that is not assignable to the type of the member that is represented by the corresponding element of <paramref name="members" />.-or-An element of <paramref name="members" /> represents a property that does not have a get accessor.</exception>
    public static NewExpression New(
        ConstructorInfo constructor,
        IEnumerable<Expression> arguments,
        params MemberInfo[] members)
    {
        return New(constructor, arguments, members.ToReadOnlyCollection<MemberInfo>());
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the specified constructor that takes no arguments.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property set to the specified value.</returns>
    /// <param name="constructor">The <see cref="T:System.Reflection.ConstructorInfo" /> to set the <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="constructor" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The constructor that <paramref name="constructor" /> represents has at least one parameter.</exception>
    public static NewExpression New(ConstructorInfo constructor) => New(constructor, ((IEnumerable<Expression>)null).ToReadOnlyCollection<Expression>());

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the parameterless constructor of the specified type.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property set to the <see cref="T:System.Reflection.ConstructorInfo" /> that represents the parameterless constructor of the specified type.</returns>
    /// <param name="type">A <see cref="T:System.Type" /> that has a constructor that takes no arguments.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="type" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The type that <paramref name="type" /> represents does not have a parameterless constructor.</exception>
    public static NewExpression New(Type type)
    {
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        if (type == typeof(void))
            throw Error.ArgumentCannotBeOfTypeVoid();
        if (!type.IsValueType)
            return New(type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) ?? throw Error.TypeMissingDefaultConstructor(type));
        var readOnlyCollection = ((IEnumerable<Expression>)null).ToReadOnlyCollection<Expression>();
        return new NewExpression(type, null, readOnlyCollection);
    }

    private static void ValidateNewArgs(
        ConstructorInfo constructor,
        ref ReadOnlyCollection<Expression> arguments,
        ReadOnlyCollection<MemberInfo> members)
    {
        ParameterInfo[] parameters;
        if ((parameters = constructor.GetParameters()).Length > 0)
        {
            if (arguments.Count != parameters.Length)
                throw Error.IncorrectNumberOfConstructorArguments();
            if (arguments.Count != members.Count)
                throw Error.IncorrectNumberOfArgumentsForMembers();
            List<Expression> sequence = null;
            var index1 = 0;
            for (var count = arguments.Count; index1 < count; ++index1)
            {
                var expression = arguments[index1];
                if (expression == null)
                    throw Error.ArgumentNull("argument");
                var member = members[index1];
                if (member == null)
                    throw Error.ArgumentNull("member");
                if (member.DeclaringType != constructor.DeclaringType)
                    throw Error.ArgumentMemberNotDeclOnType(member.Name, constructor.DeclaringType.Name);
                Type memberType;
                ValidateAnonymousTypeMember(member, out memberType);
                if (!AreReferenceAssignable(expression.Type, memberType))
                    expression = IsSameOrSubclass(typeof(Expression), memberType) && AreAssignable(memberType, expression.GetType()) ? (Expression)Quote(expression) : throw Error.ArgumentTypeDoesNotMatchMember(expression.Type, memberType);
                var type = parameters[index1].ParameterType;
                if (type.IsByRef)
                    type = type.GetElementType();
                if (!AreReferenceAssignable(type, expression.Type))
                {
                    if (!IsSameOrSubclass(typeof(Expression), type) || !AreAssignable(type, expression.Type))
                        throw Error.ExpressionTypeDoesNotMatchConstructorParameter(expression.Type, type);
                    expression = Quote(expression);
                }
                if (sequence == null && expression != arguments[index1])
                {
                    sequence = new List<Expression>(arguments.Count);
                    for (var index2 = 0; index2 < index1; ++index2)
                        sequence.Add(arguments[index2]);
                }
                sequence?.Add(expression);
            }
            if (sequence == null)
                return;
            arguments = sequence.ToReadOnlyCollection<Expression>();
        }
        else
        {
            if (arguments != null && arguments.Count > 0)
                throw Error.IncorrectNumberOfConstructorArguments();
            if (members != null && members.Count > 0)
                throw Error.IncorrectNumberOfMembersForGivenConstructor();
        }
    }

    private static void ValidateNewArgs(
        Type type,
        ConstructorInfo constructor,
        ref ReadOnlyCollection<Expression> arguments)
    {
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        if (!type.IsValueType && constructor == null)
            throw Error.ArgumentNull(nameof(constructor));
        ParameterInfo[] parameters;
        if (constructor != null && (parameters = constructor.GetParameters()).Length > 0)
        {
            if (arguments.Count != parameters.Length)
                throw Error.IncorrectNumberOfConstructorArguments();
            List<Expression> sequence = null;
            var index1 = 0;
            for (var count = arguments.Count; index1 < count; ++index1)
            {
                var expression = arguments[index1];
                var parameterInfo = parameters[index1];
                if (expression == null)
                    throw Error.ArgumentNull(nameof(arguments));
                var type1 = parameterInfo.ParameterType;
                if (type1.IsByRef)
                    type1 = type1.GetElementType();
                if (!AreReferenceAssignable(type1, expression.Type))
                    expression = IsSameOrSubclass(typeof(Expression), type1) && AreAssignable(type1, expression.GetType()) ? (Expression)Quote(expression) : throw Error.ExpressionTypeDoesNotMatchConstructorParameter(expression.Type, type1);
                if (sequence == null && expression != arguments[index1])
                {
                    sequence = new List<Expression>(arguments.Count);
                    for (var index2 = 0; index2 < index1; ++index2)
                        sequence.Add(arguments[index2]);
                }
                sequence?.Add(expression);
            }
            if (sequence == null)
                return;
            arguments = sequence.ToReadOnlyCollection<Expression>();
        }
        else if (arguments != null && arguments.Count > 0)
            throw Error.IncorrectNumberOfConstructorArguments();
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that represents creating an array that has a specified rank.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.NewArrayBounds" /> and the <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.</returns>
    /// <param name="type">A <see cref="T:System.Type" /> that represents the element type of the array.</param>
    /// <param name="bounds">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="type" /> or <paramref name="bounds" /> is null.-or-An element of <paramref name="bounds" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="bounds" /> does not represent an integral type.</exception>
    public static NewArrayExpression NewArrayBounds(
        Type type,
        params Expression[] bounds)
    {
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        if (bounds == null)
            throw Error.ArgumentNull(nameof(bounds));
        return !type.Equals(typeof(void)) ? NewArrayBounds(type, bounds.ToReadOnlyCollection<Expression>()) : throw Error.ArgumentCannotBeOfTypeVoid();
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that represents creating an array that has a specified rank.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.NewArrayBounds" /> and the <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.</returns>
    /// <param name="type">A <see cref="T:System.Type" /> that represents the element type of the array.</param>
    /// <param name="bounds">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="type" /> or <paramref name="bounds" /> is null.-or-An element of <paramref name="bounds" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="bounds" /> does not represent an integral type.</exception>
    public static NewArrayExpression NewArrayBounds(
        Type type,
        IEnumerable<Expression> bounds)
    {
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        if (bounds == null)
            throw Error.ArgumentNull(nameof(bounds));
        if (type.Equals(typeof(void)))
            throw Error.ArgumentCannotBeOfTypeVoid();
        var readOnlyCollection = bounds.ToReadOnlyCollection<Expression>();
        var index = 0;
        for (var count = readOnlyCollection.Count; index < count; ++index)
            ValidateIntegerArg((readOnlyCollection[index] ?? throw Error.ArgumentNull(nameof(bounds))).Type);
        return new NewArrayExpression(ExpressionType.NewArrayBounds, type.MakeArrayType(readOnlyCollection.Count), readOnlyCollection);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that represents creating a one-dimensional array and initializing it from a list of elements.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.NewArrayInit" /> and the <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.</returns>
    /// <param name="type">A <see cref="T:System.Type" /> that represents the element type of the array.</param>
    /// <param name="initializers">An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="type" /> or <paramref name="initializers" /> is null.-or-An element of <paramref name="initializers" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="initializers" /> represents a type that is not assignable to the type <paramref name="type" />.</exception>
    public static NewArrayExpression NewArrayInit(
        Type type,
        params Expression[] initializers)
    {
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        if (initializers == null)
            throw Error.ArgumentNull(nameof(initializers));
        return !type.Equals(typeof(void)) ? NewArrayInit(type, initializers.ToReadOnlyCollection<Expression>()) : throw Error.ArgumentCannotBeOfTypeVoid();
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that represents creating a one-dimensional array and initializing it from a list of elements.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.NewArrayInit" /> and the <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.</returns>
    /// <param name="type">A <see cref="T:System.Type" /> that represents the element type of the array.</param>
    /// <param name="initializers">An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="type" /> or <paramref name="initializers" /> is null.-or-An element of <paramref name="initializers" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="initializers" /> represents a type that is not assignable to the type that <paramref name="type" /> represents.</exception>
    public static NewArrayExpression NewArrayInit(
        Type type,
        IEnumerable<Expression> initializers)
    {
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        if (initializers == null)
            throw Error.ArgumentNull(nameof(initializers));
        if (type.Equals(typeof(void)))
            throw Error.ArgumentCannotBeOfTypeVoid();
        var readOnlyCollection = initializers.ToReadOnlyCollection<Expression>();
        List<Expression> sequence = null;
        var index1 = 0;
        for (var count = readOnlyCollection.Count; index1 < count; ++index1)
        {
            var expression = readOnlyCollection[index1];
            if (expression == null)
                throw Error.ArgumentNull(nameof(initializers));
            if (!AreReferenceAssignable(type, expression.Type))
                expression = IsSameOrSubclass(typeof(Expression), type) && AreAssignable(type, expression.GetType()) ? (Expression)Quote(expression) : throw Error.ExpressionTypeCannotInitializeArrayType(expression.Type, type);
            if (sequence == null && expression != readOnlyCollection[index1])
            {
                sequence = new List<Expression>(readOnlyCollection.Count);
                for (var index2 = 0; index2 < index1; ++index2)
                    sequence.Add(readOnlyCollection[index2]);
            }
            sequence?.Add(expression);
        }
        if (sequence != null)
            readOnlyCollection = sequence.ToReadOnlyCollection<Expression>();
        return new NewArrayExpression(ExpressionType.NewArrayInit, type.MakeArrayType(), readOnlyCollection);
    }

    private static void ValidateSettableFieldOrPropertyMember(
        MemberInfo member,
        out Type memberType)
    {
        switch (member)
        {
            case FieldInfo fieldInfo:
                memberType = fieldInfo.FieldType;
                break;
            case PropertyInfo p0:
                memberType = p0.CanWrite ? p0.PropertyType : throw Error.PropertyDoesNotHaveSetter(p0);
                break;
            default:
                throw Error.ArgumentMustBeFieldInfoOrPropertInfo();
        }
    }

    private static void ValidateAnonymousTypeMember(MemberInfo member, out Type memberType)
    {
        switch (member.MemberType)
        {
            case MemberTypes.Field:
                var fieldInfo = member as FieldInfo;
                memberType = !fieldInfo?.IsStatic??true ? fieldInfo!.FieldType : throw Error.ArgumentMustBeInstanceMember();
                break;
            case MemberTypes.Method:
                var methodInfo = member as MethodInfo;
                memberType = !methodInfo?.IsStatic??true ? methodInfo!.ReturnType : throw Error.ArgumentMustBeInstanceMember();
                break;
            case MemberTypes.Property:
                var p0 = member as PropertyInfo;
                if (!p0.CanRead)
                    throw Error.PropertyDoesNotHaveGetter(p0);
                memberType = !p0.GetGetMethod().IsStatic ? p0.PropertyType : throw Error.ArgumentMustBeInstanceMember();
                break;
            default:
                throw Error.ArgumentMustBeFieldInfoOrPropertInfoOrMethod();
        }
    }

    private static void ValidateGettableFieldOrPropertyMember(
        MemberInfo member,
        out Type memberType)
    {
        switch (member)
        {
            case FieldInfo fieldInfo:
                memberType = fieldInfo.FieldType;
                break;
            case PropertyInfo p0:
                memberType = p0.CanRead ? p0.PropertyType : throw Error.PropertyDoesNotHaveGetter(p0);
                break;
            default:
                throw Error.ArgumentMustBeFieldInfoOrPropertInfo();
        }
    }

    private static void ValidateMemberInitArgs(
        Type type,
        ReadOnlyCollection<MemberBinding> bindings)
    {
        var index = 0;
        for (var count = bindings.Count; index < count; ++index)
        {
            var binding = bindings[index];
            if (!AreAssignable(binding.Member.DeclaringType, type))
                throw Error.NotAMemberOfType(binding.Member.Name, type);
        }
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a bitwise complement operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Not" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The unary not operator is not defined for <paramref name="expression" />.Type.</exception>
    public static UnaryExpression Not(Expression expression)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return IsIntegerOrBool(expression.Type) ? new UnaryExpression(ExpressionType.Not, expression, expression.Type) : GetUserDefinedUnaryOperator(ExpressionType.Not, "op_LogicalNot", expression) ?? GetUserDefinedUnaryOperatorOrThrow(ExpressionType.Not, "op_OnesComplement", expression);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a bitwise complement operation. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Not" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly one argument.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the unary not operator is not defined for <paramref name="expression" />.Type.-or-<paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value type) is not assignable to the argument type of the method represented by <paramref name="method" />.</exception>
    public static UnaryExpression Not(Expression expression, MethodInfo method)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return method == null ? Not(expression) : GetMethodBasedUnaryOperator(ExpressionType.Not, expression, method);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise OR operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Or" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The bitwise OR operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Or(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return left.Type == right.Type && IsIntegerOrBool(left.Type) ? new BinaryExpression(ExpressionType.Or, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Or, "op_BitwiseOr", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise OR operation. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Or" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the bitwise OR operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Or(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? Or(left, right) : GetMethodBasedBinaryOperator(ExpressionType.Or, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a conditional OR operation that evaluates the second operand only if it has to.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.OrElse" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The bitwise OR operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.-or-<paramref name="left" />.Type and <paramref name="right" />.Type are not the same Boolean type.</exception>
    public static BinaryExpression OrElse(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        if (left.Type == right.Type && IsBool(left.Type))
            return new BinaryExpression(ExpressionType.OrElse, left, right, left.Type);
        var definedBinaryOperator = GetUserDefinedBinaryOperator(ExpressionType.OrElse, left.Type, right.Type, "op_BitwiseOr");
        if (definedBinaryOperator == null)
            throw Error.BinaryOperatorNotDefined(ExpressionType.OrElse, left.Type, right.Type);
        ValidateUserDefinedConditionalLogicOperator(ExpressionType.OrElse, left.Type, right.Type, definedBinaryOperator);
        var type = !IsNullableType(left.Type) || definedBinaryOperator.ReturnType != GetNonNullableType(left.Type) ? definedBinaryOperator.ReturnType : left.Type;
        return new BinaryExpression(ExpressionType.OrElse, left, right, definedBinaryOperator, type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a conditional OR operation that evaluates the second operand only if it has to. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.OrElse" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the bitwise OR operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.-or-<paramref name="method" /> is null and <paramref name="left" />.Type and <paramref name="right" />.Type are not the same Boolean type.</exception>
    public static BinaryExpression OrElse(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        if (method == null)
            return OrElse(left, right);
        ValidateUserDefinedConditionalLogicOperator(ExpressionType.OrElse, left.Type, right.Type, method);
        var type = !IsNullableType(left.Type) || method.ReturnType != GetNonNullableType(left.Type) ? method.ReturnType : left.Type;
        return new BinaryExpression(ExpressionType.OrElse, left, right, method, type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.ParameterExpression" />.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ParameterExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Parameter" /> and the <see cref="P:System.Linq.Expressions.Expression.Type" /> and <see cref="P:System.Linq.Expressions.ParameterExpression.Name" /> properties set to the specified values.</returns>
    /// <param name="type">A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" /> property equal to.</param>
    /// <param name="name">The value to set the <see cref="P:System.Linq.Expressions.ParameterExpression.Name" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="type" /> is null.</exception>
    public static ParameterExpression Parameter(Type type, string name)
    {
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        return type != typeof(void) ? new ParameterExpression(type, name) : throw Error.ArgumentCannotBeOfTypeVoid();
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents raising a number to a power.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Power" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The exponentiation operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.-or-<paramref name="left" />.Type and/or <paramref name="right" />.Type are not <see cref="T:System.Double" />.</exception>
    public static BinaryExpression Power(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return Power(left, right, typeof(Math).GetMethod("Pow", BindingFlags.Static | BindingFlags.Public) ?? throw Error.BinaryOperatorNotDefined(ExpressionType.Power, left.Type, right.Type));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents raising a number to a power. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Power" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the exponentiation operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.-or-<paramref name="method" /> is null and <paramref name="left" />.Type and/or <paramref name="right" />.Type are not <see cref="T:System.Double" />.</exception>
    public static BinaryExpression Power(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? Power(left, right) : GetMethodBasedBinaryOperator(ExpressionType.Power, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a property.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" /> and the <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> and <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property equal to.</param>
    /// <param name="property">The <see cref="T:System.Reflection.PropertyInfo" /> to set the <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="property" /> is null.-or-The property that <paramref name="property" /> represents is not static (Shared in Visual Basic) and <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="expression" />.Type is not assignable to the declaring type of the property that <paramref name="property" /> represents.</exception>
    public static MemberExpression Property(
        Expression expression,
        PropertyInfo property)
    {
        if (property == null)
            throw Error.ArgumentNull(nameof(property));
        if (!property.CanRead)
            throw Error.PropertyDoesNotHaveGetter(property);
        if (!property.GetGetMethod(true).IsStatic)
        {
            if (expression == null)
                throw Error.ArgumentNull(nameof(expression));
            if (!AreReferenceAssignable(property.DeclaringType, expression.Type))
                throw Error.PropertyNotDefinedForType(property, expression.Type);
        }
        return new MemberExpression(expression, property, property.PropertyType);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a property by using a property accessor method.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" />, the <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" /> and the <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property set to the <see cref="T:System.Reflection.PropertyInfo" /> that represents the property accessed in <paramref name="propertyAccessor" />.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property equal to.</param>
    /// <param name="propertyAccessor">The <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor method.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="propertyAccessor" /> is null.-or-The method that <paramref name="propertyAccessor" /> represents is not static (Shared in Visual Basic) and <paramref name="expression" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="expression" />.Type is not assignable to the declaring type of the method represented by <paramref name="propertyAccessor" />.-or-The method that <paramref name="propertyAccessor" /> represents is not a property accessor method.</exception>
    public static MemberExpression Property(
        Expression expression,
        MethodInfo propertyAccessor)
    {
        if (propertyAccessor == null)
            throw Error.ArgumentNull(nameof(propertyAccessor));
        ValidateMethodInfo(propertyAccessor);
        return Property(expression, GetProperty(propertyAccessor));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a property given the name of the property.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" />, the <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" />, and the <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property set to the <see cref="T:System.Reflection.PropertyInfo" /> that represents the property denoted by <paramref name="propertyName" />.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> whose <see cref="P:System.Linq.Expressions.Expression.Type" /> contains a property named <paramref name="propertyName" />.</param>
    /// <param name="propertyName">The name of a property.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> or <paramref name="propertyName" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">No property named <paramref name="propertyName" /> is defined in <paramref name="expression" />.Type or its base types.</exception>
    public static MemberExpression Property(
        Expression expression,
        string propertyName)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return Property(expression, (expression.Type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy) ?? expression.Type.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)) ?? throw Error.PropertyNotDefinedForType(propertyName, expression.Type));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a property or field given the name of the property or field.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" />, the <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" />, and the <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property set to the <see cref="T:System.Reflection.PropertyInfo" /> or <see cref="T:System.Reflection.FieldInfo" /> that represents the property or field denoted by <paramref name="propertyOrFieldName" />.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> whose <see cref="P:System.Linq.Expressions.Expression.Type" /> contains a property or field named <paramref name="propertyOrFieldName" />.</param>
    /// <param name="propertyOrFieldName">The name of a property or field.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> or <paramref name="propertyOrFieldName" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">No property or field named <paramref name="propertyOrFieldName" /> is defined in <paramref name="expression" />.Type or its base types.</exception>
    public static MemberExpression PropertyOrField(
        Expression expression,
        string propertyOrFieldName)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        var property1 = expression.Type.GetProperty(propertyOrFieldName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        if (property1 != null)
            return Property(expression, property1);
        var field = expression.Type.GetField(propertyOrFieldName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        if (field != null)
            return Field(expression, field);
        var property2 = expression.Type.GetProperty(propertyOrFieldName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        if (property2 != null)
            return Property(expression, property2);
        return Field(expression, expression.Type.GetField(propertyOrFieldName, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy) ?? throw Error.NotAMemberOfType(propertyOrFieldName, expression.Type));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an expression that has a constant value of type <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Quote" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> is null.</exception>
    public static UnaryExpression Quote(Expression expression) => expression != null ? new UnaryExpression(ExpressionType.Quote, expression, expression.GetType()) : throw Error.ArgumentNull(nameof(expression));

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift operation.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.RightShift" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The right-shift operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression RightShift(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return IsInteger(left.Type) && GetNonNullableType(right.Type) == typeof(int) ? new BinaryExpression(ExpressionType.RightShift, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.RightShift, "op_RightShift", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift operation. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.RightShift" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the right-shift operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression RightShift(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? RightShift(left, right) : GetMethodBasedBinaryOperator(ExpressionType.RightShift, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction operation that does not have overflow checking.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Subtract" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The subtraction operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Subtract(Expression left, Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return left.Type == right.Type && IsArithmetic(left.Type) ? new BinaryExpression(ExpressionType.Subtract, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Subtract, "op_Subtraction", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction operation that does not have overflow checking. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.Subtract" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the subtraction operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression Subtract(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? Subtract(left, right) : GetMethodBasedBinaryOperator(ExpressionType.Subtract, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction operation that has overflow checking.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.SubtractChecked" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.InvalidOperationException">The subtraction operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression SubtractChecked(
        Expression left,
        Expression right)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return left.Type == right.Type && IsArithmetic(left.Type) ? new BinaryExpression(ExpressionType.SubtractChecked, left, right, left.Type) : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.SubtractChecked, "op_Subtraction", left, right, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction operation that has overflow checking. The implementing method can be specified.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.SubtractChecked" /> and the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />, <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.</returns>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> to set the <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual Basic), or does not take exactly two arguments.</exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="method" /> is null and the subtraction operator is not defined for <paramref name="left" />.Type and <paramref name="right" />.Type.</exception>
    public static BinaryExpression SubtractChecked(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        if (left == null)
            throw Error.ArgumentNull(nameof(left));
        if (right == null)
            throw Error.ArgumentNull(nameof(right));
        return method == null ? SubtractChecked(left, right) : GetMethodBasedBinaryOperator(ExpressionType.SubtractChecked, left, right, method, true);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an explicit reference or boxing conversion where null is supplied if the conversion fails.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.TypeAs" /> and the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and <see cref="P:System.Linq.Expressions.Expression.Type" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.</param>
    /// <param name="type">A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> or <paramref name="type" /> is null.</exception>
    public static UnaryExpression TypeAs(Expression expression, Type type)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        return !type.IsValueType || IsNullableType(type) ? new UnaryExpression(ExpressionType.TypeAs, expression, type) : throw Error.IncorrectTypeForTypeAs(type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.TypeBinaryExpression" />.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.TypeIs" /> and the <see cref="P:System.Linq.Expressions.TypeBinaryExpression.Expression" /> and <see cref="P:System.Linq.Expressions.TypeBinaryExpression.TypeOperand" /> properties set to the specified values.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to set the <see cref="P:System.Linq.Expressions.TypeBinaryExpression.Expression" /> property equal to.</param>
    /// <param name="type">A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.TypeBinaryExpression.TypeOperand" /> property equal to.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> or <paramref name="type" /> is null.</exception>
    public static TypeBinaryExpression TypeIs(Expression expression, Type type)
    {
        if (expression == null)
            throw Error.ArgumentNull(nameof(expression));
        return type != null ? new TypeBinaryExpression(ExpressionType.TypeIs, expression, type, typeof(bool)) : throw Error.ArgumentNull(nameof(type));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" />, given an operand, by calling the appropriate factory method.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.UnaryExpression" /> that results from calling the appropriate factory method.</returns>
    /// <param name="unaryType">The <see cref="T:System.Linq.Expressions.ExpressionType" /> that specifies the type of unary operation.</param>
    /// <param name="operand">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the operand.</param>
    /// <param name="type">The <see cref="T:System.Type" /> that specifies the type to be converted to (pass null if not applicable).</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="unaryType" /> does not correspond to a unary expression node.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="operand" /> is null.</exception>
    public static UnaryExpression MakeUnary(
        ExpressionType unaryType,
        Expression operand,
        Type type)
    {
        return MakeUnary(unaryType, operand, type, null);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" />, given an operand and implementing method, by calling the appropriate factory method.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.UnaryExpression" /> that results from calling the appropriate factory method.</returns>
    /// <param name="unaryType">The <see cref="T:System.Linq.Expressions.ExpressionType" /> that specifies the type of unary operation.</param>
    /// <param name="operand">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the operand.</param>
    /// <param name="type">The <see cref="T:System.Type" /> that specifies the type to be converted to (pass null if not applicable).</param>
    /// <param name="method">The <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="unaryType" /> does not correspond to a unary expression node.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="operand" /> is null.</exception>
    public static UnaryExpression MakeUnary(
        ExpressionType unaryType,
        Expression operand,
        Type type,
        MethodInfo method)
    {
        switch (unaryType)
        {
            case ExpressionType.ArrayLength:
                return ArrayLength(operand);
            case ExpressionType.Convert:
                return Convert(operand, type, method);
            case ExpressionType.ConvertChecked:
                return ConvertChecked(operand, type, method);
            case ExpressionType.Negate:
                return Negate(operand, method);
            case ExpressionType.NegateChecked:
                return NegateChecked(operand, method);
            case ExpressionType.Not:
                return Not(operand, method);
            case ExpressionType.Quote:
                return Quote(operand);
            case ExpressionType.TypeAs:
                return TypeAs(operand, type);
            default:
                throw Error.UnhandledUnary(unaryType);
        }
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" />, given the left and right operands, by calling an appropriate factory method.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.BinaryExpression" /> that results from calling the appropriate factory method.</returns>
    /// <param name="binaryType">The <see cref="T:System.Linq.Expressions.ExpressionType" /> that specifies the type of binary operation.</param>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the left operand.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the right operand.</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="binaryType" /> does not correspond to a binary expression node.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    public static BinaryExpression MakeBinary(
        ExpressionType binaryType,
        Expression left,
        Expression right)
    {
        return MakeBinary(binaryType, left, right, false, null);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" />, given the left operand, right operand and implementing method, by calling the appropriate factory method.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.BinaryExpression" /> that results from calling the appropriate factory method.</returns>
    /// <param name="binaryType">The <see cref="T:System.Linq.Expressions.ExpressionType" /> that specifies the type of binary operation.</param>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the left operand.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the right operand.</param>
    /// <param name="liftToNull">true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true; false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that specifies the implementing method.</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="binaryType" /> does not correspond to a binary expression node.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    public static BinaryExpression MakeBinary(
        ExpressionType binaryType,
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        return MakeBinary(binaryType, left, right, liftToNull, method, null);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" />, given the left operand, right operand, implementing method and type conversion function, by calling the appropriate factory method.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.BinaryExpression" /> that results from calling the appropriate factory method.</returns>
    /// <param name="binaryType">The <see cref="T:System.Linq.Expressions.ExpressionType" /> that specifies the type of binary operation.</param>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the left operand.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the right operand.</param>
    /// <param name="liftToNull">true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true; false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that specifies the implementing method.</param>
    /// <param name="conversion">A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that represents a type conversion function. This parameter is used only if <paramref name="binaryType" /> is <see cref="F:System.Linq.Expressions.ExpressionType.Coalesce" />.</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="binaryType" /> does not correspond to a binary expression node.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="left" /> or <paramref name="right" /> is null.</exception>
    public static BinaryExpression MakeBinary(
        ExpressionType binaryType,
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method,
        LambdaExpression conversion)
    {
        switch (binaryType)
        {
            case ExpressionType.Add:
                return Add(left, right, method);
            case ExpressionType.AddChecked:
                return AddChecked(left, right, method);
            case ExpressionType.And:
                return And(left, right, method);
            case ExpressionType.AndAlso:
                return AndAlso(left, right);
            case ExpressionType.ArrayIndex:
                return ArrayIndex(left, right);
            case ExpressionType.Coalesce:
                return Coalesce(left, right, conversion);
            case ExpressionType.Divide:
                return Divide(left, right, method);
            case ExpressionType.Equal:
                return Equal(left, right, liftToNull, method);
            case ExpressionType.ExclusiveOr:
                return ExclusiveOr(left, right, method);
            case ExpressionType.GreaterThan:
                return GreaterThan(left, right, liftToNull, method);
            case ExpressionType.GreaterThanOrEqual:
                return GreaterThanOrEqual(left, right, liftToNull, method);
            case ExpressionType.LeftShift:
                return LeftShift(left, right, method);
            case ExpressionType.LessThan:
                return LessThan(left, right, liftToNull, method);
            case ExpressionType.LessThanOrEqual:
                return LessThanOrEqual(left, right, liftToNull, method);
            case ExpressionType.Modulo:
                return Modulo(left, right, method);
            case ExpressionType.Multiply:
                return Multiply(left, right, method);
            case ExpressionType.MultiplyChecked:
                return MultiplyChecked(left, right, method);
            case ExpressionType.NotEqual:
                return NotEqual(left, right, liftToNull, method);
            case ExpressionType.Or:
                return Or(left, right, method);
            case ExpressionType.OrElse:
                return OrElse(left, right);
            case ExpressionType.Power:
                return Power(left, right, method);
            case ExpressionType.RightShift:
                return RightShift(left, right, method);
            case ExpressionType.Subtract:
                return Subtract(left, right, method);
            case ExpressionType.SubtractChecked:
                return SubtractChecked(left, right, method);
            default:
                throw Error.UnhandledBinary(binaryType);
        }
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing either a field or a property.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.MemberExpression" /> that results from calling the appropriate factory method.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the object that the member belongs to.</param>
    /// <param name="member">The <see cref="T:System.Reflection.MemberInfo" /> that describes the field or property to be accessed.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="expression" /> or <paramref name="member" /> is null.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="member" /> does not represent a field or property.</exception>
    public static MemberExpression MakeMemberAccess(
        Expression expression,
        MemberInfo member)
    {
        switch (member)
        {
            case null:
                throw Error.ArgumentNull(nameof(member));
            case FieldInfo field:
                return Field(expression, field);
            case PropertyInfo property:
                return Property(expression, property);
            default:
                throw Error.MemberNotFieldOrProperty(member);
        }
    }

    private static BinaryExpression GetEqualityComparisonOperator(
        ExpressionType binaryType,
        string opName,
        Expression left,
        Expression right,
        bool liftToNull)
    {
        if (left.Type == right.Type && (IsNumeric(left.Type) || left.Type == typeof(object)))
            return IsNullableType(left.Type) && liftToNull ? new BinaryExpression(binaryType, left, right, typeof(bool?)) : new BinaryExpression(binaryType, left, right, typeof(bool));
        var definedBinaryOperator = GetUserDefinedBinaryOperator(binaryType, opName, left, right, liftToNull);
        if (definedBinaryOperator != null)
            return definedBinaryOperator;
        if (!HasBuiltInEqualityOperator(left.Type, right.Type) && !IsNullComparison(left, right))
            throw Error.BinaryOperatorNotDefined(binaryType, left.Type, right.Type);
        return IsNullableType(left.Type) && liftToNull ? new BinaryExpression(binaryType, left, right, typeof(bool?)) : new BinaryExpression(binaryType, left, right, typeof(bool));
    }

    private static bool IsNullComparison(Expression left, Expression right)
    {
        if (IsNullConstant(left) && !IsNullConstant(right) && IsNullableType(right.Type))
            return true;
        return IsNullConstant(right) && !IsNullConstant(left) && IsNullableType(left.Type);
    }

    private static bool HasBuiltInEqualityOperator(Type left, Type right)
    {
        if (left.IsInterface && !right.IsValueType || right.IsInterface && !left.IsValueType || !left.IsValueType && !right.IsValueType && (AreReferenceAssignable(left, right) || AreReferenceAssignable(right, left)))
            return true;
        if (left != right)
            return false;
        var nonNullableType = GetNonNullableType(left);
        return nonNullableType == typeof(bool) || IsNumeric(nonNullableType) || nonNullableType.IsEnum;
    }

    private static BinaryExpression GetComparisonOperator(
        ExpressionType binaryType,
        string opName,
        Expression left,
        Expression right,
        bool liftToNull)
    {
        if (left.Type != right.Type || !IsNumeric(left.Type))
            return GetUserDefinedBinaryOperatorOrThrow(binaryType, opName, left, right, liftToNull);
        return IsNullableType(left.Type) && liftToNull ? new BinaryExpression(binaryType, left, right, typeof(bool?)) : new BinaryExpression(binaryType, left, right, typeof(bool));
    }

    private static UnaryExpression GetUserDefinedCoercionOrThrow(
        ExpressionType coercionType,
        Expression expression,
        Type convertToType)
    {
        return GetUserDefinedCoercion(coercionType, expression, convertToType) ?? throw Error.CoercionOperatorNotDefined(expression.Type, convertToType);
    }

    private static UnaryExpression GetUserDefinedCoercion(
        ExpressionType coercionType,
        Expression expression,
        Type convertToType)
    {
        var nonNullableType1 = GetNonNullableType(expression.Type);
        var nonNullableType2 = GetNonNullableType(convertToType);
        var methods1 = nonNullableType1.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var conversionOperator1 = FindConversionOperator(methods1, expression.Type, convertToType);
        if (conversionOperator1 != null)
            return new UnaryExpression(coercionType, expression, conversionOperator1, convertToType);
        var methods2 = nonNullableType2.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var conversionOperator2 = FindConversionOperator(methods2, expression.Type, convertToType);
        if (conversionOperator2 != null)
            return new UnaryExpression(coercionType, expression, conversionOperator2, convertToType);
        if (nonNullableType1 != expression.Type || nonNullableType2 != convertToType)
        {
            var method = FindConversionOperator(methods1, nonNullableType1, nonNullableType2) ?? FindConversionOperator(methods2, nonNullableType1, nonNullableType2);
            if (method != null)
                return new UnaryExpression(coercionType, expression, method, convertToType);
        }
        return null;
    }

    private static MethodInfo FindConversionOperator(
        MethodInfo[] methods,
        Type typeFrom,
        Type typeTo)
    {
        foreach (var method in methods)
        {
            if ((!(method.Name != "op_Implicit") || !(method.Name != "op_Explicit")) && method.ReturnType == typeTo && method.GetParameters()[0].ParameterType == typeFrom)
                return method;
        }
        return null;
    }

    private static UnaryExpression GetUserDefinedUnaryOperatorOrThrow(
        ExpressionType unaryType,
        string name,
        Expression operand)
    {
        var definedUnaryOperator = GetUserDefinedUnaryOperator(unaryType, name, operand);
        if (definedUnaryOperator == null)
            throw Error.UnaryOperatorNotDefined(unaryType, operand.Type);
        ValidateParamswithOperandsOrThrow(definedUnaryOperator.Method.GetParameters()[0].ParameterType, operand.Type, unaryType, name);
        return definedUnaryOperator;
    }

    private static UnaryExpression GetUserDefinedUnaryOperator(
        ExpressionType unaryType,
        string name,
        Expression operand)
    {
        var type = operand.Type;
        var types = new Type[1] { type };
        var nonNullableType = GetNonNullableType(type);
        var method1 = nonNullableType.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        if (method1 != null)
            return new UnaryExpression(unaryType, operand, method1, method1.ReturnType);
        if (IsNullableType(type))
        {
            types[0] = nonNullableType;
            var method2 = nonNullableType.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
            if (method2 != null && method2.ReturnType.IsValueType && !IsNullableType(method2.ReturnType))
                return new UnaryExpression(unaryType, operand, method2, GetNullableType(method2.ReturnType));
        }
        return null;
    }

    private static void ValidateParamswithOperandsOrThrow(
        Type paramType,
        Type operandType,
        ExpressionType exprType,
        string name)
    {
        if (IsNullableType(paramType) && !IsNullableType(operandType))
            throw Error.OperandTypesDoNotMatchParameters(exprType, name);
    }

    private static BinaryExpression GetUserDefinedBinaryOperatorOrThrow(
        ExpressionType binaryType,
        string name,
        Expression left,
        Expression right,
        bool liftToNull)
    {
        var definedBinaryOperator = GetUserDefinedBinaryOperator(binaryType, name, left, right, liftToNull);
        if (definedBinaryOperator == null)
            throw Error.BinaryOperatorNotDefined(binaryType, left.Type, right.Type);
        ValidateParamswithOperandsOrThrow(definedBinaryOperator.Method.GetParameters()[0].ParameterType, left.Type, binaryType, name);
        ValidateParamswithOperandsOrThrow(definedBinaryOperator.Method.GetParameters()[1].ParameterType, right.Type, binaryType, name);
        return definedBinaryOperator;
    }

    private static MethodInfo GetUserDefinedBinaryOperator(
        ExpressionType binaryType,
        Type leftType,
        Type rightType,
        string name)
    {
        var types = new Type[2] { leftType, rightType };
        var nonNullableType1 = GetNonNullableType(leftType);
        var nonNullableType2 = GetNonNullableType(rightType);
        var bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var method = nonNullableType1.GetMethod(name, bindingAttr, null, types, null) ?? nonNullableType2.GetMethod(name, bindingAttr, null, types, null);
        if (IsLiftingConditionalLogicalOperator(leftType, rightType, method, binaryType))
            method = GetUserDefinedBinaryOperator(binaryType, nonNullableType1, nonNullableType2, name);
        return method;
    }

    private static bool IsLiftingConditionalLogicalOperator(
        Type left,
        Type right,
        MethodInfo method,
        ExpressionType binaryType)
    {
        if (!IsNullableType(right) || !IsNullableType(left) || method != null)
            return false;
        return binaryType == ExpressionType.AndAlso || binaryType == ExpressionType.OrElse;
    }

    private static BinaryExpression GetUserDefinedBinaryOperator(
        ExpressionType binaryType,
        string name,
        Expression left,
        Expression right,
        bool liftToNull)
    {
        var definedBinaryOperator1 = GetUserDefinedBinaryOperator(binaryType, left.Type, right.Type, name);
        if (definedBinaryOperator1 != null)
            return new BinaryExpression(binaryType, left, right, definedBinaryOperator1, definedBinaryOperator1.ReturnType);
        if (IsNullableType(left.Type) && IsNullableType(right.Type))
        {
            var nonNullableType1 = GetNonNullableType(left.Type);
            var nonNullableType2 = GetNonNullableType(right.Type);
            var definedBinaryOperator2 = GetUserDefinedBinaryOperator(binaryType, nonNullableType1, nonNullableType2, name);
            if (definedBinaryOperator2 != null && definedBinaryOperator2.ReturnType.IsValueType && !IsNullableType(definedBinaryOperator2.ReturnType))
                return definedBinaryOperator2.ReturnType != typeof(bool) || liftToNull ? new BinaryExpression(binaryType, left, right, definedBinaryOperator2, GetNullableType(definedBinaryOperator2.ReturnType)) : new BinaryExpression(binaryType, left, right, definedBinaryOperator2, typeof(bool));
        }
        return null;
    }

    private static void ValidateOperator(MethodInfo method)
    {
        ValidateMethodInfo(method);
        if (!method.IsStatic)
            throw Error.UserDefinedOperatorMustBeStatic(method);
        if (method.ReturnType == typeof(void))
            throw Error.UserDefinedOperatorMustNotBeVoid(method);
    }

    private static void ValidateUserDefinedConditionalLogicOperator(
        ExpressionType nodeType,
        Type left,
        Type right,
        MethodInfo method)
    {
        ValidateOperator(method);
        var parameters = method.GetParameters();
        if (parameters.Length != 2)
            throw Error.IncorrectNumberOfMethodCallArguments(method);
        if (!ParameterIsAssignable(parameters[0], left) && (!IsNullableType(left) || !ParameterIsAssignable(parameters[0], GetNonNullableType(left))))
            throw Error.OperandTypesDoNotMatchParameters(nodeType, method.Name);
        if (!ParameterIsAssignable(parameters[1], right) && (!IsNullableType(right) || !ParameterIsAssignable(parameters[1], GetNonNullableType(right))))
            throw Error.OperandTypesDoNotMatchParameters(nodeType, method.Name);
        if (parameters[0].ParameterType != parameters[1].ParameterType)
            throw Error.LogicalOperatorMustHaveConsistentTypes(nodeType, method.Name);
        if (method.ReturnType != parameters[0].ParameterType)
            throw Error.LogicalOperatorMustHaveConsistentTypes(nodeType, method.Name);
        if (IsValidLiftedConditionalLogicalOperator(left, right, parameters))
        {
            left = GetNonNullableType(left);
            right = GetNonNullableType(left);
        }
        var types = new Type[1]
        {
            parameters[0].ParameterType
        };
        var method1 = method.DeclaringType.GetMethod("op_True", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        var method2 = method.DeclaringType.GetMethod("op_False", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
        if (method1 == null || method2 == null)
            throw Error.LogicalOperatorMustHaveBooleanOperators(nodeType, method.Name);
        if (method1.ReturnType != typeof(bool))
            throw Error.LogicalOperatorMustHaveBooleanOperators(nodeType, method.Name);
        if (method2.ReturnType != typeof(bool))
            throw Error.LogicalOperatorMustHaveBooleanOperators(nodeType, method.Name);
    }

    private static bool IsValidLiftedConditionalLogicalOperator(
        Type left,
        Type right,
        ParameterInfo[] pms)
    {
        return left == right && IsNullableType(right) && pms[1].ParameterType == GetNonNullableType(right);
    }

    private static UnaryExpression GetMethodBasedCoercionOperator(
        ExpressionType unaryType,
        Expression operand,
        Type convertToType,
        MethodInfo method)
    {
        ValidateOperator(method);
        var parameters = method.GetParameters();
        if (parameters.Length != 1)
            throw Error.IncorrectNumberOfMethodCallArguments(method);
        if (ParameterIsAssignable(parameters[0], operand.Type) && method.ReturnType == convertToType)
            return new UnaryExpression(unaryType, operand, method, method.ReturnType);
        if ((IsNullableType(operand.Type) || IsNullableType(convertToType)) && ParameterIsAssignable(parameters[0], GetNonNullableType(operand.Type)) && method.ReturnType == GetNonNullableType(convertToType))
            return new UnaryExpression(unaryType, operand, method, convertToType);
        throw Error.OperandTypesDoNotMatchParameters(unaryType, method.Name);
    }

    private static UnaryExpression GetMethodBasedUnaryOperator(
        ExpressionType unaryType,
        Expression operand,
        MethodInfo method)
    {
        ValidateOperator(method);
        var parameters = method.GetParameters();
        if (parameters.Length != 1)
            throw Error.IncorrectNumberOfMethodCallArguments(method);
        if (ParameterIsAssignable(parameters[0], operand.Type))
        {
            ValidateParamswithOperandsOrThrow(parameters[0].ParameterType, operand.Type, unaryType, method.Name);
            return new UnaryExpression(unaryType, operand, method, method.ReturnType);
        }
        if (IsNullableType(operand.Type) && ParameterIsAssignable(parameters[0], GetNonNullableType(operand.Type)) && method.ReturnType.IsValueType && !IsNullableType(method.ReturnType))
            return new UnaryExpression(unaryType, operand, method, GetNullableType(method.ReturnType));
        throw Error.OperandTypesDoNotMatchParameters(unaryType, method.Name);
    }

    private static BinaryExpression GetMethodBasedBinaryOperator(
        ExpressionType binaryType,
        Expression left,
        Expression right,
        MethodInfo method,
        bool liftToNull)
    {
        ValidateOperator(method);
        var parameters = method.GetParameters();
        if (parameters.Length != 2)
            throw Error.IncorrectNumberOfMethodCallArguments(method);
        if (ParameterIsAssignable(parameters[0], left.Type) && ParameterIsAssignable(parameters[1], right.Type))
        {
            ValidateParamswithOperandsOrThrow(parameters[0].ParameterType, left.Type, binaryType, method.Name);
            ValidateParamswithOperandsOrThrow(parameters[1].ParameterType, right.Type, binaryType, method.Name);
            return new BinaryExpression(binaryType, left, right, method, method.ReturnType);
        }
        if (!IsNullableType(left.Type) || !IsNullableType(right.Type) || !ParameterIsAssignable(parameters[0], GetNonNullableType(left.Type)) || !ParameterIsAssignable(parameters[1], GetNonNullableType(right.Type)) || !method.ReturnType.IsValueType || IsNullableType(method.ReturnType))
            throw Error.OperandTypesDoNotMatchParameters(binaryType, method.Name);
        return method.ReturnType != typeof(bool) || liftToNull ? new BinaryExpression(binaryType, left, right, method, GetNullableType(method.ReturnType)) : new BinaryExpression(binaryType, left, right, method, typeof(bool));
    }

    private static bool ParameterIsAssignable(ParameterInfo pi, Type argType)
    {
        var dest = pi.ParameterType;
        if (dest.IsByRef)
            dest = dest.GetElementType();
        return AreReferenceAssignable(dest, argType);
    }

    private static void ValidateIntegerArg(Type type)
    {
        if (!IsInteger(type))
            throw Error.ArgumentMustBeInteger();
    }

    private static void ValidateIntegerOrBoolArg(Type type)
    {
        if (!IsIntegerOrBool(type))
            throw Error.ArgumentMustBeIntegerOrBoolean();
    }

    private static void ValidateNumericArg(Type type)
    {
        if (!IsNumeric(type))
            throw Error.ArgumentMustBeNumeric();
    }

    private static void ValidateConvertibleArg(Type type)
    {
        if (!IsConvertible(type))
            throw Error.ArgumentMustBeConvertible();
    }

    private static void ValidateBoolArg(Type type)
    {
        if (!IsBool(type))
            throw Error.ArgumentMustBeBoolean();
    }

    private static Type ValidateCoalesceArgTypes(Type left, Type right)
    {
        var nonNullableType = GetNonNullableType(left);
        if (left.IsValueType && !IsNullableType(left))
            throw Error.CoalesceUsedOnNonNullType();
        if (IsNullableType(left) && IsImplicitlyConvertible(right, nonNullableType))
            return nonNullableType;
        if (IsImplicitlyConvertible(right, left))
            return left;
        return IsImplicitlyConvertible(nonNullableType, right) ? right : throw Error.ArgumentTypesMustMatch();
    }

    private static void ValidateSameArgTypes(Type left, Type right)
    {
        if (left != right)
            throw Error.ArgumentTypesMustMatch();
    }

    private static void ValidateElementInitAddMethodInfo(MethodInfo addMethod)
    {
        ValidateMethodInfo(addMethod);
        if (addMethod.GetParameters().Length == 0)
            throw Error.ElementInitializerMethodWithZeroArgs();
        if (!addMethod.Name.Equals("Add", StringComparison.OrdinalIgnoreCase))
            throw Error.ElementInitializerMethodNotAdd();
        if (addMethod.IsStatic)
            throw Error.ElementInitializerMethodStatic();
        foreach (var parameter in addMethod.GetParameters())
        {
            if (parameter.ParameterType.IsByRef)
                throw Error.ElementInitializerMethodNoRefOutParam(parameter.Name, addMethod.Name);
        }
    }

    private static void ValidateMethodInfo(MethodInfo method)
    {
        if (method.IsGenericMethodDefinition)
            throw Error.MethodIsGeneric(method);
        if (method.ContainsGenericParameters)
            throw Error.MethodContainsGenericParameters(method);
    }

    private static void ValidateType(Type type)
    {
        if (type.IsGenericTypeDefinition)
            throw Error.TypeIsGeneric(type);
        if (type.ContainsGenericParameters)
            throw Error.TypeContainsGenericParameters(type);
    }

    internal static Type GetNullableType(Type type)
    {
        if (type == null)
            throw Error.ArgumentNull(nameof(type));
        if (!type.IsValueType || IsNullableType(type))
            return type;
        return typeof(Nullable<>).MakeGenericType(type);
    }

    private static bool IsSameOrSubclass(Type type, Type subType) => type == subType || subType.IsSubclassOf(type);

    private static bool AreReferenceAssignable(Type dest, Type src) => dest == src || !dest.IsValueType && !src.IsValueType && AreAssignable(dest, src);

    private static bool AreAssignable(Type dest, Type src) => dest == src || dest.IsAssignableFrom(src) || dest.IsArray && src.IsArray && dest.GetArrayRank() == src.GetArrayRank() && AreReferenceAssignable(dest.GetElementType(), src.GetElementType()) || src.IsArray && dest.IsGenericType && (dest.GetGenericTypeDefinition() == typeof(IEnumerable<>) || dest.GetGenericTypeDefinition() == typeof(IList<>) || dest.GetGenericTypeDefinition() == typeof(ICollection<>)) && dest.GetGenericArguments()[0] == src.GetElementType();

    internal static bool IsNullableType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

    internal static Type GetNonNullableType(Type type)
    {
        if (IsNullableType(type))
            type = type.GetGenericArguments()[0];
        return type;
    }

    private static bool IsNullConstant(Expression expr) => expr is ConstantExpression constantExpression && constantExpression.Value == null;

    private static bool IsUnSigned(Type type)
    {
        type = GetNonNullableType(type);
        if (type.IsEnum)
            return false;
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }

    private static bool IsArithmetic(Type type)
    {
        type = GetNonNullableType(type);
        if (type.IsEnum)
            return false;
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
                return true;
            default:
                return false;
        }
    }

    private static bool IsNumeric(Type type)
    {
        type = GetNonNullableType(type);
        if (type.IsEnum)
            return false;
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Char:
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
                return true;
            default:
                return false;
        }
    }

    private static bool IsImplicitlyConvertible(Type source, Type destination) => IsIdentityConversion(source, destination) || IsImplicitNumericConversion(source, destination) || IsImplicitReferenceConversion(source, destination) || IsImplicitBoxingConversion(source, destination) || IsImplicitNullableConversion(source, destination);

    private static bool IsIdentityConversion(Type source, Type destination) => source == destination;

    private static bool IsImplicitNumericConversion(Type source, Type destination)
    {
        var typeCode1 = Type.GetTypeCode(source);
        var typeCode2 = Type.GetTypeCode(destination);
        switch (typeCode1)
        {
            case TypeCode.Char:
                switch (typeCode2)
                {
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return true;
                    default:
                        return false;
                }
            case TypeCode.SByte:
                switch (typeCode2)
                {
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return true;
                    default:
                        return false;
                }
            case TypeCode.Byte:
                switch (typeCode2)
                {
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return true;
                    default:
                        return false;
                }
            case TypeCode.Int16:
                switch (typeCode2)
                {
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return true;
                    default:
                        return false;
                }
            case TypeCode.UInt16:
                switch (typeCode2)
                {
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return true;
                    default:
                        return false;
                }
            case TypeCode.Int32:
                switch (typeCode2)
                {
                    case TypeCode.Int64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return true;
                    default:
                        return false;
                }
            case TypeCode.UInt32:
                switch (typeCode2)
                {
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return true;
                    default:
                        return false;
                }
            case TypeCode.Int64:
            case TypeCode.UInt64:
                switch (typeCode2)
                {
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return true;
                    default:
                        return false;
                }
            case TypeCode.Single:
                return typeCode2 == TypeCode.Double;
            default:
                return false;
        }
    }

    private static bool IsImplicitReferenceConversion(Type source, Type destination) => AreAssignable(destination, source);

    private static bool IsImplicitBoxingConversion(Type source, Type destination) => source.IsValueType && (destination == typeof(object) || destination == typeof(ValueType)) || source.IsEnum && destination == typeof(Enum);

    private static bool IsImplicitNullableConversion(Type source, Type destination) => IsNullableType(destination) && IsImplicitlyConvertible(GetNonNullableType(source), GetNonNullableType(destination));

    private static bool IsConvertible(Type type)
    {
        type = GetNonNullableType(type);
        if (type.IsEnum)
            return true;
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
            case TypeCode.Char:
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
                return true;
            default:
                return false;
        }
    }

    private static bool IsInteger(Type type)
    {
        type = GetNonNullableType(type);
        if (type.IsEnum)
            return false;
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }

    private static bool IsIntegerOrBool(Type type)
    {
        type = GetNonNullableType(type);
        if (type.IsEnum)
            return false;
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }

    private static bool IsBool(Type type)
    {
        type = GetNonNullableType(type);
        return type == typeof(bool);
    }

    internal static ReadOnlyCollection<T> ReturnReadOnly<T>(ref IList<T> collection)
    {
        var ts = collection;
        var ts1 = ts as ReadOnlyCollection<T>;
        if (ts1 != null)
        {
            return ts1;
        }
        Threading.Net20Interlocked.CompareExchange<IList<T>>(ref collection, ts.ToReadOnly<T>(), ts);
        return (ReadOnlyCollection<T>)collection;
    }

    internal class IndexExpressionProxy
    {
        private readonly IndexExpression _node;

        public ReadOnlyCollection<Expression> Arguments => _node.Arguments;

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public PropertyInfo Indexer => _node.Indexer;

        public ExpressionType NodeType => _node.NodeType;

        public Expression Object => _node.Object;

        public Type Type => _node.Type;

        public IndexExpressionProxy(IndexExpression node)
        {
            _node = node;
        }
    }

}