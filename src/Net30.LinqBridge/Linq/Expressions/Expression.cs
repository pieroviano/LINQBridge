#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Globalization;
using System.IO;
using System.Linq.Expressions.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Linq.Expressions;

/// <summary>
///     Provides the base class from which the classes that represent expression tree nodes are derived. It also
///     contains static (Shared in Visual Basic) factory methods to create the various node types. This is an abstract
///     class.
/// </summary>
public abstract class Expression
{
    private static readonly CacheDict<Type, MethodInfo> _LambdaDelegateCache = new(40);
    private static volatile CacheDict<Type, LambdaFactory> _LambdaFactories;
    private static ConditionalWeakTable<Expression, ExtensionInfo> _legacyCtorSupportTable;

    /// <summary>Initializes a new instance of the <see cref="T:System.Linq.Expressions.Expression" /> class.</summary>
    /// <param name="nodeType">The <see cref="T:System.Linq.Expressions.ExpressionType" /> to set as the node type.</param>
    /// <param name="type">
    ///     The <see cref="P:System.Linq.Expressions.Expression.Type" /> of this
    ///     <see cref="T:System.Linq.Expressions.Expression" />.
    /// </param>
    [Obsolete(
        "use a different constructor that does not take ExpressionType. Then override NodeType and Type properties to provide the values that would be specified to this constructor.")]
    protected Expression(ExpressionType nodeType, Type type)
    {
        if (_legacyCtorSupportTable == null)
        {
            Interlocked.CompareExchange(ref _legacyCtorSupportTable,
                new ConditionalWeakTable<Expression, ExtensionInfo>(), null);
        }

        _legacyCtorSupportTable.Add(this, new ExtensionInfo(nodeType, type));
    }

    /// <summary>Constructs a new instance of <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    protected Expression()
    {
    }

    /// <summary>Gets the node type of this <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>One of the <see cref="T:System.Linq.Expressions.ExpressionType" /> values.</returns>
    public virtual ExpressionType NodeType
    {
        get
        {
            ExtensionInfo extensionInfo;
            if (_legacyCtorSupportTable != null && _legacyCtorSupportTable.TryGetValue(this, out extensionInfo))
            {
                return extensionInfo.NodeType;
            }

            throw Error.ExtensionNodeMustOverrideProperty("Expression.NodeType");
        }
    }

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>The <see cref="T:System.Type" /> that represents the static type of the expression.</returns>
    public virtual Type Type
    {
        get
        {
            ExtensionInfo extensionInfo;
            if (_legacyCtorSupportTable != null && _legacyCtorSupportTable.TryGetValue(this, out extensionInfo))
            {
                return extensionInfo.Type;
            }

            throw Error.ExtensionNodeMustOverrideProperty("Expression.Type");
        }
    }

    /// <summary>
    ///     Indicates that the node can be reduced to a simpler node. If this returns true, Reduce() can be called to
    ///     produce the reduced form.
    /// </summary>
    /// <returns>True if the node can be reduced, otherwise false.</returns>
    public virtual bool CanReduce => false;

    private string DebugView
    {
        get
        {
            using (var writer = new StringWriter(CultureInfo.CurrentCulture))
            {
                DebugViewWriter.WriteTo(this, writer);
                return writer.ToString();
            }
        }
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Add" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The addition operator is not defined for <paramref name="left" />
    ///     .Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Add(Expression left, Expression right)
    {
        return Add(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition
    ///     operation that does not have overflow checking. The implementing method can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Add" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the addition operator is not defined for <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Add(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.Add, left, right, method, true);
        }

        return left.Type == right.Type && TypeUtils.IsArithmetic(left.Type)
            ? new SimpleBinaryExpression(ExpressionType.Add, left, right, left.Type)
            : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Add, "op_Addition", left, right, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AddAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression AddAssign(Expression left, Expression right)
    {
        return AddAssign(left, right, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AddAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression AddAssign(Expression left, Expression right, MethodInfo method)
    {
        return AddAssign(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AddAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression AddAssign(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.AddAssign, left, right, method, conversion, true);
        }

        if (!(left.Type == right.Type) || !TypeUtils.IsArithmetic(left.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.AddAssign, "op_Addition", left, right, conversion,
                true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        return new SimpleBinaryExpression(ExpressionType.AddAssign, left, right, left.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment
    ///     operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AddAssignChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression AddAssignChecked(Expression left, Expression right)
    {
        return AddAssignChecked(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment
    ///     operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AddAssignChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression AddAssignChecked(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        return AddAssignChecked(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an addition assignment
    ///     operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AddAssignChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression AddAssignChecked(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.AddAssignChecked, left, right, method, conversion, true);
        }

        if (!(left.Type == right.Type) || !TypeUtils.IsArithmetic(left.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.AddAssignChecked, "op_Addition", left, right,
                conversion, true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        return new SimpleBinaryExpression(ExpressionType.AddAssignChecked, left, right, left.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition
    ///     operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AddChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The addition operator is not defined for <paramref name="left" />
    ///     .Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression AddChecked(Expression left, Expression right)
    {
        return AddChecked(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic addition
    ///     operation that has overflow checking. The implementing method can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AddChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the addition operator is not defined for <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression AddChecked(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.AddChecked, left, right, method, true);
        }

        return left.Type == right.Type && TypeUtils.IsArithmetic(left.Type)
            ? new SimpleBinaryExpression(ExpressionType.AddChecked, left, right, left.Type)
            : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.AddChecked, "op_Addition", left, right, false);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise AND operation.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.And" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The bitwise AND operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression And(Expression left, Expression right)
    {
        return And(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise AND operation.
    ///     The implementing method can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.And" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the bitwise AND operator is not defined for <paramref name="left" />.Type
    ///     and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression And(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.And, left, right, method, true);
        }

        return left.Type == right.Type && TypeUtils.IsIntegerOrBool(left.Type)
            ? new SimpleBinaryExpression(ExpressionType.And, left, right, left.Type)
            : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.And, "op_BitwiseAnd", left, right, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a conditional AND
    ///     operation that evaluates the second operand only if the first operand evaluates to true.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AndAlso" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The bitwise AND operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.-or-<paramref name="left" />.Type and
    ///     <paramref name="right" />.Type are not the same Boolean type.
    /// </exception>
    public static BinaryExpression AndAlso(Expression left, Expression right)
    {
        return AndAlso(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a conditional AND
    ///     operation that evaluates the second operand only if the first operand is resolved to true. The implementing method
    ///     can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AndAlso" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the bitwise AND operator is not defined for <paramref name="left" />.Type
    ///     and <paramref name="right" />.Type.-or-<paramref name="method" /> is null and <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type are not the same Boolean type.
    /// </exception>
    public static BinaryExpression AndAlso(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (method == null)
        {
            if (left.Type == right.Type)
            {
                if (left.Type == typeof(bool))
                {
                    return new LogicalBinaryExpression(ExpressionType.AndAlso, left, right);
                }

                if (left.Type == typeof(bool?))
                {
                    return new SimpleBinaryExpression(ExpressionType.AndAlso, left, right, left.Type);
                }
            }

            method = GetUserDefinedBinaryOperator(ExpressionType.AndAlso, left.Type, right.Type, "op_BitwiseAnd");
            if (!(method != null))
            {
                throw Error.BinaryOperatorNotDefined(ExpressionType.AndAlso, left.Type, right.Type);
            }

            ValidateUserDefinedConditionalLogicOperator(ExpressionType.AndAlso, left.Type, right.Type, method);
            var type = !left.Type.IsNullableType() ||
                       !TypeUtils.AreEquivalent(method.ReturnType, left.Type.GetNonNullableType())
                ? method.ReturnType
                : left.Type;
            return new MethodBinaryExpression(ExpressionType.AndAlso, left, right, type, method);
        }

        ValidateUserDefinedConditionalLogicOperator(ExpressionType.AndAlso, left.Type, right.Type, method);
        var type1 = !left.Type.IsNullableType() ||
                    !TypeUtils.AreEquivalent(method.ReturnType, left.Type.GetNonNullableType())
            ? method.ReturnType
            : left.Type;
        return new MethodBinaryExpression(ExpressionType.AndAlso, left, right, type1, method);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise AND assignment
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AndAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression AndAssign(Expression left, Expression right)
    {
        return AndAssign(left, right, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise AND assignment
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AndAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression AndAssign(Expression left, Expression right, MethodInfo method)
    {
        return AndAssign(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise AND assignment
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.AndAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression AndAssign(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.AndAssign, left, right, method, conversion, true);
        }

        if (!(left.Type == right.Type) || !TypeUtils.IsIntegerOrBool(left.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.AndAssign, "op_BitwiseAnd", left, right,
                conversion, true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        return new SimpleBinaryExpression(ExpressionType.AndAssign, left, right, left.Type);
    }

    /// <summary>Creates an <see cref="T:System.Linq.Expressions.IndexExpression" /> to access an array.</summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.IndexExpression" />.</returns>
    /// <param name="array">An expression representing the array to index.</param>
    /// <param name="indexes">An array that contains expressions used to index the array.</param>
    public static IndexExpression ArrayAccess(Expression array, params Expression[] indexes)
    {
        return ArrayAccess(array, (IEnumerable<Expression>)indexes);
    }

    /// <summary>Creates an <see cref="T:System.Linq.Expressions.IndexExpression" /> to access a multidimensional array.</summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.IndexExpression" />.</returns>
    /// <param name="array">An expression that represents the multidimensional array.</param>
    /// <param name="indexes">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> containing expressions used to index
    ///     the array.
    /// </param>
    public static IndexExpression ArrayAccess(Expression array, IEnumerable<Expression> indexes)
    {
        RequiresCanRead(array, nameof(array));
        var type = array.Type;
        if (!type.IsArray)
        {
            throw Error.ArgumentMustBeArray();
        }

        var arguments = indexes.ToReadOnly();
        if (type.GetArrayRank() != arguments.Count)
        {
            throw Error.IncorrectNumberOfIndexes();
        }

        foreach (var expression in arguments)
        {
            RequiresCanRead(expression, nameof(indexes));
            if (expression.Type != typeof(int))
            {
                throw Error.ArgumentMustBeArrayIndexType();
            }
        }

        return new IndexExpression(array, null, arguments);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents applying an array index
    ///     operator to an array of rank one.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ArrayIndex" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="array">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="index">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="array" /> or <paramref name="index" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="array" />.Type does not represent an array type.-or-<paramref name="array" />.Type represents an
    ///     array type whose rank is not 1.-or-<paramref name="index" />.Type does not represent the
    ///     <see cref="T:System.Int32" /> type.
    /// </exception>
    public static BinaryExpression ArrayIndex(Expression array, Expression index)
    {
        RequiresCanRead(array, nameof(array));
        RequiresCanRead(index, nameof(index));
        var type = !(index.Type != typeof(int)) ? array.Type : throw Error.ArgumentMustBeArrayIndexType();
        if (!type.IsArray)
        {
            throw Error.ArgumentMustBeArray();
        }

        if (type.GetArrayRank() != 1)
        {
            throw Error.IncorrectNumberOfIndexes();
        }

        return new SimpleBinaryExpression(ExpressionType.ArrayIndex, array, index, type.GetElementType());
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents applying an array
    ///     index operator to a multidimensional array.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.
    /// </returns>
    /// <param name="array">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> instances - indexes for the array
    ///     index operation.
    /// </param>
    /// <param name="indexes">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="array" /> or <paramref name="indexes" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="array" />.Type does not represent an array type.-or-The rank of <paramref name="array" />.Type does
    ///     not match the number of elements in <paramref name="indexes" />.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of
    ///     <paramref name="indexes" /> does not represent the <see cref="T:System.Int32" /> type.
    /// </exception>
    public static MethodCallExpression ArrayIndex(Expression array, params Expression[] indexes)
    {
        return ArrayIndex(array, (IEnumerable<Expression>)indexes);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents applying an array
    ///     index operator to an array of rank more than one.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.
    /// </returns>
    /// <param name="array">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to.
    /// </param>
    /// <param name="indexes">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="array" /> or <paramref name="indexes" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="array" />.Type does not represent an array type.-or-The rank of <paramref name="array" />.Type does
    ///     not match the number of elements in <paramref name="indexes" />.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of
    ///     <paramref name="indexes" /> does not represent the <see cref="T:System.Int32" /> type.
    /// </exception>
    public static MethodCallExpression ArrayIndex(Expression array, IEnumerable<Expression> indexes)
    {
        RequiresCanRead(array, nameof(array));
        ContractUtils.RequiresNotNull(indexes, nameof(indexes));
        var type = array.Type;
        if (!type.IsArray)
        {
            throw Error.ArgumentMustBeArray();
        }

        var arguments = indexes.ToReadOnly();
        if (type.GetArrayRank() != arguments.Count)
        {
            throw Error.IncorrectNumberOfIndexes();
        }

        foreach (var expression in arguments)
        {
            RequiresCanRead(expression, nameof(indexes));
            if (expression.Type != typeof(int))
            {
                throw Error.ArgumentMustBeArrayIndexType();
            }
        }

        var method = array.Type.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public);
        return Call(array, method, arguments);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an expression for obtaining
    ///     the length of a one-dimensional array.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ArrayLength" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to <paramref name="array" />.
    /// </returns>
    /// <param name="array">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="array" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="array" />.Type does not represent an array type.
    /// </exception>
    public static UnaryExpression ArrayLength(Expression array)
    {
        ContractUtils.RequiresNotNull(array, nameof(array));
        if (!array.Type.IsArray || !typeof(Array).IsAssignableFrom(array.Type))
        {
            throw Error.ArgumentMustBeArray();
        }

        return array.Type.GetArrayRank() == 1
            ? new UnaryExpression(ExpressionType.ArrayLength, array, typeof(int), null)
            : throw Error.ArgumentMustBeSingleDimensionalArrayType();
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an assignment operation.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Assign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression Assign(Expression left, Expression right)
    {
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        TypeUtils.ValidateType(left.Type);
        TypeUtils.ValidateType(right.Type);
        if (!TypeUtils.AreReferenceAssignable(left.Type, right.Type))
        {
            throw Error.ExpressionTypeDoesNotMatchAssignment(right.Type, left.Type);
        }

        return new AssignBinaryExpression(left, right);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberAssignment" /> that represents the initialization of a
    ///     field or property.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberAssignment" /> that has
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.MemberBindingType.Assignment" /> and the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and
    ///     <see cref="P:System.Linq.Expressions.MemberAssignment.Expression" /> properties set to the specified values.
    /// </returns>
    /// <param name="member">
    ///     A <see cref="T:System.Reflection.MemberInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.
    /// </param>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberAssignment.Expression" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="member" /> or <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="member" /> does not represent a field or property.-or-The property represented by
    ///     <paramref name="member" /> does not have a set accessor.-or-<paramref name="expression" />.Type is not assignable
    ///     to the type of the field or property that <paramref name="member" /> represents.
    /// </exception>
    public static MemberAssignment Bind(MemberInfo member, Expression expression)
    {
        ContractUtils.RequiresNotNull(member, nameof(member));
        RequiresCanRead(expression, nameof(expression));
        Type memberType;
        ValidateSettableFieldOrPropertyMember(member, out memberType);
        if (!memberType.IsAssignableFrom(expression.Type))
        {
            throw Error.ArgumentTypesMustMatch();
        }

        return new MemberAssignment(member, expression);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberAssignment" /> that represents the initialization of a
    ///     member by using a property accessor method.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberAssignment" /> that has the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.MemberBindingType.Assignment" />, the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the
    ///     <see cref="T:System.Reflection.PropertyInfo" /> that represents the property accessed in
    ///     <paramref name="propertyAccessor" />, and the <see cref="P:System.Linq.Expressions.MemberAssignment.Expression" />
    ///     property set to <paramref name="expression" />.
    /// </returns>
    /// <param name="propertyAccessor">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor
    ///     method.
    /// </param>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberAssignment.Expression" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="propertyAccessor" /> or <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The property accessed by
    ///     <paramref name="propertyAccessor" /> does not have a set accessor.-or-<paramref name="expression" />.Type is not
    ///     assignable to the type of the field or property that <paramref name="member" /> represents.
    /// </exception>
    public static MemberAssignment Bind(MethodInfo propertyAccessor, Expression expression)
    {
        ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
        ContractUtils.RequiresNotNull(expression, nameof(expression));
        ValidateMethodInfo(propertyAccessor);
        return Bind(GetProperty(propertyAccessor), expression);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains two expressions and has no
    ///     variables.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="arg0">The first expression in the block.</param>
    /// <param name="arg1">The second expression in the block.</param>
    public static BlockExpression Block(Expression arg0, Expression arg1)
    {
        RequiresCanRead(arg0, nameof(arg0));
        RequiresCanRead(arg1, nameof(arg1));
        return new Block2(arg0, arg1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains three expressions and has no
    ///     variables.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="arg0">The first expression in the block.</param>
    /// <param name="arg1">The second expression in the block.</param>
    /// <param name="arg2">The third expression in the block.</param>
    public static BlockExpression Block(Expression arg0, Expression arg1, Expression arg2)
    {
        RequiresCanRead(arg0, nameof(arg0));
        RequiresCanRead(arg1, nameof(arg1));
        RequiresCanRead(arg2, nameof(arg2));
        return new Block3(arg0, arg1, arg2);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains four expressions and has no
    ///     variables.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="arg0">The first expression in the block.</param>
    /// <param name="arg1">The second expression in the block.</param>
    /// <param name="arg2">The third expression in the block.</param>
    /// <param name="arg3">The fourth expression in the block.</param>
    public static BlockExpression Block(
        Expression arg0,
        Expression arg1,
        Expression arg2,
        Expression arg3)
    {
        RequiresCanRead(arg0, nameof(arg0));
        RequiresCanRead(arg1, nameof(arg1));
        RequiresCanRead(arg2, nameof(arg2));
        RequiresCanRead(arg3, nameof(arg3));
        return new Block4(arg0, arg1, arg2, arg3);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains five expressions and has no
    ///     variables.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="arg0">The first expression in the block.</param>
    /// <param name="arg1">The second expression in the block.</param>
    /// <param name="arg2">The third expression in the block.</param>
    /// <param name="arg3">The fourth expression in the block.</param>
    /// <param name="arg4">The fifth expression in the block.</param>
    public static BlockExpression Block(
        Expression arg0,
        Expression arg1,
        Expression arg2,
        Expression arg3,
        Expression arg4)
    {
        RequiresCanRead(arg0, nameof(arg0));
        RequiresCanRead(arg1, nameof(arg1));
        RequiresCanRead(arg2, nameof(arg2));
        RequiresCanRead(arg3, nameof(arg3));
        RequiresCanRead(arg4, nameof(arg4));
        return new Block5(arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains the given expressions and has
    ///     no variables.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="expressions">The expressions in the block.</param>
    public static BlockExpression Block(params Expression[] expressions)
    {
        ContractUtils.RequiresNotNull(expressions, nameof(expressions));
        switch (expressions.Length)
        {
            case 2:
                return Block(expressions[0], expressions[1]);
            case 3:
                return Block(expressions[0], expressions[1], expressions[2]);
            case 4:
                return Block(expressions[0], expressions[1], expressions[2], expressions[3]);
            case 5:
                return Block(expressions[0], expressions[1], expressions[2], expressions[3], expressions[4]);
            default:
                ContractUtils.RequiresNotEmpty(expressions, nameof(expressions));
                RequiresCanRead(expressions, nameof(expressions));
                return new BlockN(CollectionExtensions.Copy(expressions));
        }
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains the given expressions and has
    ///     no variables.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="expressions">The expressions in the block.</param>
    public static BlockExpression Block(IEnumerable<Expression> expressions)
    {
        return Block(EmptyReadOnlyCollection<ParameterExpression>.Instance, expressions);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains the given expressions, has no
    ///     variables and has specific result type.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="type">The result type of the block.</param>
    /// <param name="expressions">The expressions in the block.</param>
    public static BlockExpression Block(Type type, params Expression[] expressions)
    {
        ContractUtils.RequiresNotNull(expressions, nameof(expressions));
        return Block(type, (IEnumerable<Expression>)expressions);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains the given expressions, has no
    ///     variables and has specific result type.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="type">The result type of the block.</param>
    /// <param name="expressions">The expressions in the block.</param>
    public static BlockExpression Block(Type type, IEnumerable<Expression> expressions)
    {
        return Block(type, EmptyReadOnlyCollection<ParameterExpression>.Instance, expressions);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains the given variables and
    ///     expressions.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="variables">The variables in the block.</param>
    /// <param name="expressions">The expressions in the block.</param>
    public static BlockExpression Block(
        IEnumerable<ParameterExpression> variables,
        params Expression[] expressions)
    {
        return Block(variables, (IEnumerable<Expression>)expressions);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains the given variables and
    ///     expressions.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="type">The result type of the block.</param>
    /// <param name="variables">The variables in the block.</param>
    /// <param name="expressions">The expressions in the block.</param>
    public static BlockExpression Block(
        Type type,
        IEnumerable<ParameterExpression> variables,
        params Expression[] expressions)
    {
        return Block(type, variables, (IEnumerable<Expression>)expressions);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains the given variables and
    ///     expressions.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="variables">The variables in the block.</param>
    /// <param name="expressions">The expressions in the block.</param>
    public static BlockExpression Block(
        IEnumerable<ParameterExpression> variables,
        IEnumerable<Expression> expressions)
    {
        ContractUtils.RequiresNotNull(expressions, nameof(expressions));
        var readOnlyCollection = expressions.ToReadOnly();
        ContractUtils.RequiresNotEmpty(readOnlyCollection, nameof(expressions));
        RequiresCanRead(readOnlyCollection, nameof(expressions));
        return Block(readOnlyCollection.Last().Type, variables, readOnlyCollection);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BlockExpression" /> that contains the given variables and
    ///     expressions.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.BlockExpression" />.</returns>
    /// <param name="type">The result type of the block.</param>
    /// <param name="variables">The variables in the block.</param>
    /// <param name="expressions">The expressions in the block.</param>
    public static BlockExpression Block(
        Type type,
        IEnumerable<ParameterExpression> variables,
        IEnumerable<Expression> expressions)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        ContractUtils.RequiresNotNull(expressions, nameof(expressions));
        var readOnlyCollection1 = expressions.ToReadOnly();
        var readOnlyCollection2 = variables.ToReadOnly();
        ContractUtils.RequiresNotEmpty(readOnlyCollection1, nameof(expressions));
        RequiresCanRead(readOnlyCollection1, nameof(expressions));
        ValidateVariables(readOnlyCollection2, nameof(variables));
        var expression = readOnlyCollection1.Last();
        if (type != typeof(void) && !TypeUtils.AreReferenceAssignable(type, expression.Type))
        {
            throw Error.ArgumentTypesMustMatch();
        }

        if (!TypeUtils.AreEquivalent(type, expression.Type))
        {
            return new ScopeWithType(readOnlyCollection2, readOnlyCollection1, type);
        }

        return readOnlyCollection1.Count == 1
            ? new Scope1(readOnlyCollection2, readOnlyCollection1[0])
            : new ScopeN(readOnlyCollection2, readOnlyCollection1);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a break statement.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Break, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and a
    ///     null value to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    public static GotoExpression Break(LabelTarget target)
    {
        return MakeGoto(GotoExpressionKind.Break, target, null, typeof(void));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a break statement. The value
    ///     passed to the label upon jumping can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Break, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and
    ///     <paramref name="value" /> to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
    public static GotoExpression Break(LabelTarget target, Expression value)
    {
        return MakeGoto(GotoExpressionKind.Break, target, value, typeof(void));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a break statement with the
    ///     specified type.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Break, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    /// <param name="type">
    ///     An <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    public static GotoExpression Break(LabelTarget target, Type type)
    {
        return MakeGoto(GotoExpressionKind.Break, target, null, type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a break statement with the
    ///     specified type. The value passed to the label upon jumping can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Break, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and
    ///     <paramref name="value" /> to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
    /// <param name="type">
    ///     An <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    public static GotoExpression Break(LabelTarget target, Expression value, Type type)
    {
        return MakeGoto(GotoExpressionKind.Break, target, value, type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static
    ///     (Shared in Visual Basic) method that takes one argument.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.
    /// </param>
    /// <param name="arg0">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the first argument.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="method" /> is null.
    /// </exception>
    public static MethodCallExpression Call(MethodInfo method, Expression arg0)
    {
        ContractUtils.RequiresNotNull(method, nameof(method));
        ContractUtils.RequiresNotNull(arg0, nameof(arg0));
        var parameters = ValidateMethodAndGetParameters(null, method);
        ValidateArgumentCount(method, ExpressionType.Call, 1, parameters);
        arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, parameters[0]);
        return new MethodCallExpression1(method, arg0);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static
    ///     method that takes two arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.
    /// </param>
    /// <param name="arg0">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the first argument.</param>
    /// <param name="arg1">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the second argument.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="method" /> is null.
    /// </exception>
    public static MethodCallExpression Call(MethodInfo method, Expression arg0, Expression arg1)
    {
        ContractUtils.RequiresNotNull(method, nameof(method));
        ContractUtils.RequiresNotNull(arg0, nameof(arg0));
        ContractUtils.RequiresNotNull(arg1, nameof(arg1));
        var parameters = ValidateMethodAndGetParameters(null, method);
        ValidateArgumentCount(method, ExpressionType.Call, 2, parameters);
        arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, parameters[0]);
        arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, parameters[1]);
        return new MethodCallExpression2(method, arg0, arg1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static
    ///     method that takes three arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.
    /// </param>
    /// <param name="arg0">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the first argument.</param>
    /// <param name="arg1">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the second argument.</param>
    /// <param name="arg2">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the third argument.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="method" /> is null.
    /// </exception>
    public static MethodCallExpression Call(
        MethodInfo method,
        Expression arg0,
        Expression arg1,
        Expression arg2)
    {
        ContractUtils.RequiresNotNull(method, nameof(method));
        ContractUtils.RequiresNotNull(arg0, nameof(arg0));
        ContractUtils.RequiresNotNull(arg1, nameof(arg1));
        ContractUtils.RequiresNotNull(arg2, nameof(arg2));
        var parameters = ValidateMethodAndGetParameters(null, method);
        ValidateArgumentCount(method, ExpressionType.Call, 3, parameters);
        arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, parameters[0]);
        arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, parameters[1]);
        arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, parameters[2]);
        return new MethodCallExpression3(method, arg0, arg1, arg2);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static
    ///     method that takes four arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.
    /// </param>
    /// <param name="arg0">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the first argument.</param>
    /// <param name="arg1">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the second argument.</param>
    /// <param name="arg2">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the third argument.</param>
    /// <param name="arg3">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the fourth argument.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="method" /> is null.
    /// </exception>
    public static MethodCallExpression Call(
        MethodInfo method,
        Expression arg0,
        Expression arg1,
        Expression arg2,
        Expression arg3)
    {
        ContractUtils.RequiresNotNull(method, nameof(method));
        ContractUtils.RequiresNotNull(arg0, nameof(arg0));
        ContractUtils.RequiresNotNull(arg1, nameof(arg1));
        ContractUtils.RequiresNotNull(arg2, nameof(arg2));
        ContractUtils.RequiresNotNull(arg3, nameof(arg3));
        var parameters = ValidateMethodAndGetParameters(null, method);
        ValidateArgumentCount(method, ExpressionType.Call, 4, parameters);
        arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, parameters[0]);
        arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, parameters[1]);
        arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, parameters[2]);
        arg3 = ValidateOneArgument(method, ExpressionType.Call, arg3, parameters[3]);
        return new MethodCallExpression4(method, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static
    ///     method that takes five arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.
    /// </param>
    /// <param name="arg0">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the first argument.</param>
    /// <param name="arg1">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the second argument.</param>
    /// <param name="arg2">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the third argument.</param>
    /// <param name="arg3">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the fourth argument.</param>
    /// <param name="arg4">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the fifth argument.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="method" /> is null.
    /// </exception>
    public static MethodCallExpression Call(
        MethodInfo method,
        Expression arg0,
        Expression arg1,
        Expression arg2,
        Expression arg3,
        Expression arg4)
    {
        ContractUtils.RequiresNotNull(method, nameof(method));
        ContractUtils.RequiresNotNull(arg0, nameof(arg0));
        ContractUtils.RequiresNotNull(arg1, nameof(arg1));
        ContractUtils.RequiresNotNull(arg2, nameof(arg2));
        ContractUtils.RequiresNotNull(arg3, nameof(arg3));
        ContractUtils.RequiresNotNull(arg4, nameof(arg4));
        var parameters = ValidateMethodAndGetParameters(null, method);
        ValidateArgumentCount(method, ExpressionType.Call, 5, parameters);
        arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, parameters[0]);
        arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, parameters[1]);
        arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, parameters[2]);
        arg3 = ValidateOneArgument(method, ExpressionType.Call, arg3, parameters[3]);
        arg4 = ValidateOneArgument(method, ExpressionType.Call, arg4, parameters[4]);
        return new MethodCallExpression5(method, arg0, arg1, arg2, arg3, arg4);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static
    ///     (Shared in Visual Basic) method that has arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.
    /// </returns>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> that represents a static (Shared in Visual Basic)
    ///     method to set the <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.
    /// </param>
    /// <param name="arguments">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="method" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The number of elements in <paramref name="arguments" /> does not equal the
    ///     number of parameters for the method represented by <paramref name="method" />.-or-One or more of the elements of
    ///     <paramref name="arguments" /> is not assignable to the corresponding parameter for the method represented by
    ///     <paramref name="method" />.
    /// </exception>
    public static MethodCallExpression Call(MethodInfo method, params Expression[] arguments)
    {
        return Call(null, method, arguments);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static
    ///     (Shared in Visual Basic) method.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="method">The <see cref="T:System.Reflection.MethodInfo" /> that represents the target method.</param>
    /// <param name="arguments">
    ///     A collection of <see cref="T:System.Linq.Expressions.Expression" /> that represents the call
    ///     arguments.
    /// </param>
    public static MethodCallExpression Call(MethodInfo method, IEnumerable<Expression> arguments)
    {
        return Call(null, method, arguments);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to an instance
    ///     method that takes no arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="instance">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> that specifies the instance for an
    ///     instance method call (pass null for a static (Shared in Visual Basic) method).
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="method" /> is null.-or-<paramref name="instance" /> is null and <paramref name="method" />
    ///     represents an instance method.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="instance" />.Type is not assignable to the declaring type of the method represented by
    ///     <paramref name="method" />.
    /// </exception>
    public static MethodCallExpression Call(Expression instance, MethodInfo method)
    {
        return Call(instance, method, EmptyReadOnlyCollection<Expression>.Instance);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method
    ///     that takes arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" />,
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.
    /// </returns>
    /// <param name="instance">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> that specifies the instance fo an
    ///     instance method call (pass null for a static (Shared in Visual Basic) method).
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.
    /// </param>
    /// <param name="arguments">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="method" /> is null.-or-<paramref name="instance" /> is null and <paramref name="method" />
    ///     represents an instance method.-or-<paramref name="arguments" /> is not null and one or more of its elements is
    ///     null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="instance" />.Type is not assignable to the declaring type of the method represented by
    ///     <paramref name="method" />.-or-The number of elements in <paramref name="arguments" /> does not equal the number of
    ///     parameters for the method represented by <paramref name="method" />.-or-One or more of the elements of
    ///     <paramref name="arguments" /> is not assignable to the corresponding parameter for the method represented by
    ///     <paramref name="method" />.
    /// </exception>
    public static MethodCallExpression Call(
        Expression instance,
        MethodInfo method,
        params Expression[] arguments)
    {
        return Call(instance, method, (IEnumerable<Expression>)arguments);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to an instance
    ///     method that takes two arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="instance">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> that specifies the instance for an
    ///     instance call. (pass null for a static (Shared in Visual Basic) method).
    /// </param>
    /// <param name="method">The <see cref="T:System.Reflection.MethodInfo" /> that represents the target method.</param>
    /// <param name="arg0">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the first argument.</param>
    /// <param name="arg1">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the second argument.</param>
    public static MethodCallExpression Call(
        Expression instance,
        MethodInfo method,
        Expression arg0,
        Expression arg1)
    {
        ContractUtils.RequiresNotNull(method, nameof(method));
        ContractUtils.RequiresNotNull(arg0, nameof(arg0));
        ContractUtils.RequiresNotNull(arg1, nameof(arg1));
        var parameters = ValidateMethodAndGetParameters(instance, method);
        ValidateArgumentCount(method, ExpressionType.Call, 2, parameters);
        arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, parameters[0]);
        arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, parameters[1]);
        return instance != null
            ? new InstanceMethodCallExpression2(method, instance, arg0, arg1)
            : new MethodCallExpression2(method, arg0, arg1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method
    ///     that takes three arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="instance">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> that specifies the instance for an
    ///     instance call. (pass null for a static (Shared in Visual Basic) method).
    /// </param>
    /// <param name="method">The <see cref="T:System.Reflection.MethodInfo" /> that represents the target method.</param>
    /// <param name="arg0">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the first argument.</param>
    /// <param name="arg1">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the second argument.</param>
    /// <param name="arg2">The <see cref="T:System.Linq.Expressions.Expression" /> that represents the third argument.</param>
    public static MethodCallExpression Call(
        Expression instance,
        MethodInfo method,
        Expression arg0,
        Expression arg1,
        Expression arg2)
    {
        ContractUtils.RequiresNotNull(method, nameof(method));
        ContractUtils.RequiresNotNull(arg0, nameof(arg0));
        ContractUtils.RequiresNotNull(arg1, nameof(arg1));
        ContractUtils.RequiresNotNull(arg2, nameof(arg2));
        var parameters = ValidateMethodAndGetParameters(instance, method);
        ValidateArgumentCount(method, ExpressionType.Call, 3, parameters);
        arg0 = ValidateOneArgument(method, ExpressionType.Call, arg0, parameters[0]);
        arg1 = ValidateOneArgument(method, ExpressionType.Call, arg1, parameters[1]);
        arg2 = ValidateOneArgument(method, ExpressionType.Call, arg2, parameters[2]);
        return instance != null
            ? new InstanceMethodCallExpression3(method, instance, arg0, arg1, arg2)
            : new MethodCallExpression3(method, arg0, arg1, arg2);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to an instance
    ///     method by calling the appropriate factory method.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" />, the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to <paramref name="instance" />
    ///     , <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> set to the
    ///     <see cref="T:System.Reflection.MethodInfo" /> that represents the specified instance method, and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> set to the specified arguments.
    /// </returns>
    /// <param name="instance">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> whose
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property value will be searched for a specific method.
    /// </param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="typeArguments">
    ///     An array of <see cref="T:System.Type" /> objects that specify the type parameters of the
    ///     generic method. This argument should be null when methodName specifies a non-generic method.
    /// </param>
    /// <param name="arguments">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects that represents the
    ///     arguments to the method.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="instance" /> or <paramref name="methodName" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     No method whose name is <paramref name="methodName" />, whose type
    ///     parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" />
    ///     is found in <paramref name="instance" />.Type or its base types.-or-More than one method whose name is
    ///     <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter
    ///     types match <paramref name="arguments" /> is found in <paramref name="instance" />.Type or its base types.
    /// </exception>
    public static MethodCallExpression Call(
        Expression instance,
        string methodName,
        Type[] typeArguments,
        params Expression[] arguments)
    {
        ContractUtils.RequiresNotNull(instance, nameof(instance));
        ContractUtils.RequiresNotNull(methodName, nameof(methodName));
        if (arguments == null)
        {
            arguments = new Expression[0];
        }

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.FlattenHierarchy;
        return Call(instance, FindMethod(instance.Type, methodName, typeArguments, arguments, flags), arguments);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a static
    ///     (Shared in Visual Basic) method by calling the appropriate factory method.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" />, the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property set to the
    ///     <see cref="T:System.Reflection.MethodInfo" /> that represents the specified static (Shared in Visual Basic) method,
    ///     and the <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> property set to the specified
    ///     arguments.
    /// </returns>
    /// <param name="type">
    ///     The <see cref="T:System.Type" /> that specifies the type that contains the specified static (Shared
    ///     in Visual Basic) method.
    /// </param>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="typeArguments">
    ///     An array of <see cref="T:System.Type" /> objects that specify the type parameters of the
    ///     generic method. This argument should be null when methodName specifies a non-generic method.
    /// </param>
    /// <param name="arguments">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects that represent the
    ///     arguments to the method.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="type" /> or <paramref name="methodName" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     No method whose name is <paramref name="methodName" />, whose type
    ///     parameters match <paramref name="typeArguments" />, and whose parameter types match <paramref name="arguments" />
    ///     is found in <paramref name="type" /> or its base types.-or-More than one method whose name is
    ///     <paramref name="methodName" />, whose type parameters match <paramref name="typeArguments" />, and whose parameter
    ///     types match <paramref name="arguments" /> is found in <paramref name="type" /> or its base types.
    /// </exception>
    public static MethodCallExpression Call(
        Type type,
        string methodName,
        Type[] typeArguments,
        params Expression[] arguments)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        ContractUtils.RequiresNotNull(methodName, nameof(methodName));
        if (arguments == null)
        {
            arguments = new Expression[0];
        }

        var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
        return Call(null, FindMethod(type, methodName, typeArguments, arguments, flags), arguments);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that represents a call to a method
    ///     that takes arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MethodCallExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Call" /> and the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" />,
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> properties set to the specified values.
    /// </returns>
    /// <param name="instance">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property equal to (pass null for a static
    ///     (Shared in Visual Basic) method).
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Method" /> property equal to.
    /// </param>
    /// <param name="arguments">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="method" /> is null.-or-<paramref name="instance" /> is null and <paramref name="method" />
    ///     represents an instance method.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="instance" />.Type is not assignable to the declaring type of the method represented by
    ///     <paramref name="method" />.-or-The number of elements in <paramref name="arguments" /> does not equal the number of
    ///     parameters for the method represented by <paramref name="method" />.-or-One or more of the elements of
    ///     <paramref name="arguments" /> is not assignable to the corresponding parameter for the method represented by
    ///     <paramref name="method" />.
    /// </exception>
    public static MethodCallExpression Call(
        Expression instance,
        MethodInfo method,
        IEnumerable<Expression> arguments)
    {
        ContractUtils.RequiresNotNull(method, nameof(method));
        var arguments1 = arguments.ToReadOnly();
        ValidateMethodInfo(method);
        ValidateStaticOrInstanceMethod(instance, method);
        ValidateArgumentTypes(method, ExpressionType.Call, ref arguments1);
        return instance == null
            ? new MethodCallExpressionN(method, arguments1)
            : new InstanceMethodCallExpressionN(method, instance, arguments1);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.CatchBlock" /> representing a catch statement.</summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.CatchBlock" />.</returns>
    /// <param name="type">
    ///     The <see cref="P:System.Linq.Expressions.Expression.Type" /> of <see cref="T:System.Exception" />
    ///     this <see cref="T:System.Linq.Expressions.CatchBlock" /> will handle.
    /// </param>
    /// <param name="body">The body of the catch statement.</param>
    public static CatchBlock Catch(Type type, Expression body)
    {
        return MakeCatchBlock(type, null, body, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.CatchBlock" /> representing a catch statement with a reference
    ///     to the caught <see cref="T:System.Exception" /> object for use in the handler body.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.CatchBlock" />.</returns>
    /// <param name="variable">
    ///     A <see cref="T:System.Linq.Expressions.ParameterExpression" /> representing a reference to the
    ///     <see cref="T:System.Exception" /> object caught by this handler.
    /// </param>
    /// <param name="body">The body of the catch statement.</param>
    public static CatchBlock Catch(ParameterExpression variable, Expression body)
    {
        ContractUtils.RequiresNotNull(variable, nameof(variable));
        return MakeCatchBlock(variable.Type, variable, body, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.CatchBlock" /> representing a catch statement with an
    ///     <see cref="T:System.Exception" /> filter but no reference to the caught <see cref="T:System.Exception" /> object.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.CatchBlock" />.</returns>
    /// <param name="type">
    ///     The <see cref="P:System.Linq.Expressions.Expression.Type" /> of <see cref="T:System.Exception" />
    ///     this <see cref="T:System.Linq.Expressions.CatchBlock" /> will handle.
    /// </param>
    /// <param name="body">The body of the catch statement.</param>
    /// <param name="filter">The body of the <see cref="T:System.Exception" /> filter.</param>
    public static CatchBlock Catch(Type type, Expression body, Expression filter)
    {
        return MakeCatchBlock(type, null, body, filter);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.CatchBlock" /> representing a catch statement with an
    ///     <see cref="T:System.Exception" /> filter and a reference to the caught <see cref="T:System.Exception" /> object.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.CatchBlock" />.</returns>
    /// <param name="variable">
    ///     A <see cref="T:System.Linq.Expressions.ParameterExpression" /> representing a reference to the
    ///     <see cref="T:System.Exception" /> object caught by this handler.
    /// </param>
    /// <param name="body">The body of the catch statement.</param>
    /// <param name="filter">The body of the <see cref="T:System.Exception" /> filter.</param>
    public static CatchBlock Catch(ParameterExpression variable, Expression body, Expression filter)
    {
        ContractUtils.RequiresNotNull(variable, nameof(variable));
        return MakeCatchBlock(variable.Type, variable, body, filter);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.DebugInfoExpression" /> for clearing a sequence point.</summary>
    /// <returns>An instance of <see cref="T:System.Linq.Expressions.DebugInfoExpression" /> for clearning a sequence point.</returns>
    /// <param name="document">The <see cref="T:System.Linq.Expressions.SymbolDocumentInfo" /> that represents the source file.</param>
    public static DebugInfoExpression ClearDebugInfo(SymbolDocumentInfo document)
    {
        ContractUtils.RequiresNotNull(document, nameof(document));
        return new ClearDebugInfoExpression(document);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a coalescing operation.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Coalesce" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property of <paramref name="left" /> does not represent a reference type or a nullable value type.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type are not convertible to each other.
    /// </exception>
    public static BinaryExpression Coalesce(Expression left, Expression right)
    {
        return Coalesce(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a coalescing operation,
    ///     given a conversion function.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Coalesce" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type are not convertible to each other.-or-
    ///     <paramref name="conversion" /> is not null and <paramref name="conversion" />.Type is a delegate type that does not
    ///     take exactly one argument.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property of <paramref name="left" /> does not represent a reference type or a nullable value type.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of <paramref name="left" /> represents a type
    ///     that is not assignable to the parameter type of the delegate type <paramref name="conversion" />.Type.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of <paramref name="right" /> is not equal to the
    ///     return type of the delegate type <paramref name="conversion" />.Type.
    /// </exception>
    public static BinaryExpression Coalesce(
        Expression left,
        Expression right,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (conversion == null)
        {
            var type = ValidateCoalesceArgTypes(left.Type, right.Type);
            return new SimpleBinaryExpression(ExpressionType.Coalesce, left, right, type);
        }

        if (left.Type.IsValueType && !left.Type.IsNullableType())
        {
            throw Error.CoalesceUsedOnNonNullType();
        }

        var method = conversion.Type.GetMethod("Invoke");
        var parameterInfoArray = !(method.ReturnType == typeof(void))
            ? method.GetParameters()
            : throw Error.UserDefinedOperatorMustNotBeVoid(conversion);
        if (parameterInfoArray.Length != 1)
        {
            throw Error.IncorrectNumberOfMethodCallArguments(conversion);
        }

        if (!TypeUtils.AreEquivalent(method.ReturnType, right.Type))
        {
            throw Error.OperandTypesDoNotMatchParameters(ExpressionType.Coalesce, conversion.ToString());
        }

        if (!ParameterIsAssignable(parameterInfoArray[0], left.Type.GetNonNullableType()) &&
            !ParameterIsAssignable(parameterInfoArray[0], left.Type))
        {
            throw Error.OperandTypesDoNotMatchParameters(ExpressionType.Coalesce, conversion.ToString());
        }

        return new CoalesceConversionBinaryExpression(left, right, conversion);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ConditionalExpression" /> that represents a conditional
    ///     statement.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ConditionalExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Conditional" /> and the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.Test" />,
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfTrue" />, and
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfFalse" /> properties set to the specified values.
    /// </returns>
    /// <param name="test">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.Test" /> property equal to.
    /// </param>
    /// <param name="ifTrue">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfTrue" /> property equal to.
    /// </param>
    /// <param name="ifFalse">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfFalse" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="test" /> or <paramref name="ifTrue" /> or <paramref name="ifFalse" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="test" />.Type is not <see cref="T:System.Boolean" />.-or-<paramref name="ifTrue" />.Type is not
    ///     equal to <paramref name="ifFalse" />.Type.
    /// </exception>
    public static ConditionalExpression Condition(
        Expression test,
        Expression ifTrue,
        Expression ifFalse)
    {
        RequiresCanRead(test, nameof(test));
        RequiresCanRead(ifTrue, nameof(ifTrue));
        RequiresCanRead(ifFalse, nameof(ifFalse));
        if (test.Type != typeof(bool))
        {
            throw Error.ArgumentMustBeBoolean();
        }

        if (!TypeUtils.AreEquivalent(ifTrue.Type, ifFalse.Type))
        {
            throw Error.ArgumentTypesMustMatch();
        }

        return ConditionalExpression.Make(test, ifTrue, ifFalse, ifTrue.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ConditionalExpression" /> that represents a conditional
    ///     statement.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ConditionalExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Conditional" /> and the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.Test" />,
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfTrue" />, and
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfFalse" /> properties set to the specified values.
    /// </returns>
    /// <param name="test">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.Test" /> property equal to.
    /// </param>
    /// <param name="ifTrue">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfTrue" /> property equal to.
    /// </param>
    /// <param name="ifFalse">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfFalse" /> property equal to.
    /// </param>
    /// <param name="type">
    ///     A <see cref="P:System.Linq.Expressions.Expression.Type" /> to set the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property equal to.
    /// </param>
    public static ConditionalExpression Condition(
        Expression test,
        Expression ifTrue,
        Expression ifFalse,
        Type type)
    {
        RequiresCanRead(test, nameof(test));
        RequiresCanRead(ifTrue, nameof(ifTrue));
        RequiresCanRead(ifFalse, nameof(ifFalse));
        ContractUtils.RequiresNotNull(type, nameof(type));
        if (test.Type != typeof(bool))
        {
            throw Error.ArgumentMustBeBoolean();
        }

        if (type != typeof(void) && (!TypeUtils.AreReferenceAssignable(type, ifTrue.Type) ||
                                     !TypeUtils.AreReferenceAssignable(type, ifFalse.Type)))
        {
            throw Error.ArgumentTypesMustMatch();
        }

        return ConditionalExpression.Make(test, ifTrue, ifFalse, type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ConstantExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> property set to the specified value.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ConstantExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Constant" /> and the
    ///     <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> property set to the specified value.
    /// </returns>
    /// <param name="value">
    ///     An <see cref="T:System.Object" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> property equal to.
    /// </param>
    public static ConstantExpression Constant(object value)
    {
        return ConstantExpression.Make(value, value == null ? typeof(object) : value.GetType());
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ConstantExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> and
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> properties set to the specified values.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ConstantExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Constant" /> and the
    ///     <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> and
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> properties set to the specified values.
    /// </returns>
    /// <param name="value">
    ///     An <see cref="T:System.Object" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConstantExpression.Value" /> property equal to.
    /// </param>
    /// <param name="type">
    ///     A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="type" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="value" /> is not null and <paramref name="type" /> is not assignable from the dynamic type of
    ///     <paramref name="value" />.
    /// </exception>
    public static ConstantExpression Constant(object value, Type type)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        if (value == null && type.IsValueType && !type.IsNullableType())
        {
            throw Error.ArgumentTypesMustMatch();
        }

        return value == null || type.IsAssignableFrom(value.GetType())
            ? ConstantExpression.Make(value, type)
            : throw Error.ArgumentTypesMustMatch();
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a continue statement.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Continue, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and a
    ///     null value to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    public static GotoExpression Continue(LabelTarget target)
    {
        return MakeGoto(GotoExpressionKind.Continue, target, null, typeof(void));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a continue statement with the
    ///     specified type.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Continue, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and a null value
    ///     to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    /// <param name="type">
    ///     An <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    public static GotoExpression Continue(LabelTarget target, Type type)
    {
        return MakeGoto(GotoExpressionKind.Continue, target, null, type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a type conversion
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Convert" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> properties set to the specified values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <param name="type">
    ///     A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> or <paramref name="type" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     No conversion operator is defined between
    ///     <paramref name="expression" />.Type and <paramref name="type" />.
    /// </exception>
    public static UnaryExpression Convert(Expression expression, Type type)
    {
        return Convert(expression, type, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a conversion operation for
    ///     which the implementing method is specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Convert" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" />,
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" />, and
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <param name="type">
    ///     A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> or <paramref name="type" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly one argument.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     No conversion operator is defined between
    ///     <paramref name="expression" />.Type and <paramref name="type" />.-or-<paramref name="expression" />.Type is not
    ///     assignable to the argument type of the method represented by <paramref name="method" />.-or-The return type of the
    ///     method represented by <paramref name="method" /> is not assignable to <paramref name="type" />.-or-
    ///     <paramref name="expression" />.Type or <paramref name="type" /> is a nullable value type and the corresponding
    ///     non-nullable value type does not equal the argument type or the return type, respectively, of the method
    ///     represented by <paramref name="method" />.
    /// </exception>
    /// <exception cref="T:System.Reflection.AmbiguousMatchException">
    ///     More than one method that matches the
    ///     <paramref name="method" /> description was found.
    /// </exception>
    public static UnaryExpression Convert(Expression expression, Type type, MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        ContractUtils.RequiresNotNull(type, nameof(type));
        TypeUtils.ValidateType(type);
        if (!(method == null))
        {
            return GetMethodBasedCoercionOperator(ExpressionType.Convert, expression, type, method);
        }

        return TypeUtils.HasIdentityPrimitiveOrNullableConversion(expression.Type, type) ||
               TypeUtils.HasReferenceConversion(expression.Type, type)
            ? new UnaryExpression(ExpressionType.Convert, expression, type, null)
            : GetUserDefinedCoercionOrThrow(ExpressionType.Convert, expression, type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a conversion operation that
    ///     throws an exception if the target type is overflowed.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ConvertChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> properties set to the specified values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <param name="type">
    ///     A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> or <paramref name="type" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     No conversion operator is defined between
    ///     <paramref name="expression" />.Type and <paramref name="type" />.
    /// </exception>
    public static UnaryExpression ConvertChecked(Expression expression, Type type)
    {
        return ConvertChecked(expression, type, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a conversion operation that
    ///     throws an exception if the target type is overflowed and for which the implementing method is specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ConvertChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" />,
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" />, and
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <param name="type">
    ///     A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> or <paramref name="type" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly one argument.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     No conversion operator is defined between
    ///     <paramref name="expression" />.Type and <paramref name="type" />.-or-<paramref name="expression" />.Type is not
    ///     assignable to the argument type of the method represented by <paramref name="method" />.-or-The return type of the
    ///     method represented by <paramref name="method" /> is not assignable to <paramref name="type" />.-or-
    ///     <paramref name="expression" />.Type or <paramref name="type" /> is a nullable value type and the corresponding
    ///     non-nullable value type does not equal the argument type or the return type, respectively, of the method
    ///     represented by <paramref name="method" />.
    /// </exception>
    /// <exception cref="T:System.Reflection.AmbiguousMatchException">
    ///     More than one method that matches the
    ///     <paramref name="method" /> description was found.
    /// </exception>
    public static UnaryExpression ConvertChecked(Expression expression, Type type, MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        ContractUtils.RequiresNotNull(type, nameof(type));
        TypeUtils.ValidateType(type);
        if (!(method == null))
        {
            return GetMethodBasedCoercionOperator(ExpressionType.ConvertChecked, expression, type, method);
        }

        if (TypeUtils.HasIdentityPrimitiveOrNullableConversion(expression.Type, type))
        {
            return new UnaryExpression(ExpressionType.ConvertChecked, expression, type, null);
        }

        return TypeUtils.HasReferenceConversion(expression.Type, type)
            ? new UnaryExpression(ExpressionType.Convert, expression, type, null)
            : GetUserDefinedCoercionOrThrow(ExpressionType.ConvertChecked, expression, type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.DebugInfoExpression" /> with the specified span.</summary>
    /// <returns>An instance of <see cref="T:System.Linq.Expressions.DebugInfoExpression" />.</returns>
    /// <param name="document">The <see cref="T:System.Linq.Expressions.SymbolDocumentInfo" /> that represents the source file.</param>
    /// <param name="startLine">
    ///     The start line of this <see cref="T:System.Linq.Expressions.DebugInfoExpression" />. Must be
    ///     greater than 0.
    /// </param>
    /// <param name="startColumn">
    ///     The start column of this <see cref="T:System.Linq.Expressions.DebugInfoExpression" />. Must
    ///     be greater than 0.
    /// </param>
    /// <param name="endLine">
    ///     The end line of this <see cref="T:System.Linq.Expressions.DebugInfoExpression" />. Must be
    ///     greater or equal than the start line.
    /// </param>
    /// <param name="endColumn">
    ///     The end column of this <see cref="T:System.Linq.Expressions.DebugInfoExpression" />. If the end
    ///     line is the same as the start line, it must be greater or equal than the start column. In any case, must be greater
    ///     than 0.
    /// </param>
    public static DebugInfoExpression DebugInfo(
        SymbolDocumentInfo document,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn)
    {
        ContractUtils.RequiresNotNull(document, nameof(document));
        if (startLine == 16707566 /*0xFEEFEE*/ && startColumn == 0 && endLine == 16707566 /*0xFEEFEE*/ &&
            endColumn == 0)
        {
            return new ClearDebugInfoExpression(document);
        }

        ValidateSpan(startLine, startColumn, endLine, endColumn);
        return new SpanDebugInfoExpression(document, startLine, startColumn, endLine, endColumn);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the decrementing of the
    ///     expression by 1.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the decremented expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to decrement.</param>
    public static UnaryExpression Decrement(Expression expression)
    {
        return Decrement(expression, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the decrementing of the
    ///     expression by 1.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the decremented expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to decrement.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</param>
    public static UnaryExpression Decrement(Expression expression, MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        if (!(method == null))
        {
            return GetMethodBasedUnaryOperator(ExpressionType.Decrement, expression, method);
        }

        return TypeUtils.IsArithmetic(expression.Type)
            ? new UnaryExpression(ExpressionType.Decrement, expression, expression.Type, null)
            : GetUserDefinedUnaryOperatorOrThrow(ExpressionType.Decrement, "op_Decrement", expression);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DefaultExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property set to the specified type.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DefaultExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Default" /> and the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property set to the specified type.
    /// </returns>
    /// <param name="type">
    ///     A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    public static DefaultExpression Default(Type type)
    {
        return type == typeof(void) ? Empty() : new DefaultExpression(type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic division
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Divide" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The division operator is not defined for <paramref name="left" />
    ///     .Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Divide(Expression left, Expression right)
    {
        return Divide(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic division
    ///     operation. The implementing method can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Divide" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the division operator is not defined for <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Divide(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.Divide, left, right, method, true);
        }

        return left.Type == right.Type && TypeUtils.IsArithmetic(left.Type)
            ? new SimpleBinaryExpression(ExpressionType.Divide, left, right, left.Type)
            : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Divide, "op_Division", left, right, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a division assignment
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.DivideAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression DivideAssign(Expression left, Expression right)
    {
        return DivideAssign(left, right, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a division assignment
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.DivideAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression DivideAssign(Expression left, Expression right, MethodInfo method)
    {
        return DivideAssign(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a division assignment
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.DivideAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression DivideAssign(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.DivideAssign, left, right, method, conversion, true);
        }

        if (!(left.Type == right.Type) || !TypeUtils.IsArithmetic(left.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.DivideAssign, "op_Division", left, right,
                conversion, true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        return new SimpleBinaryExpression(ExpressionType.DivideAssign, left, right, left.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" /> and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="returnType">The result type of the dynamic expression.</param>
    /// <param name="arguments">The arguments to the dynamic operation.</param>
    public static DynamicExpression Dynamic(
        CallSiteBinder binder,
        Type returnType,
        params Expression[] arguments)
    {
        return Dynamic(binder, returnType, (IEnumerable<Expression>)arguments);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" /> and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="returnType">The result type of the dynamic expression.</param>
    /// <param name="arg0">The first argument to the dynamic operation.</param>
    public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        ValidateDynamicArgument(arg0);
        var nextTypeInfo = DelegateHelpers.GetNextTypeInfo(returnType,
            DelegateHelpers.GetNextTypeInfo(arg0.Type, DelegateHelpers.NextTypeInfo(typeof(CallSite))));
        var delegateType = nextTypeInfo.DelegateType;
        if (delegateType == null)
        {
            delegateType = nextTypeInfo.MakeDelegateType(returnType, arg0);
        }

        return DynamicExpression.Make(returnType, delegateType, binder, arg0);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" /> and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="returnType">The result type of the dynamic expression.</param>
    /// <param name="arg0">The first argument to the dynamic operation.</param>
    /// <param name="arg1">The second argument to the dynamic operation.</param>
    public static DynamicExpression Dynamic(
        CallSiteBinder binder,
        Type returnType,
        Expression arg0,
        Expression arg1)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        ValidateDynamicArgument(arg0);
        ValidateDynamicArgument(arg1);
        var nextTypeInfo = DelegateHelpers.GetNextTypeInfo(returnType,
            DelegateHelpers.GetNextTypeInfo(arg1.Type,
                DelegateHelpers.GetNextTypeInfo(arg0.Type, DelegateHelpers.NextTypeInfo(typeof(CallSite)))));
        var delegateType = nextTypeInfo.DelegateType;
        if (delegateType == null)
        {
            delegateType = nextTypeInfo.MakeDelegateType(returnType, arg0, arg1);
        }

        return DynamicExpression.Make(returnType, delegateType, binder, arg0, arg1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" /> and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="returnType">The result type of the dynamic expression.</param>
    /// <param name="arg0">The first argument to the dynamic operation.</param>
    /// <param name="arg1">The second argument to the dynamic operation.</param>
    /// <param name="arg2">The third argument to the dynamic operation.</param>
    public static DynamicExpression Dynamic(
        CallSiteBinder binder,
        Type returnType,
        Expression arg0,
        Expression arg1,
        Expression arg2)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        ValidateDynamicArgument(arg0);
        ValidateDynamicArgument(arg1);
        ValidateDynamicArgument(arg2);
        var nextTypeInfo = DelegateHelpers.GetNextTypeInfo(returnType,
            DelegateHelpers.GetNextTypeInfo(arg2.Type,
                DelegateHelpers.GetNextTypeInfo(arg1.Type,
                    DelegateHelpers.GetNextTypeInfo(arg0.Type, DelegateHelpers.NextTypeInfo(typeof(CallSite))))));
        var delegateType = nextTypeInfo.DelegateType;
        if (delegateType == null)
        {
            delegateType = nextTypeInfo.MakeDelegateType(returnType, arg0, arg1, arg2);
        }

        return DynamicExpression.Make(returnType, delegateType, binder, arg0, arg1, arg2);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" /> and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="returnType">The result type of the dynamic expression.</param>
    /// <param name="arg0">The first argument to the dynamic operation.</param>
    /// <param name="arg1">The second argument to the dynamic operation.</param>
    /// <param name="arg2">The third argument to the dynamic operation.</param>
    /// <param name="arg3">The fourth argument to the dynamic operation.</param>
    public static DynamicExpression Dynamic(
        CallSiteBinder binder,
        Type returnType,
        Expression arg0,
        Expression arg1,
        Expression arg2,
        Expression arg3)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        ValidateDynamicArgument(arg0);
        ValidateDynamicArgument(arg1);
        ValidateDynamicArgument(arg2);
        ValidateDynamicArgument(arg3);
        var nextTypeInfo = DelegateHelpers.GetNextTypeInfo(returnType,
            DelegateHelpers.GetNextTypeInfo(arg3.Type,
                DelegateHelpers.GetNextTypeInfo(arg2.Type,
                    DelegateHelpers.GetNextTypeInfo(arg1.Type,
                        DelegateHelpers.GetNextTypeInfo(arg0.Type, DelegateHelpers.NextTypeInfo(typeof(CallSite)))))));
        var delegateType = nextTypeInfo.DelegateType;
        if (delegateType == null)
        {
            delegateType = nextTypeInfo.MakeDelegateType(returnType, arg0, arg1, arg2, arg3);
        }

        return DynamicExpression.Make(returnType, delegateType, binder, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" /> and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="returnType">The result type of the dynamic expression.</param>
    /// <param name="arguments">The arguments to the dynamic operation.</param>
    public static DynamicExpression Dynamic(
        CallSiteBinder binder,
        Type returnType,
        IEnumerable<Expression> arguments)
    {
        ContractUtils.RequiresNotNull(arguments, nameof(arguments));
        ContractUtils.RequiresNotNull(returnType, nameof(returnType));
        var readOnlyCollection = arguments.ToReadOnly();
        ContractUtils.RequiresNotEmpty(readOnlyCollection, "args");
        return MakeDynamic(binder, returnType, readOnlyCollection);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.ElementInit" />, given an array of values as the second
    ///     argument.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.ElementInit" /> that has the
    ///     <see cref="P:System.Linq.Expressions.ElementInit.AddMethod" /> and
    ///     <see cref="P:System.Linq.Expressions.ElementInit.Arguments" /> properties set to the specified values.
    /// </returns>
    /// <param name="addMethod">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ElementInit.AddMethod" /> property equal to.
    /// </param>
    /// <param name="arguments">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to set the
    ///     <see cref="P:System.Linq.Expressions.ElementInit.Arguments" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="addMethod" /> or <paramref name="arguments" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The method that addMethod represents is not named "Add" (case
    ///     insensitive).-or-The method that addMethod represents is not an instance method.-or-arguments does not contain the
    ///     same number of elements as the number of parameters for the method that addMethod represents.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of
    ///     <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the method that
    ///     <paramref name="addMethod" /> represents.
    /// </exception>
    public static ElementInit ElementInit(
        MethodInfo addMethod,
        params Expression[] arguments)
    {
        return ElementInit(addMethod, (IEnumerable<Expression>)arguments);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.ElementInit" />, given an
    ///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> as the second argument.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.ElementInit" /> that has the
    ///     <see cref="P:System.Linq.Expressions.ElementInit.AddMethod" /> and
    ///     <see cref="P:System.Linq.Expressions.ElementInit.Arguments" /> properties set to the specified values.
    /// </returns>
    /// <param name="addMethod">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ElementInit.AddMethod" /> property equal to.
    /// </param>
    /// <param name="arguments">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects to set the
    ///     <see cref="P:System.Linq.Expressions.ElementInit.Arguments" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="addMethod" /> or <paramref name="arguments" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The method that <paramref name="addMethod" /> represents is not named
    ///     "Add" (case insensitive).-or-The method that <paramref name="addMethod" /> represents is not an instance
    ///     method.-or-<paramref name="arguments" /> does not contain the same number of elements as the number of parameters
    ///     for the method that <paramref name="addMethod" /> represents.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of
    ///     <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the method that
    ///     <paramref name="addMethod" /> represents.
    /// </exception>
    public static ElementInit ElementInit(
        MethodInfo addMethod,
        IEnumerable<Expression> arguments)
    {
        ContractUtils.RequiresNotNull(addMethod, nameof(addMethod));
        ContractUtils.RequiresNotNull(arguments, nameof(arguments));
        var arguments1 = arguments.ToReadOnly();
        RequiresCanRead(arguments1, nameof(arguments));
        ValidateElementInitAddMethodInfo(addMethod);
        ValidateArgumentTypes(addMethod, ExpressionType.Call, ref arguments1);
        return new ElementInit(addMethod, arguments1);
    }

    /// <summary>Creates an empty expression that has <see cref="T:System.Void" /> type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DefaultExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Default" /> and the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property set to <see cref="T:System.Void" />.
    /// </returns>
    public static DefaultExpression Empty()
    {
        return new DefaultExpression(typeof(void));
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an equality comparison.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Equal" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The equality operator is not defined for <paramref name="left" />
    ///     .Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Equal(Expression left, Expression right)
    {
        return Equal(left, right, false, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an equality comparison.
    ///     The implementing method can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Equal" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="liftToNull">
    ///     true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true;
    ///     false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the equality operator is not defined for <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Equal(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        return method == null
            ? GetEqualityComparisonOperator(ExpressionType.Equal, "op_Equality", left, right, liftToNull)
            : GetMethodBasedBinaryOperator(ExpressionType.Equal, left, right, method, liftToNull);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise XOR operation,
    ///     using op_ExclusiveOr for user-defined types.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ExclusiveOr" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The XOR operator is not defined for <paramref name="left" />.Type
    ///     and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression ExclusiveOr(Expression left, Expression right)
    {
        return ExclusiveOr(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise XOR operation,
    ///     using op_ExclusiveOr for user-defined types. The implementing method can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ExclusiveOr" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the XOR operator is not defined for <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression ExclusiveOr(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.ExclusiveOr, left, right, method, true);
        }

        return left.Type == right.Type && TypeUtils.IsIntegerOrBool(left.Type)
            ? new SimpleBinaryExpression(ExpressionType.ExclusiveOr, left, right, left.Type)
            : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.ExclusiveOr, "op_ExclusiveOr", left, right, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise XOR assignment
    ///     operation, using op_ExclusiveOr for user-defined types.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ExclusiveOrAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression ExclusiveOrAssign(Expression left, Expression right)
    {
        return ExclusiveOrAssign(left, right, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise XOR assignment
    ///     operation, using op_ExclusiveOr for user-defined types.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ExclusiveOrAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression ExclusiveOrAssign(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        return ExclusiveOrAssign(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise XOR assignment
    ///     operation, using op_ExclusiveOr for user-defined types.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ExclusiveOrAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression ExclusiveOrAssign(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.ExclusiveOrAssign, left, right, method, conversion,
                true);
        }

        if (!(left.Type == right.Type) || !TypeUtils.IsIntegerOrBool(left.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.ExclusiveOrAssign, "op_ExclusiveOr", left, right,
                conversion, true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        return new SimpleBinaryExpression(ExpressionType.ExclusiveOrAssign, left, right, left.Type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a field.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" /> and the
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> and
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> properties set to the specified values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property equal to. For static (Shared in
    ///     Visual Basic), <paramref name="expression" /> must be null.
    /// </param>
    /// <param name="field">
    ///     The <see cref="T:System.Reflection.FieldInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="field" /> is null.-or-The field represented by <paramref name="field" /> is not static (Shared in
    ///     Visual Basic) and <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="expression" />.Type is not assignable to the declaring type of the field represented by
    ///     <paramref name="field" />.
    /// </exception>
    public static MemberExpression Field(Expression expression, FieldInfo field)
    {
        ContractUtils.RequiresNotNull(field, nameof(field));
        if (field.IsStatic)
        {
            if (expression != null)
            {
                throw new ArgumentException(Strings.OnlyStaticFieldsHaveNullInstance, nameof(expression));
            }
        }
        else
        {
            if (expression == null)
            {
                throw new ArgumentException(Strings.OnlyStaticFieldsHaveNullInstance, nameof(field));
            }

            RequiresCanRead(expression, nameof(expression));
            if (!TypeUtils.AreReferenceAssignable(field.DeclaringType, expression.Type))
            {
                throw Error.FieldInfoNotDefinedForType(field.DeclaringType, field.Name, expression.Type);
            }
        }

        return MemberExpression.Make(expression, field);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a field given
    ///     the name of the field.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" />, the
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" />
    ///     , and the <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property set to the
    ///     <see cref="T:System.Reflection.FieldInfo" /> that represents the field denoted by <paramref name="fieldName" />.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> whose
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> contains a field named <paramref name="fieldName" />. This
    ///     can be null for static fields.
    /// </param>
    /// <param name="fieldName">The name of a field to be accessed.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> or <paramref name="fieldName" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     No field named <paramref name="fieldName" /> is defined in
    ///     <paramref name="expression" />.Type or its base types.
    /// </exception>
    public static MemberExpression Field(Expression expression, string fieldName)
    {
        RequiresCanRead(expression, nameof(expression));
        var field = expression.Type.GetField(fieldName,
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        if (field == null)
        {
            field = expression.Type.GetField(fieldName,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.FlattenHierarchy);
        }

        return !(field == null)
            ? Field(expression, field)
            : throw Error.InstanceFieldNotDefinedForType(fieldName, expression.Type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a field.</summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.MemberExpression" />.</returns>
    /// <param name="expression">The containing object of the field. This can be null for static fields.</param>
    /// <param name="type">The <see cref="P:System.Linq.Expressions.Expression.Type" /> that contains the field.</param>
    /// <param name="fieldName">The field to be accessed.</param>
    public static MemberExpression Field(Expression expression, Type type, string fieldName)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        var field = type.GetField(fieldName,
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
            BindingFlags.FlattenHierarchy);
        if (field == null)
        {
            field = type.GetField(fieldName,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic |
                BindingFlags.FlattenHierarchy);
        }

        return !(field == null) ? Field(expression, field) : throw Error.FieldNotDefinedForType(fieldName, type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Type" /> object that represents a generic System.Action delegate type that has
    ///     specific type arguments.
    /// </summary>
    /// <returns>The type of a System.Action delegate that has the specified type arguments.</returns>
    /// <param name="typeArgs">
    ///     An array of up to sixteen <see cref="T:System.Type" /> objects that specify the type arguments
    ///     for the System.Action delegate type.
    /// </param>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="typeArgs" /> contains more than sixteen elements.
    /// </exception>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="typeArgs" /> is null.
    /// </exception>
    public static Type GetActionType(params Type[] typeArgs)
    {
        var type = ValidateTryGetFuncActionArgs(typeArgs)
            ? DelegateHelpers.GetActionType(typeArgs)
            : throw Error.TypeMustNotBeByRef();
        return !(type == null) ? type : throw Error.IncorrectNumberOfTypeArgsForAction();
    }

    /// <summary>
    ///     Gets a <see cref="P:System.Linq.Expressions.Expression.Type" /> object that represents a generic System.Func
    ///     or System.Action delegate type that has specific type arguments.
    /// </summary>
    /// <returns>The delegate type.</returns>
    /// <param name="typeArgs">The type arguments of the delegate.</param>
    public static Type GetDelegateType(params Type[] typeArgs)
    {
        ContractUtils.RequiresNotEmpty(typeArgs, nameof(typeArgs));
        ContractUtils.RequiresNotNullItems(typeArgs, nameof(typeArgs));
        return DelegateHelpers.MakeDelegateType(typeArgs);
    }

    /// <summary>
    ///     Creates a <see cref="P:System.Linq.Expressions.Expression.Type" /> object that represents a generic
    ///     System.Func delegate type that has specific type arguments. The last type argument specifies the return type of the
    ///     created delegate.
    /// </summary>
    /// <returns>The type of a System.Func delegate that has the specified type arguments.</returns>
    /// <param name="typeArgs">
    ///     An array of one to seventeen <see cref="T:System.Type" /> objects that specify the type
    ///     arguments for the System.Func delegate type.
    /// </param>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="typeArgs" /> contains fewer than one or more than seventeen elements.
    /// </exception>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="typeArgs" /> is null.
    /// </exception>
    public static Type GetFuncType(params Type[] typeArgs)
    {
        var type = ValidateTryGetFuncActionArgs(typeArgs)
            ? DelegateHelpers.GetFuncType(typeArgs)
            : throw Error.TypeMustNotBeByRef();
        return !(type == null) ? type : throw Error.IncorrectNumberOfTypeArgsForFunc();
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a "go to" statement.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Goto, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to the specified value, and a null
    ///     value to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    public static GotoExpression Goto(LabelTarget target)
    {
        return MakeGoto(GotoExpressionKind.Goto, target, null, typeof(void));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a "go to" statement with the
    ///     specified type.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Goto, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to the specified value, the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and a null value
    ///     to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    /// <param name="type">
    ///     An <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    public static GotoExpression Goto(LabelTarget target, Type type)
    {
        return MakeGoto(GotoExpressionKind.Goto, target, null, type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a "go to" statement. The value
    ///     passed to the label upon jumping can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Goto, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and
    ///     <paramref name="value" /> to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
    public static GotoExpression Goto(LabelTarget target, Expression value)
    {
        return MakeGoto(GotoExpressionKind.Goto, target, value, typeof(void));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a "go to" statement with the
    ///     specified type. The value passed to the label upon jumping can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Goto, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and
    ///     <paramref name="value" /> to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
    /// <param name="type">
    ///     An <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    public static GotoExpression Goto(LabelTarget target, Expression value, Type type)
    {
        return MakeGoto(GotoExpressionKind.Goto, target, value, type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "greater than" numeric
    ///     comparison.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.GreaterThan" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The "greater than" operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression GreaterThan(Expression left, Expression right)
    {
        return GreaterThan(left, right, false, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "greater than" numeric
    ///     comparison. The implementing method can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.GreaterThan" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="liftToNull">
    ///     true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true;
    ///     false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the "greater than" operator is not defined for <paramref name="left" />.Type
    ///     and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression GreaterThan(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        return method == null
            ? GetComparisonOperator(ExpressionType.GreaterThan, "op_GreaterThan", left, right, liftToNull)
            : GetMethodBasedBinaryOperator(ExpressionType.GreaterThan, left, right, method, liftToNull);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "greater than or equal"
    ///     numeric comparison.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.GreaterThanOrEqual" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The "greater than or equal" operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression GreaterThanOrEqual(Expression left, Expression right)
    {
        return GreaterThanOrEqual(left, right, false, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "greater than or equal"
    ///     numeric comparison.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.GreaterThanOrEqual" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="liftToNull">
    ///     true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true;
    ///     false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the "greater than or equal" operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression GreaterThanOrEqual(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        return method == null
            ? GetComparisonOperator(ExpressionType.GreaterThanOrEqual, "op_GreaterThanOrEqual", left, right, liftToNull)
            : GetMethodBasedBinaryOperator(ExpressionType.GreaterThanOrEqual, left, right, method, liftToNull);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ConditionalExpression" /> that represents a conditional block
    ///     with an if statement.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ConditionalExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Conditional" /> and the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.Test" />,
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfTrue" />, properties set to the specified values. The
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfFalse" /> property is set to default expression and
    ///     the type of the resulting <see cref="T:System.Linq.Expressions.ConditionalExpression" /> returned by this method is
    ///     <see cref="T:System.Void" />.
    /// </returns>
    /// <param name="test">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.Test" /> property equal to.
    /// </param>
    /// <param name="ifTrue">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfTrue" /> property equal to.
    /// </param>
    public static ConditionalExpression IfThen(Expression test, Expression ifTrue)
    {
        return Condition(test, ifTrue, Empty(), typeof(void));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ConditionalExpression" /> that represents a conditional block
    ///     with if and else statements.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ConditionalExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Conditional" /> and the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.Test" />,
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfTrue" />, and
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfFalse" /> properties set to the specified values. The
    ///     type of the resulting <see cref="T:System.Linq.Expressions.ConditionalExpression" /> returned by this method is
    ///     <see cref="T:System.Void" />.
    /// </returns>
    /// <param name="test">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.Test" /> property equal to.
    /// </param>
    /// <param name="ifTrue">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfTrue" /> property equal to.
    /// </param>
    /// <param name="ifFalse">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ConditionalExpression.IfFalse" /> property equal to.
    /// </param>
    public static ConditionalExpression IfThenElse(
        Expression test,
        Expression ifTrue,
        Expression ifFalse)
    {
        return Condition(test, ifTrue, ifFalse, typeof(void));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the incrementing of the
    ///     expression value by 1.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the incremented expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to increment.</param>
    public static UnaryExpression Increment(Expression expression)
    {
        return Increment(expression, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the incrementing of the
    ///     expression by 1.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the incremented expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to increment.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</param>
    public static UnaryExpression Increment(Expression expression, MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        if (!(method == null))
        {
            return GetMethodBasedUnaryOperator(ExpressionType.Increment, expression, method);
        }

        return TypeUtils.IsArithmetic(expression.Type)
            ? new UnaryExpression(ExpressionType.Increment, expression, expression.Type, null)
            : GetUserDefinedUnaryOperatorOrThrow(ExpressionType.Increment, "op_Increment", expression);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.InvocationExpression" /> that applies a delegate or lambda
    ///     expression to a list of argument expressions.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.InvocationExpression" /> that applies the specified delegate or lambda
    ///     expression to the provided arguments.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> that represents the delegate or lambda
    ///     expression to be applied.
    /// </param>
    /// <param name="arguments">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects that represent the
    ///     arguments that the delegate or lambda expression is applied to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="expression" />.Type does not represent a delegate type or an
    ///     <see cref="T:System.Linq.Expressions.Expression`1" />.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is
    ///     not assignable to the type of the corresponding parameter of the delegate represented by
    ///     <paramref name="expression" />.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="arguments" /> does not contain the same number of elements as the list of parameters for the
    ///     delegate represented by <paramref name="expression" />.
    /// </exception>
    public static InvocationExpression Invoke(Expression expression, params Expression[] arguments)
    {
        return Invoke(expression, (IEnumerable<Expression>)arguments);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.InvocationExpression" /> that applies a delegate or lambda
    ///     expression to a list of argument expressions.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.InvocationExpression" /> that applies the specified delegate or lambda
    ///     expression to the provided arguments.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> that represents the delegate or lambda
    ///     expression to be applied to.
    /// </param>
    /// <param name="arguments">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects that represent the arguments that the delegate or
    ///     lambda expression is applied to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="expression" />.Type does not represent a delegate type or an
    ///     <see cref="T:System.Linq.Expressions.Expression`1" />.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is
    ///     not assignable to the type of the corresponding parameter of the delegate represented by
    ///     <paramref name="expression" />.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="arguments" /> does not contain the same number of elements as the list of parameters for the
    ///     delegate represented by <paramref name="expression" />.
    /// </exception>
    public static InvocationExpression Invoke(
        Expression expression,
        IEnumerable<Expression> arguments)
    {
        RequiresCanRead(expression, nameof(expression));
        var arguments1 = arguments.ToReadOnly();
        var invokeMethod = GetInvokeMethod(expression);
        ValidateArgumentTypes(invokeMethod, ExpressionType.Invoke, ref arguments1);
        return new InvocationExpression(expression, arguments1, invokeMethod.ReturnType);
    }

    /// <summary>Returns whether the expression evaluates to false.</summary>
    /// <returns>An instance of <see cref="T:System.Linq.Expressions.UnaryExpression" />.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to evaluate.</param>
    public static UnaryExpression IsFalse(Expression expression)
    {
        return IsFalse(expression, null);
    }

    /// <summary>Returns whether the expression evaluates to false.</summary>
    /// <returns>An instance of <see cref="T:System.Linq.Expressions.UnaryExpression" />.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to evaluate.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</param>
    public static UnaryExpression IsFalse(Expression expression, MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        if (!(method == null))
        {
            return GetMethodBasedUnaryOperator(ExpressionType.IsFalse, expression, method);
        }

        return TypeUtils.IsBool(expression.Type)
            ? new UnaryExpression(ExpressionType.IsFalse, expression, expression.Type, null)
            : GetUserDefinedUnaryOperatorOrThrow(ExpressionType.IsFalse, "op_False", expression);
    }

    /// <summary>Returns whether the expression evaluates to true.</summary>
    /// <returns>An instance of <see cref="T:System.Linq.Expressions.UnaryExpression" />.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to evaluate.</param>
    public static UnaryExpression IsTrue(Expression expression)
    {
        return IsTrue(expression, null);
    }

    /// <summary>Returns whether the expression evaluates to true.</summary>
    /// <returns>An instance of <see cref="T:System.Linq.Expressions.UnaryExpression" />.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to evaluate.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</param>
    public static UnaryExpression IsTrue(Expression expression, MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        if (!(method == null))
        {
            return GetMethodBasedUnaryOperator(ExpressionType.IsTrue, expression, method);
        }

        return TypeUtils.IsBool(expression.Type)
            ? new UnaryExpression(ExpressionType.IsTrue, expression, expression.Type, null)
            : GetUserDefinedUnaryOperatorOrThrow(ExpressionType.IsTrue, "op_True", expression);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.LabelExpression" /> representing a label without a default
    ///     value.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.LabelExpression" /> without a default value.</returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> which this
    ///     <see cref="T:System.Linq.Expressions.LabelExpression" /> will be associated with.
    /// </param>
    public static LabelExpression Label(LabelTarget target)
    {
        return Label(target, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.LabelExpression" /> representing a label with the given default
    ///     value.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.LabelExpression" /> with the given default value.</returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> which this
    ///     <see cref="T:System.Linq.Expressions.LabelExpression" /> will be associated with.
    /// </param>
    /// <param name="defaultValue">
    ///     The value of this <see cref="T:System.Linq.Expressions.LabelExpression" /> when the label is
    ///     reached through regular control flow.
    /// </param>
    public static LabelExpression Label(LabelTarget target, Expression defaultValue)
    {
        ValidateGoto(target, ref defaultValue, "label", nameof(defaultValue));
        return new LabelExpression(target, defaultValue);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.LabelTarget" /> representing a label with void type and no
    ///     name.
    /// </summary>
    /// <returns>The new <see cref="T:System.Linq.Expressions.LabelTarget" />.</returns>
    public static LabelTarget Label()
    {
        return Label(typeof(void), null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.LabelTarget" /> representing a label with void type and the
    ///     given name.
    /// </summary>
    /// <returns>The new <see cref="T:System.Linq.Expressions.LabelTarget" />.</returns>
    /// <param name="name">The name of the label.</param>
    public static LabelTarget Label(string name)
    {
        return Label(typeof(void), name);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.LabelTarget" /> representing a label with the given type.</summary>
    /// <returns>The new <see cref="T:System.Linq.Expressions.LabelTarget" />.</returns>
    /// <param name="type">The type of value that is passed when jumping to the label.</param>
    public static LabelTarget Label(Type type)
    {
        return Label(type, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.LabelTarget" /> representing a label with the given type and
    ///     name.
    /// </summary>
    /// <returns>The new <see cref="T:System.Linq.Expressions.LabelTarget" />.</returns>
    /// <param name="type">The type of value that is passed when jumping to the label.</param>
    /// <param name="name">The name of the label.</param>
    public static LabelTarget Label(Type type, string name)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        TypeUtils.ValidateType(type);
        return new LabelTarget(type, name);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.Expression`1" /> where the delegate type is known at compile
    ///     time.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.Expression`1" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Lambda" /> and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="parameters">
    ///     An array of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to
    ///     populate the <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    /// <typeparam name="TDelegate">A delegate type.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="body" /> is null.-or-One or more elements in <paramref name="parameters" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="TDelegate" /> is not a delegate type.-or-<paramref name="body" />.Type represents a type that is
    ///     not assignable to the return type of <paramref name="TDelegate" />.-or-<paramref name="parameters" /> does not
    ///     contain the same number of elements as the list of parameters for <paramref name="TDelegate" />.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" />
    ///     is not assignable from the type of the corresponding parameter type of <paramref name="TDelegate" />.
    /// </exception>
    public static Expression<TDelegate> Lambda<TDelegate>(
        Expression body,
        params ParameterExpression[] parameters)
    {
        return Lambda<TDelegate>(body, false, (IEnumerable<ParameterExpression>)parameters);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.Expression`1" /> where the delegate type is known at compile
    ///     time.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.Expression`1" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.NodeType" /> property equal to
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Lambda" /> and the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Body" /> property equal to.
    /// </param>
    /// <param name="tailCall">
    ///     A <see cref="T:System.Boolean" /> that indicates if tail call optimization will be applied when
    ///     compiling the created expression.
    /// </param>
    /// <param name="parameters">
    ///     An array that contains <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to
    ///     use to populate the <see cref="P:System.Linq.Expressions.Expression`1.Parameters" /> collection.
    /// </param>
    /// <typeparam name="TDelegate">The delegate type. </typeparam>
    public static Expression<TDelegate> Lambda<TDelegate>(
        Expression body,
        bool tailCall,
        params ParameterExpression[] parameters)
    {
        return Lambda<TDelegate>(body, tailCall, (IEnumerable<ParameterExpression>)parameters);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.Expression`1" /> where the delegate type is known at compile
    ///     time.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.Expression`1" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Lambda" /> and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    /// <typeparam name="TDelegate">A delegate type.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="body" /> is null.-or-One or more elements in <paramref name="parameters" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="TDelegate" /> is not a delegate type.-or-<paramref name="body" />.Type represents a type that is
    ///     not assignable to the return type of <paramref name="TDelegate" />.-or-<paramref name="parameters" /> does not
    ///     contain the same number of elements as the list of parameters for <paramref name="TDelegate" />.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" />
    ///     is not assignable from the type of the corresponding parameter type of <paramref name="TDelegate" />.
    /// </exception>
    public static Expression<TDelegate> Lambda<TDelegate>(
        Expression body,
        IEnumerable<ParameterExpression> parameters)
    {
        return Lambda<TDelegate>(body, null, false, parameters);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.Expression`1" /> where the delegate type is known at compile
    ///     time.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.Expression`1" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.NodeType" /> property equal to
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Lambda" /> and the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Body" /> property equal to.
    /// </param>
    /// <param name="tailCall">
    ///     A <see cref="T:System.Boolean" /> that indicates if tail call optimization will be applied when
    ///     compiling the created expression.
    /// </param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Parameters" /> collection.
    /// </param>
    /// <typeparam name="TDelegate">The delegate type. </typeparam>
    public static Expression<TDelegate> Lambda<TDelegate>(
        Expression body,
        bool tailCall,
        IEnumerable<ParameterExpression> parameters)
    {
        return Lambda<TDelegate>(body, null, tailCall, parameters);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.Expression`1" /> where the delegate type is known at compile
    ///     time.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.Expression`1" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.NodeType" /> property equal to
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Lambda" /> and the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Body" /> property equal to.
    /// </param>
    /// <param name="name">The name of the lambda. Used for generating debugging information.</param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Parameters" /> collection.
    /// </param>
    /// <typeparam name="TDelegate">The delegate type. </typeparam>
    public static Expression<TDelegate> Lambda<TDelegate>(
        Expression body,
        string name,
        IEnumerable<ParameterExpression> parameters)
    {
        return Lambda<TDelegate>(body, name, false, parameters);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.Expression`1" /> where the delegate type is known at compile
    ///     time.
    /// </summary>
    /// <returns>
    ///     An <see cref="T:System.Linq.Expressions.Expression`1" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.NodeType" /> property equal to
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Lambda" /> and the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Body" /> property equal to.
    /// </param>
    /// <param name="name">The name of the lambda. Used for generating debugging info.</param>
    /// <param name="tailCall">
    ///     A <see cref="T:System.Boolean" /> that indicates if tail call optimization will be applied when
    ///     compiling the created expression.
    /// </param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.Expression`1.Parameters" /> collection.
    /// </param>
    /// <typeparam name="TDelegate">The delegate type. </typeparam>
    public static Expression<TDelegate> Lambda<TDelegate>(
        Expression body,
        string name,
        bool tailCall,
        IEnumerable<ParameterExpression> parameters)
    {
        var parameters1 = parameters.ToReadOnly();
        ValidateLambdaArgs(typeof(TDelegate), ref body, parameters1);
        return new Expression<TDelegate>(body, name, tailCall, parameters1);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.LambdaExpression" /> by first constructing a delegate type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Lambda" /> and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="parameters">
    ///     An array of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to
    ///     populate the <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="body" /> is null.-or-One or more elements of <paramref name="parameters" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="parameters" /> contains more than sixteen elements.
    /// </exception>
    public static LambdaExpression Lambda(Expression body, params ParameterExpression[] parameters)
    {
        return Lambda(body, false, (IEnumerable<ParameterExpression>)parameters);
    }

    /// <summary>Creates a LambdaExpression by first constructing a delegate type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="tailCall">
    ///     A <see cref="T:System.Boolean" /> that indicates if tail call optimization will be applied when
    ///     compiling the created expression.
    /// </param>
    /// <param name="parameters">
    ///     An array that contains <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to
    ///     use to populate the <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    public static LambdaExpression Lambda(
        Expression body,
        bool tailCall,
        params ParameterExpression[] parameters)
    {
        return Lambda(body, tailCall, (IEnumerable<ParameterExpression>)parameters);
    }

    /// <summary>Creates a LambdaExpression by first constructing a delegate type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    public static LambdaExpression Lambda(
        Expression body,
        IEnumerable<ParameterExpression> parameters)
    {
        return Lambda(body, null, false, parameters);
    }

    /// <summary>Creates a LambdaExpression by first constructing a delegate type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="tailCall">
    ///     A <see cref="T:System.Boolean" /> that indicates if tail call optimization will be applied when
    ///     compiling the created expression.
    /// </param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    public static LambdaExpression Lambda(
        Expression body,
        bool tailCall,
        IEnumerable<ParameterExpression> parameters)
    {
        return Lambda(body, null, tailCall, parameters);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.LambdaExpression" /> by first constructing a delegate type. It
    ///     can be used when the delegate type is not known at compile time.
    /// </summary>
    /// <returns>
    ///     An object that represents a lambda expression which has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Lambda" /> and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="delegateType">A <see cref="T:System.Type" /> that represents a delegate signature for the lambda.</param>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="parameters">
    ///     An array of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to
    ///     populate the <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="delegateType" /> or <paramref name="body" /> is null.-or-One or more elements in
    ///     <paramref name="parameters" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="delegateType" /> does not represent a delegate type.-or-<paramref name="body" />.Type represents a
    ///     type that is not assignable to the return type of the delegate type represented by <paramref name="delegateType" />
    ///     .-or-<paramref name="parameters" /> does not contain the same number of elements as the list of parameters for the
    ///     delegate type represented by <paramref name="delegateType" />.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" />
    ///     is not assignable from the type of the corresponding parameter type of the delegate type represented by
    ///     <paramref name="delegateType" />.
    /// </exception>
    public static LambdaExpression Lambda(
        Type delegateType,
        Expression body,
        params ParameterExpression[] parameters)
    {
        return Lambda(delegateType, body, null, false, parameters);
    }

    /// <summary>Creates a LambdaExpression by first constructing a delegate type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="delegateType">
    ///     A <see cref="P:System.Linq.Expressions.Expression.Type" /> representing the delegate
    ///     signature for the lambda.
    /// </param>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="tailCall">
    ///     A <see cref="T:System.Boolean" /> that indicates if tail call optimization will be applied when
    ///     compiling the created expression.
    /// </param>
    /// <param name="parameters">
    ///     An array that contains <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to
    ///     use to populate the <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    public static LambdaExpression Lambda(
        Type delegateType,
        Expression body,
        bool tailCall,
        params ParameterExpression[] parameters)
    {
        return Lambda(delegateType, body, null, tailCall, parameters);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.LambdaExpression" /> by first constructing a delegate type. It
    ///     can be used when the delegate type is not known at compile time.
    /// </summary>
    /// <returns>
    ///     An object that represents a lambda expression which has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Lambda" /> and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="delegateType">A <see cref="T:System.Type" /> that represents a delegate signature for the lambda.</param>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="delegateType" /> or <paramref name="body" /> is null.-or-One or more elements in
    ///     <paramref name="parameters" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="delegateType" /> does not represent a delegate type.-or-<paramref name="body" />.Type represents a
    ///     type that is not assignable to the return type of the delegate type represented by <paramref name="delegateType" />
    ///     .-or-<paramref name="parameters" /> does not contain the same number of elements as the list of parameters for the
    ///     delegate type represented by <paramref name="delegateType" />.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="parameters" />
    ///     is not assignable from the type of the corresponding parameter type of the delegate type represented by
    ///     <paramref name="delegateType" />.
    /// </exception>
    public static LambdaExpression Lambda(
        Type delegateType,
        Expression body,
        IEnumerable<ParameterExpression> parameters)
    {
        return Lambda(delegateType, body, null, false, parameters);
    }

    /// <summary>Creates a LambdaExpression by first constructing a delegate type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="delegateType">
    ///     A <see cref="P:System.Linq.Expressions.Expression.Type" /> representing the delegate
    ///     signature for the lambda.
    /// </param>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="tailCall">
    ///     A <see cref="T:System.Boolean" /> that indicates if tail call optimization will be applied when
    ///     compiling the created expression.
    /// </param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    public static LambdaExpression Lambda(
        Type delegateType,
        Expression body,
        bool tailCall,
        IEnumerable<ParameterExpression> parameters)
    {
        return Lambda(delegateType, body, null, tailCall, parameters);
    }

    /// <summary>Creates a LambdaExpression by first constructing a delegate type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    public static LambdaExpression Lambda(
        Expression body,
        string name,
        IEnumerable<ParameterExpression> parameters)
    {
        return Lambda(body, name, false, parameters);
    }

    /// <summary>Creates a LambdaExpression by first constructing a delegate type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
    /// <param name="tailCall">
    ///     A <see cref="T:System.Boolean" /> that indicates if tail call optimization will be applied when
    ///     compiling the created expression.
    /// </param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    public static LambdaExpression Lambda(
        Expression body,
        string name,
        bool tailCall,
        IEnumerable<ParameterExpression> parameters)
    {
        ContractUtils.RequiresNotNull(body, nameof(body));
        var parameters1 = parameters.ToReadOnly();
        var count = parameters1.Count;
        var types = new Type[count + 1];
        if (count > 0)
        {
            var set = new Set<ParameterExpression>(parameters1.Count);
            for (var index = 0; index < count; ++index)
            {
                var p0 = parameters1[index];
                ContractUtils.RequiresNotNull(p0, "parameter");
                types[index] = p0.IsByRef ? p0.Type.MakeByRefType() : p0.Type;
                if (set.Contains(p0))
                {
                    throw Error.DuplicateVariable(p0);
                }

                set.Add(p0);
            }
        }

        types[count] = body.Type;
        return CreateLambda(DelegateHelpers.MakeDelegateType(types), body, name, tailCall, parameters1);
    }

    /// <summary>Creates a LambdaExpression by first constructing a delegate type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="delegateType">
    ///     A <see cref="P:System.Linq.Expressions.Expression.Type" /> representing the delegate
    ///     signature for the lambda.
    /// </param>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    public static LambdaExpression Lambda(
        Type delegateType,
        Expression body,
        string name,
        IEnumerable<ParameterExpression> parameters)
    {
        var parameters1 = parameters.ToReadOnly();
        ValidateLambdaArgs(delegateType, ref body, parameters1);
        return CreateLambda(delegateType, body, name, false, parameters1);
    }

    /// <summary>Creates a LambdaExpression by first constructing a delegate type.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.NodeType" /> property equal to Lambda and the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> and
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> properties set to the specified values.
    /// </returns>
    /// <param name="delegateType">
    ///     A <see cref="P:System.Linq.Expressions.Expression.Type" /> representing the delegate
    ///     signature for the lambda.
    /// </param>
    /// <param name="body">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property equal to.
    /// </param>
    /// <param name="name">The name for the lambda. Used for emitting debug information.</param>
    /// <param name="tailCall">
    ///     A <see cref="T:System.Boolean" /> that indicates if tail call optimization will be applied when
    ///     compiling the created expression.
    /// </param>
    /// <param name="parameters">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> collection.
    /// </param>
    public static LambdaExpression Lambda(
        Type delegateType,
        Expression body,
        string name,
        bool tailCall,
        IEnumerable<ParameterExpression> parameters)
    {
        var parameters1 = parameters.ToReadOnly();
        ValidateLambdaArgs(delegateType, ref body, parameters1);
        return CreateLambda(delegateType, body, name, tailCall, parameters1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.LeftShift" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The left-shift operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression LeftShift(Expression left, Expression right)
    {
        return LeftShift(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.LeftShift" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the left-shift operator is not defined for <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression LeftShift(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.LeftShift, left, right, method, true);
        }

        if (!IsSimpleShift(left.Type, right.Type))
        {
            return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.LeftShift, "op_LeftShift", left, right, true);
        }

        var resultTypeOfShift = GetResultTypeOfShift(left.Type, right.Type);
        return new SimpleBinaryExpression(ExpressionType.LeftShift, left, right, resultTypeOfShift);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift
    ///     assignment operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.LeftShiftAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression LeftShiftAssign(Expression left, Expression right)
    {
        return LeftShiftAssign(left, right, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift
    ///     assignment operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.LeftShiftAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression LeftShiftAssign(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        return LeftShiftAssign(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise left-shift
    ///     assignment operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.LeftShiftAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression LeftShiftAssign(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.LeftShiftAssign, left, right, method, conversion, true);
        }

        if (!IsSimpleShift(left.Type, right.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.LeftShiftAssign, "op_LeftShift", left, right,
                conversion, true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        var resultTypeOfShift = GetResultTypeOfShift(left.Type, right.Type);
        return new SimpleBinaryExpression(ExpressionType.LeftShiftAssign, left, right, resultTypeOfShift);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "less than" numeric
    ///     comparison.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.LessThan" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The "less than" operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression LessThan(Expression left, Expression right)
    {
        return LessThan(left, right, false, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "less than" numeric
    ///     comparison.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.LessThan" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="liftToNull">
    ///     true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true;
    ///     false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the "less than" operator is not defined for <paramref name="left" />.Type
    ///     and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression LessThan(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        return method == null
            ? GetComparisonOperator(ExpressionType.LessThan, "op_LessThan", left, right, liftToNull)
            : GetMethodBasedBinaryOperator(ExpressionType.LessThan, left, right, method, liftToNull);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a " less than or equal"
    ///     numeric comparison.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.LessThanOrEqual" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The "less than or equal" operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression LessThanOrEqual(Expression left, Expression right)
    {
        return LessThanOrEqual(left, right, false, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a "less than or equal"
    ///     numeric comparison.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.LessThanOrEqual" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="liftToNull">
    ///     true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true;
    ///     false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the "less than or equal" operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression LessThanOrEqual(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        return method == null
            ? GetComparisonOperator(ExpressionType.LessThanOrEqual, "op_LessThanOrEqual", left, right, liftToNull)
            : GetMethodBasedBinaryOperator(ExpressionType.LessThanOrEqual, left, right, method, liftToNull);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> where the member is a field or property.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" /> and the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and
    ///     <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> properties set to the specified values.
    /// </returns>
    /// <param name="member">
    ///     A <see cref="T:System.Reflection.MemberInfo" /> that represents a field or property to set the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.
    /// </param>
    /// <param name="initializers">
    ///     An array of <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate
    ///     the <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="member" /> is null. -or-One or more elements of <paramref name="initializers" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="member" /> does not represent a field or property.-or-The
    ///     <see cref="P:System.Reflection.FieldInfo.FieldType" /> or
    ///     <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the field or property that
    ///     <paramref name="member" /> represents does not implement <see cref="T:System.Collections.IEnumerable" />.
    /// </exception>
    public static MemberListBinding ListBind(MemberInfo member, params ElementInit[] initializers)
    {
        ContractUtils.RequiresNotNull(member, nameof(member));
        ContractUtils.RequiresNotNull(initializers, nameof(initializers));
        return ListBind(member, (IEnumerable<ElementInit>)initializers);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> where the member is a field or property.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" /> and the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and
    ///     <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> properties set to the specified values.
    /// </returns>
    /// <param name="member">
    ///     A <see cref="T:System.Reflection.MemberInfo" /> that represents a field or property to set the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.
    /// </param>
    /// <param name="initializers">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="member" /> is null. -or-One or more elements of <paramref name="initializers" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="member" /> does not represent a field or property.-or-The
    ///     <see cref="P:System.Reflection.FieldInfo.FieldType" /> or
    ///     <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the field or property that
    ///     <paramref name="member" /> represents does not implement <see cref="T:System.Collections.IEnumerable" />.
    /// </exception>
    public static MemberListBinding ListBind(MemberInfo member, IEnumerable<ElementInit> initializers)
    {
        ContractUtils.RequiresNotNull(member, nameof(member));
        ContractUtils.RequiresNotNull(initializers, nameof(initializers));
        Type memberType;
        ValidateGettableFieldOrPropertyMember(member, out memberType);
        var initializers1 = initializers.ToReadOnly();
        ValidateListInitArgs(memberType, initializers1);
        return new MemberListBinding(member, initializers1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> object based on a specified property
    ///     accessor method.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" />, the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the
    ///     <see cref="T:System.Reflection.MemberInfo" /> that represents the property accessed in
    ///     <paramref name="propertyAccessor" />, and <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" />
    ///     populated with the elements of <paramref name="initializers" />.
    /// </returns>
    /// <param name="propertyAccessor">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor
    ///     method.
    /// </param>
    /// <param name="initializers">
    ///     An array of <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate
    ///     the <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="propertyAccessor" /> is null. -or-One or more elements of <paramref name="initializers" /> are
    ///     null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The
    ///     <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the property that the method represented by
    ///     <paramref name="propertyAccessor" /> accesses does not implement <see cref="T:System.Collections.IEnumerable" />.
    /// </exception>
    public static MemberListBinding ListBind(
        MethodInfo propertyAccessor,
        params ElementInit[] initializers)
    {
        ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
        ContractUtils.RequiresNotNull(initializers, nameof(initializers));
        return ListBind(propertyAccessor, (IEnumerable<ElementInit>)initializers);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberListBinding" /> based on a specified property accessor
    ///     method.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberListBinding" /> that has the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.MemberBindingType.ListBinding" />, the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the
    ///     <see cref="T:System.Reflection.MemberInfo" /> that represents the property accessed in
    ///     <paramref name="propertyAccessor" />, and <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" />
    ///     populated with the elements of <paramref name="initializers" />.
    /// </returns>
    /// <param name="propertyAccessor">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor
    ///     method.
    /// </param>
    /// <param name="initializers">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.MemberListBinding.Initializers" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="propertyAccessor" /> is null. -or-One or more elements of <paramref name="initializers" /> are
    ///     null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The
    ///     <see cref="P:System.Reflection.PropertyInfo.PropertyType" /> of the property that the method represented by
    ///     <paramref name="propertyAccessor" /> accesses does not implement <see cref="T:System.Collections.IEnumerable" />.
    /// </exception>
    public static MemberListBinding ListBind(
        MethodInfo propertyAccessor,
        IEnumerable<ElementInit> initializers)
    {
        ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
        ContractUtils.RequiresNotNull(initializers, nameof(initializers));
        return ListBind(GetProperty(propertyAccessor), initializers);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses a method named "Add" to add
    ///     elements to a collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.
    /// </returns>
    /// <param name="newExpression">
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.
    /// </param>
    /// <param name="initializers">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate
    ///     the <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of
    ///     <paramref name="initializers" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     There is no instance method named "Add" (case insensitive)
    ///     declared in <paramref name="newExpression" />.Type or its base type.-or-The add method on
    ///     <paramref name="newExpression" />.Type or its base type does not take exactly one argument.-or-The type represented
    ///     by the <see cref="P:System.Linq.Expressions.Expression.Type" /> property of the first element of
    ///     <paramref name="initializers" /> is not assignable to the argument type of the add method on
    ///     <paramref name="newExpression" />.Type or its base type.-or-More than one argument-compatible method named "Add"
    ///     (case-insensitive) exists on <paramref name="newExpression" />.Type and/or its base type.
    /// </exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        params Expression[] initializers)
    {
        ContractUtils.RequiresNotNull(newExpression, nameof(newExpression));
        ContractUtils.RequiresNotNull(initializers, nameof(initializers));
        return ListInit(newExpression, (IEnumerable<Expression>)initializers);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses a method named "Add" to add
    ///     elements to a collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.
    /// </returns>
    /// <param name="newExpression">
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.
    /// </param>
    /// <param name="initializers">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of
    ///     <paramref name="initializers" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     There is no instance method named "Add" (case insensitive)
    ///     declared in <paramref name="newExpression" />.Type or its base type.-or-The add method on
    ///     <paramref name="newExpression" />.Type or its base type does not take exactly one argument.-or-The type represented
    ///     by the <see cref="P:System.Linq.Expressions.Expression.Type" /> property of the first element of
    ///     <paramref name="initializers" /> is not assignable to the argument type of the add method on
    ///     <paramref name="newExpression" />.Type or its base type.-or-More than one argument-compatible method named "Add"
    ///     (case-insensitive) exists on <paramref name="newExpression" />.Type and/or its base type.
    /// </exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        IEnumerable<Expression> initializers)
    {
        ContractUtils.RequiresNotNull(newExpression, nameof(newExpression));
        ContractUtils.RequiresNotNull(initializers, nameof(initializers));
        var readOnlyCollection = initializers.ToReadOnly();
        if (readOnlyCollection.Count == 0)
        {
            throw Error.ListInitializerWithZeroMembers();
        }

        var method = FindMethod(newExpression.Type, "Add", null, new Expression[1]
        {
            readOnlyCollection[0]
        }, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return ListInit(newExpression, method, initializers);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses a specified method to add
    ///     elements to a collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.
    /// </returns>
    /// <param name="newExpression">
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.
    /// </param>
    /// <param name="addMethod">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> that represents an instance method that takes
    ///     one argument, that adds an element to a collection.
    /// </param>
    /// <param name="initializers">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate
    ///     the <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of
    ///     <paramref name="initializers" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.-or-
    ///     <paramref name="addMethod" /> is not null and it does not represent an instance method named "Add" (case
    ///     insensitive) that takes exactly one argument.-or-<paramref name="addMethod" /> is not null and the type represented
    ///     by the <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of
    ///     <paramref name="initializers" /> is not assignable to the argument type of the method that
    ///     <paramref name="addMethod" /> represents.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="addMethod" /> is null and no instance method named "Add" that takes one type-compatible argument
    ///     exists on <paramref name="newExpression" />.Type or its base type.
    /// </exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        MethodInfo addMethod,
        params Expression[] initializers)
    {
        if (addMethod == null)
        {
            return ListInit(newExpression, (IEnumerable<Expression>)initializers);
        }

        ContractUtils.RequiresNotNull(newExpression, nameof(newExpression));
        ContractUtils.RequiresNotNull(initializers, nameof(initializers));
        return ListInit(newExpression, addMethod, (IEnumerable<Expression>)initializers);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses a specified method to add
    ///     elements to a collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property set to the specified value.
    /// </returns>
    /// <param name="newExpression">
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.
    /// </param>
    /// <param name="addMethod">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> that represents an instance method named "Add"
    ///     (case insensitive), that adds an element to a collection.
    /// </param>
    /// <param name="initializers">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of
    ///     <paramref name="initializers" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.-or-
    ///     <paramref name="addMethod" /> is not null and it does not represent an instance method named "Add" (case
    ///     insensitive) that takes exactly one argument.-or-<paramref name="addMethod" /> is not null and the type represented
    ///     by the <see cref="P:System.Linq.Expressions.Expression.Type" /> property of one or more elements of
    ///     <paramref name="initializers" /> is not assignable to the argument type of the method that
    ///     <paramref name="addMethod" /> represents.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="addMethod" /> is null and no instance method named "Add" that takes one type-compatible argument
    ///     exists on <paramref name="newExpression" />.Type or its base type.
    /// </exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        MethodInfo addMethod,
        IEnumerable<Expression> initializers)
    {
        if (addMethod == null)
        {
            return ListInit(newExpression, initializers);
        }

        ContractUtils.RequiresNotNull(newExpression, nameof(newExpression));
        ContractUtils.RequiresNotNull(initializers, nameof(initializers));
        var readOnlyCollection = initializers.ToReadOnly();
        var list = readOnlyCollection.Count != 0
            ? new ElementInit[readOnlyCollection.Count]
            : throw Error.ListInitializerWithZeroMembers();
        for (var index = 0; index < readOnlyCollection.Count; ++index)
        {
            list[index] = ElementInit(addMethod, readOnlyCollection[index]);
        }

        return ListInit(newExpression, new TrueReadOnlyCollection<ElementInit>(list));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses specified
    ///     <see cref="T:System.Linq.Expressions.ElementInit" /> objects to initialize a collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> and
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> properties set to the specified values.
    /// </returns>
    /// <param name="newExpression">
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.
    /// </param>
    /// <param name="initializers">
    ///     An array of <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate
    ///     the <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of
    ///     <paramref name="initializers" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.
    /// </exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        params ElementInit[] initializers)
    {
        return ListInit(newExpression, (IEnumerable<ElementInit>)initializers);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ListInitExpression" /> that uses specified
    ///     <see cref="T:System.Linq.Expressions.ElementInit" /> objects to initialize a collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ListInitExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ListInit" /> and the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> and
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> properties set to the specified values.
    /// </returns>
    /// <param name="newExpression">
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.NewExpression" /> property equal to.
    /// </param>
    /// <param name="initializers">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.ElementInit" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.ListInitExpression.Initializers" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="newExpression" /> or <paramref name="initializers" /> is null.-or-One or more elements of
    ///     <paramref name="initializers" /> are null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="newExpression" />.Type does not implement <see cref="T:System.Collections.IEnumerable" />.
    /// </exception>
    public static ListInitExpression ListInit(
        NewExpression newExpression,
        IEnumerable<ElementInit> initializers)
    {
        ContractUtils.RequiresNotNull(newExpression, nameof(newExpression));
        ContractUtils.RequiresNotNull(initializers, nameof(initializers));
        var initializers1 = initializers.ToReadOnly();
        if (initializers1.Count == 0)
        {
            throw Error.ListInitializerWithZeroMembers();
        }

        ValidateListInitArgs(newExpression.Type, initializers1);
        return new ListInitExpression(newExpression, initializers1);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.LoopExpression" /> with the given body.</summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.LoopExpression" />.</returns>
    /// <param name="body">The body of the loop.</param>
    public static LoopExpression Loop(Expression body)
    {
        return Loop(body, null);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.LoopExpression" /> with the given body and break target.</summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.LoopExpression" />.</returns>
    /// <param name="body">The body of the loop.</param>
    /// <param name="break">The break target used by the loop body.</param>
    public static LoopExpression Loop(Expression body, LabelTarget @break)
    {
        return Loop(body, @break, null);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.LoopExpression" /> with the given body.</summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.LoopExpression" />.</returns>
    /// <param name="body">The body of the loop.</param>
    /// <param name="break">The break target used by the loop body.</param>
    /// <param name="continue">The continue target used by the loop body.</param>
    public static LoopExpression Loop(Expression body, LabelTarget @break, LabelTarget @continue)
    {
        RequiresCanRead(body, nameof(body));
        if (@continue != null && @continue.Type != typeof(void))
        {
            throw Error.LabelTypeMustBeVoid();
        }

        return new LoopExpression(body, @break, @continue);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" />, given the left and right operands, by
    ///     calling an appropriate factory method.
    /// </summary>
    /// <returns>
    ///     The <see cref="T:System.Linq.Expressions.BinaryExpression" /> that results from calling the appropriate
    ///     factory method.
    /// </returns>
    /// <param name="binaryType">
    ///     The <see cref="T:System.Linq.Expressions.ExpressionType" /> that specifies the type of binary
    ///     operation.
    /// </param>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the left operand.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the right operand.</param>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="binaryType" /> does not correspond to a binary expression node.
    /// </exception>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    public static BinaryExpression MakeBinary(
        ExpressionType binaryType,
        Expression left,
        Expression right)
    {
        return MakeBinary(binaryType, left, right, false, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" />, given the left operand, right operand and
    ///     implementing method, by calling the appropriate factory method.
    /// </summary>
    /// <returns>
    ///     The <see cref="T:System.Linq.Expressions.BinaryExpression" /> that results from calling the appropriate
    ///     factory method.
    /// </returns>
    /// <param name="binaryType">
    ///     The <see cref="T:System.Linq.Expressions.ExpressionType" /> that specifies the type of binary
    ///     operation.
    /// </param>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the left operand.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the right operand.</param>
    /// <param name="liftToNull">
    ///     true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true;
    ///     false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.
    /// </param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that specifies the implementing method.</param>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="binaryType" /> does not correspond to a binary expression node.
    /// </exception>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    public static BinaryExpression MakeBinary(
        ExpressionType binaryType,
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        return MakeBinary(binaryType, left, right, liftToNull, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" />, given the left operand, right operand,
    ///     implementing method and type conversion function, by calling the appropriate factory method.
    /// </summary>
    /// <returns>
    ///     The <see cref="T:System.Linq.Expressions.BinaryExpression" /> that results from calling the appropriate
    ///     factory method.
    /// </returns>
    /// <param name="binaryType">
    ///     The <see cref="T:System.Linq.Expressions.ExpressionType" /> that specifies the type of binary
    ///     operation.
    /// </param>
    /// <param name="left">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the left operand.</param>
    /// <param name="right">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the right operand.</param>
    /// <param name="liftToNull">
    ///     true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true;
    ///     false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.
    /// </param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that specifies the implementing method.</param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> that represents a type conversion
    ///     function. This parameter is used only if <paramref name="binaryType" /> is
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Coalesce" /> or compound assignment..
    /// </param>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="binaryType" /> does not correspond to a binary expression node.
    /// </exception>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
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
                return AndAlso(left, right, method);
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
                return OrElse(left, right, method);
            case ExpressionType.Power:
                return Power(left, right, method);
            case ExpressionType.RightShift:
                return RightShift(left, right, method);
            case ExpressionType.Subtract:
                return Subtract(left, right, method);
            case ExpressionType.SubtractChecked:
                return SubtractChecked(left, right, method);
            case ExpressionType.Assign:
                return Assign(left, right);
            case ExpressionType.AddAssign:
                return AddAssign(left, right, method, conversion);
            case ExpressionType.AndAssign:
                return AndAssign(left, right, method, conversion);
            case ExpressionType.DivideAssign:
                return DivideAssign(left, right, method, conversion);
            case ExpressionType.ExclusiveOrAssign:
                return ExclusiveOrAssign(left, right, method, conversion);
            case ExpressionType.LeftShiftAssign:
                return LeftShiftAssign(left, right, method, conversion);
            case ExpressionType.ModuloAssign:
                return ModuloAssign(left, right, method, conversion);
            case ExpressionType.MultiplyAssign:
                return MultiplyAssign(left, right, method, conversion);
            case ExpressionType.OrAssign:
                return OrAssign(left, right, method, conversion);
            case ExpressionType.PowerAssign:
                return PowerAssign(left, right, method, conversion);
            case ExpressionType.RightShiftAssign:
                return RightShiftAssign(left, right, method, conversion);
            case ExpressionType.SubtractAssign:
                return SubtractAssign(left, right, method, conversion);
            case ExpressionType.AddAssignChecked:
                return AddAssignChecked(left, right, method, conversion);
            case ExpressionType.MultiplyAssignChecked:
                return MultiplyAssignChecked(left, right, method, conversion);
            case ExpressionType.SubtractAssignChecked:
                return SubtractAssignChecked(left, right, method, conversion);
            default:
                throw Error.UnhandledBinary(binaryType);
        }
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.CatchBlock" /> representing a catch statement with the
    ///     specified elements.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.CatchBlock" />.</returns>
    /// <param name="type">
    ///     The <see cref="P:System.Linq.Expressions.Expression.Type" /> of <see cref="T:System.Exception" />
    ///     this <see cref="T:System.Linq.Expressions.CatchBlock" /> will handle.
    /// </param>
    /// <param name="variable">
    ///     A <see cref="T:System.Linq.Expressions.ParameterExpression" /> representing a reference to the
    ///     <see cref="T:System.Exception" /> object caught by this handler.
    /// </param>
    /// <param name="body">The body of the catch statement.</param>
    /// <param name="filter">The body of the <see cref="T:System.Exception" /> filter.</param>
    public static CatchBlock MakeCatchBlock(
        Type type,
        ParameterExpression variable,
        Expression body,
        Expression filter)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        ContractUtils.Requires(variable == null || TypeUtils.AreEquivalent(variable.Type, type), nameof(variable));
        if (variable != null && variable.IsByRef)
        {
            throw Error.VariableMustNotBeByRef(variable, variable.Type);
        }

        RequiresCanRead(body, nameof(body));
        if (filter != null)
        {
            RequiresCanRead(filter, nameof(filter));
            if (filter.Type != typeof(bool))
            {
                throw Error.ArgumentMustBeBoolean();
            }
        }

        return new CatchBlock(type, variable, body, filter);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.DelegateType" />,
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" />, and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="delegateType">
    ///     The type of the delegate used by the
    ///     <see cref="T:System.Runtime.CompilerServices.CallSite" />.
    /// </param>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="arguments">The arguments to the dynamic operation.</param>
    public static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        params Expression[] arguments)
    {
        return MakeDynamic(delegateType, binder, (IEnumerable<Expression>)arguments);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.DelegateType" />,
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" />, and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="delegateType">
    ///     The type of the delegate used by the
    ///     <see cref="T:System.Runtime.CompilerServices.CallSite" />.
    /// </param>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="arguments">The arguments to the dynamic operation.</param>
    public static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        IEnumerable<Expression> arguments)
    {
        ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        var methodInfo = delegateType.IsSubclassOf(typeof(MulticastDelegate))
            ? GetValidMethodForDynamic(delegateType)
            : throw Error.TypeMustBeDerivedFromSystemDelegate();
        var arguments1 = arguments.ToReadOnly();
        ValidateArgumentTypes(methodInfo, ExpressionType.Dynamic, ref arguments1);
        return DynamicExpression.Make(methodInfo.GetReturnType(), delegateType, binder, arguments1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" /> and one argument.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.DelegateType" />,
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" />, and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="delegateType">
    ///     The type of the delegate used by the
    ///     <see cref="T:System.Runtime.CompilerServices.CallSite" />.
    /// </param>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="arg0">The argument to the dynamic operation.</param>
    public static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0)
    {
        ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        var methodInfo = delegateType.IsSubclassOf(typeof(MulticastDelegate))
            ? GetValidMethodForDynamic(delegateType)
            : throw Error.TypeMustBeDerivedFromSystemDelegate();
        var parametersCached = methodInfo.GetParameters();
        ValidateArgumentCount(methodInfo, ExpressionType.Dynamic, 2, parametersCached);
        ValidateDynamicArgument(arg0);
        ValidateOneArgument(methodInfo, ExpressionType.Dynamic, arg0, parametersCached[1]);
        return DynamicExpression.Make(methodInfo.GetReturnType(), delegateType, binder, arg0);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" /> and two arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.DelegateType" />,
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" />, and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="delegateType">
    ///     The type of the delegate used by the
    ///     <see cref="T:System.Runtime.CompilerServices.CallSite" />.
    /// </param>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="arg0">The first argument to the dynamic operation.</param>
    /// <param name="arg1">The second argument to the dynamic operation.</param>
    public static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0,
        Expression arg1)
    {
        ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        var methodInfo = delegateType.IsSubclassOf(typeof(MulticastDelegate))
            ? GetValidMethodForDynamic(delegateType)
            : throw Error.TypeMustBeDerivedFromSystemDelegate();
        var parametersCached = methodInfo.GetParameters();
        ValidateArgumentCount(methodInfo, ExpressionType.Dynamic, 3, parametersCached);
        ValidateDynamicArgument(arg0);
        ValidateOneArgument(methodInfo, ExpressionType.Dynamic, arg0, parametersCached[1]);
        ValidateDynamicArgument(arg1);
        ValidateOneArgument(methodInfo, ExpressionType.Dynamic, arg1, parametersCached[2]);
        return DynamicExpression.Make(methodInfo.GetReturnType(), delegateType, binder, arg0, arg1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" /> and three arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.DelegateType" />,
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" />, and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="delegateType">
    ///     The type of the delegate used by the
    ///     <see cref="T:System.Runtime.CompilerServices.CallSite" />.
    /// </param>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="arg0">The first argument to the dynamic operation.</param>
    /// <param name="arg1">The second argument to the dynamic operation.</param>
    /// <param name="arg2">The third argument to the dynamic operation.</param>
    public static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0,
        Expression arg1,
        Expression arg2)
    {
        ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        var methodInfo = delegateType.IsSubclassOf(typeof(MulticastDelegate))
            ? GetValidMethodForDynamic(delegateType)
            : throw Error.TypeMustBeDerivedFromSystemDelegate();
        var parametersCached = methodInfo.GetParameters();
        ValidateArgumentCount(methodInfo, ExpressionType.Dynamic, 4, parametersCached);
        ValidateDynamicArgument(arg0);
        ValidateOneArgument(methodInfo, ExpressionType.Dynamic, arg0, parametersCached[1]);
        ValidateDynamicArgument(arg1);
        ValidateOneArgument(methodInfo, ExpressionType.Dynamic, arg1, parametersCached[2]);
        ValidateDynamicArgument(arg2);
        ValidateOneArgument(methodInfo, ExpressionType.Dynamic, arg2, parametersCached[3]);
        return DynamicExpression.Make(methodInfo.GetReturnType(), delegateType, binder, arg0, arg1, arg2);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound
    ///     by the provided <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" /> and four arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.DynamicExpression" /> that has
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Dynamic" /> and has the
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.DelegateType" />,
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Binder" />, and
    ///     <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.
    /// </returns>
    /// <param name="delegateType">
    ///     The type of the delegate used by the
    ///     <see cref="T:System.Runtime.CompilerServices.CallSite" />.
    /// </param>
    /// <param name="binder">The runtime binder for the dynamic operation.</param>
    /// <param name="arg0">The first argument to the dynamic operation.</param>
    /// <param name="arg1">The second argument to the dynamic operation.</param>
    /// <param name="arg2">The third argument to the dynamic operation.</param>
    /// <param name="arg3">The fourth argument to the dynamic operation.</param>
    public static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0,
        Expression arg1,
        Expression arg2,
        Expression arg3)
    {
        ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        var methodInfo = delegateType.IsSubclassOf(typeof(MulticastDelegate))
            ? GetValidMethodForDynamic(delegateType)
            : throw Error.TypeMustBeDerivedFromSystemDelegate();
        var parametersCached = methodInfo.GetParameters();
        ValidateArgumentCount(methodInfo, ExpressionType.Dynamic, 5, parametersCached);
        ValidateDynamicArgument(arg0);
        ValidateOneArgument(methodInfo, ExpressionType.Dynamic, arg0, parametersCached[1]);
        ValidateDynamicArgument(arg1);
        ValidateOneArgument(methodInfo, ExpressionType.Dynamic, arg1, parametersCached[2]);
        ValidateDynamicArgument(arg2);
        ValidateOneArgument(methodInfo, ExpressionType.Dynamic, arg2, parametersCached[3]);
        ValidateDynamicArgument(arg3);
        ValidateOneArgument(methodInfo, ExpressionType.Dynamic, arg3, parametersCached[4]);
        return DynamicExpression.Make(methodInfo.GetReturnType(), delegateType, binder, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a jump of the specified
    ///     <see cref="T:System.Linq.Expressions.GotoExpressionKind" />. The value passed to the label upon jumping can also be
    ///     specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to <paramref name="kind" />, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and
    ///     <paramref name="value" /> to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="kind">
    ///     The <see cref="T:System.Linq.Expressions.GotoExpressionKind" /> of the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" />.
    /// </param>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
    /// <param name="type">
    ///     An <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    public static GotoExpression MakeGoto(
        GotoExpressionKind kind,
        LabelTarget target,
        Expression value,
        Type type)
    {
        ValidateGoto(target, ref value, nameof(target), nameof(value));
        return new GotoExpression(kind, target, value, type);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.IndexExpression" /> that represents accessing an indexed
    ///     property in an object.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.IndexExpression" />.</returns>
    /// <param name="instance">
    ///     The object to which the property belongs. It should be null if the property is static (shared in
    ///     Visual Basic).
    /// </param>
    /// <param name="indexer">An <see cref="T:System.Linq.Expressions.Expression" /> representing the property to index.</param>
    /// <param name="arguments">
    ///     An IEnumerable&lt;Expression&gt; (IEnumerable (Of Expression) in Visual Basic) that contains
    ///     the arguments that will be used to index the property.
    /// </param>
    public static IndexExpression MakeIndex(
        Expression instance,
        PropertyInfo indexer,
        IEnumerable<Expression> arguments)
    {
        return indexer != null ? Property(instance, indexer, arguments) : ArrayAccess(instance, arguments);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing either a field
    ///     or a property.
    /// </summary>
    /// <returns>
    ///     The <see cref="T:System.Linq.Expressions.MemberExpression" /> that results from calling the appropriate
    ///     factory method.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> that represents the object that the
    ///     member belongs to. This can be null for static members.
    /// </param>
    /// <param name="member">
    ///     The <see cref="T:System.Reflection.MemberInfo" /> that describes the field or property to be
    ///     accessed.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> or <paramref name="member" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="member" /> does not represent a field or property.
    /// </exception>
    public static MemberExpression MakeMemberAccess(Expression expression, MemberInfo member)
    {
        ContractUtils.RequiresNotNull(member, nameof(member));
        var field = member as FieldInfo;
        if (field != null)
        {
            return Field(expression, field);
        }

        var property = member as PropertyInfo;
        return property != null ? Property(expression, property) : throw Error.MemberNotFieldOrProperty(member);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.TryExpression" /> representing a try block with the specified
    ///     elements.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.TryExpression" />.</returns>
    /// <param name="type">The result type of the try expression. If null, bodh and all handlers must have identical type.</param>
    /// <param name="body">The body of the try block.</param>
    /// <param name="finally">
    ///     The body of the finally block. Pass null if the try block has no finally block associated with
    ///     it.
    /// </param>
    /// <param name="fault">The body of the try block. Pass null if the try block has no fault block associated with it.</param>
    /// <param name="handlers">
    ///     A collection of <see cref="T:System.Linq.Expressions.CatchBlock" />s representing the catch
    ///     statements to be associated with the try block.
    /// </param>
    public static TryExpression MakeTry(
        Type type,
        Expression body,
        Expression @finally,
        Expression fault,
        IEnumerable<CatchBlock> handlers)
    {
        RequiresCanRead(body, nameof(body));
        var readOnlyCollection = handlers.ToReadOnly();
        ContractUtils.RequiresNotNullItems(readOnlyCollection, nameof(handlers));
        ValidateTryAndCatchHaveSameType(type, body, readOnlyCollection);
        if (fault != null)
        {
            if (@finally != null || readOnlyCollection.Count > 0)
            {
                throw Error.FaultCannotHaveCatchOrFinally();
            }

            RequiresCanRead(fault, nameof(fault));
        }
        else if (@finally != null)
        {
            RequiresCanRead(@finally, nameof(@finally));
        }
        else if (readOnlyCollection.Count == 0)
        {
            throw Error.TryMustHaveCatchFinallyOrFault();
        }

        var type1 = type;
        if (type1 == null)
        {
            type1 = body.Type;
        }

        return new TryExpression(type1, body, @finally, fault, readOnlyCollection);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" />, given an operand, by calling the
    ///     appropriate factory method.
    /// </summary>
    /// <returns>
    ///     The <see cref="T:System.Linq.Expressions.UnaryExpression" /> that results from calling the appropriate factory
    ///     method.
    /// </returns>
    /// <param name="unaryType">
    ///     The <see cref="T:System.Linq.Expressions.ExpressionType" /> that specifies the type of unary
    ///     operation.
    /// </param>
    /// <param name="operand">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the operand.</param>
    /// <param name="type">
    ///     The <see cref="T:System.Type" /> that specifies the type to be converted to (pass null if not
    ///     applicable).
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="operand" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="unaryType" /> does not correspond to a unary expression node.
    /// </exception>
    public static UnaryExpression MakeUnary(ExpressionType unaryType, Expression operand, Type type)
    {
        return MakeUnary(unaryType, operand, type, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" />, given an operand and implementing method,
    ///     by calling the appropriate factory method.
    /// </summary>
    /// <returns>
    ///     The <see cref="T:System.Linq.Expressions.UnaryExpression" /> that results from calling the appropriate factory
    ///     method.
    /// </returns>
    /// <param name="unaryType">
    ///     The <see cref="T:System.Linq.Expressions.ExpressionType" /> that specifies the type of unary
    ///     operation.
    /// </param>
    /// <param name="operand">An <see cref="T:System.Linq.Expressions.Expression" /> that represents the operand.</param>
    /// <param name="type">
    ///     The <see cref="T:System.Type" /> that specifies the type to be converted to (pass null if not
    ///     applicable).
    /// </param>
    /// <param name="method">The <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="operand" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="unaryType" /> does not correspond to a unary expression node.
    /// </exception>
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
            case ExpressionType.UnaryPlus:
                return UnaryPlus(operand, method);
            case ExpressionType.NegateChecked:
                return NegateChecked(operand, method);
            case ExpressionType.Not:
                return Not(operand, method);
            case ExpressionType.Quote:
                return Quote(operand);
            case ExpressionType.TypeAs:
                return TypeAs(operand, type);
            case ExpressionType.Decrement:
                return Decrement(operand, method);
            case ExpressionType.Increment:
                return Increment(operand, method);
            case ExpressionType.Throw:
                return Throw(operand, type);
            case ExpressionType.Unbox:
                return Unbox(operand, type);
            case ExpressionType.PreIncrementAssign:
                return PreIncrementAssign(operand, method);
            case ExpressionType.PreDecrementAssign:
                return PreDecrementAssign(operand, method);
            case ExpressionType.PostIncrementAssign:
                return PostIncrementAssign(operand, method);
            case ExpressionType.PostDecrementAssign:
                return PostDecrementAssign(operand, method);
            case ExpressionType.OnesComplement:
                return OnesComplement(operand, method);
            case ExpressionType.IsTrue:
                return IsTrue(operand, method);
            case ExpressionType.IsFalse:
                return IsFalse(operand, method);
            default:
                throw Error.UnhandledUnary(unaryType);
        }
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive
    ///     initialization of members of a field or property.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that has the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.MemberBindingType.MemberBinding" /> and the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and
    ///     <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> properties set to the specified values.
    /// </returns>
    /// <param name="member">
    ///     The <see cref="T:System.Reflection.MemberInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.
    /// </param>
    /// <param name="bindings">
    ///     An array of <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate
    ///     the <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="member" /> or <paramref name="bindings" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="member" /> does not represent a field or property.-or-The
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property of an element of
    ///     <paramref name="bindings" /> does not represent a member of the type of the field or property that
    ///     <paramref name="member" /> represents.
    /// </exception>
    public static MemberMemberBinding MemberBind(MemberInfo member, params MemberBinding[] bindings)
    {
        ContractUtils.RequiresNotNull(member, nameof(member));
        ContractUtils.RequiresNotNull(bindings, nameof(bindings));
        return MemberBind(member, (IEnumerable<MemberBinding>)bindings);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive
    ///     initialization of members of a field or property.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that has the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.MemberBindingType.MemberBinding" /> and the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> and
    ///     <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> properties set to the specified values.
    /// </returns>
    /// <param name="member">
    ///     The <see cref="T:System.Reflection.MemberInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property equal to.
    /// </param>
    /// <param name="bindings">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="member" /> or <paramref name="bindings" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="member" /> does not represent a field or property.-or-The
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property of an element of
    ///     <paramref name="bindings" /> does not represent a member of the type of the field or property that
    ///     <paramref name="member" /> represents.
    /// </exception>
    public static MemberMemberBinding MemberBind(
        MemberInfo member,
        IEnumerable<MemberBinding> bindings)
    {
        ContractUtils.RequiresNotNull(member, nameof(member));
        ContractUtils.RequiresNotNull(bindings, nameof(bindings));
        var bindings1 = bindings.ToReadOnly();
        Type memberType;
        ValidateGettableFieldOrPropertyMember(member, out memberType);
        ValidateMemberInitArgs(memberType, bindings1);
        return new MemberMemberBinding(member, bindings1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive
    ///     initialization of members of a member that is accessed by using a property accessor method.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that has the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.MemberBindingType.MemberBinding" />, the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the
    ///     <see cref="T:System.Reflection.PropertyInfo" /> that represents the property accessed in
    ///     <paramref name="propertyAccessor" />, and <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" />
    ///     properties set to the specified values.
    /// </returns>
    /// <param name="propertyAccessor">
    ///     The <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor
    ///     method.
    /// </param>
    /// <param name="bindings">
    ///     An array of <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate
    ///     the <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="propertyAccessor" /> or <paramref name="bindings" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property of an element of
    ///     <paramref name="bindings" /> does not represent a member of the type of the property accessed by the method that
    ///     <paramref name="propertyAccessor" /> represents.
    /// </exception>
    public static MemberMemberBinding MemberBind(
        MethodInfo propertyAccessor,
        params MemberBinding[] bindings)
    {
        ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
        return MemberBind(GetProperty(propertyAccessor), bindings);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that represents the recursive
    ///     initialization of members of a member that is accessed by using a property accessor method.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberMemberBinding" /> that has the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.BindingType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.MemberBindingType.MemberBinding" />, the
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property set to the
    ///     <see cref="T:System.Reflection.PropertyInfo" /> that represents the property accessed in
    ///     <paramref name="propertyAccessor" />, and <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" />
    ///     properties set to the specified values.
    /// </returns>
    /// <param name="propertyAccessor">
    ///     The <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor
    ///     method.
    /// </param>
    /// <param name="bindings">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.MemberMemberBinding.Bindings" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="propertyAccessor" /> or <paramref name="bindings" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="propertyAccessor" /> does not represent a property accessor method.-or-The
    ///     <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property of an element of
    ///     <paramref name="bindings" /> does not represent a member of the type of the property accessed by the method that
    ///     <paramref name="propertyAccessor" /> represents.
    /// </exception>
    public static MemberMemberBinding MemberBind(
        MethodInfo propertyAccessor,
        IEnumerable<MemberBinding> bindings)
    {
        ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
        return MemberBind(GetProperty(propertyAccessor), bindings);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberInitExpression" />.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberInitExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MemberInit" /> and the
    ///     <see cref="P:System.Linq.Expressions.MemberInitExpression.NewExpression" /> and
    ///     <see cref="P:System.Linq.Expressions.MemberInitExpression.Bindings" /> properties set to the specified values.
    /// </returns>
    /// <param name="newExpression">
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberInitExpression.NewExpression" /> property equal to.
    /// </param>
    /// <param name="bindings">
    ///     An array of <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate
    ///     the <see cref="P:System.Linq.Expressions.MemberInitExpression.Bindings" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="newExpression" /> or <paramref name="bindings" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property
    ///     of an element of <paramref name="bindings" /> does not represent a member of the type that
    ///     <paramref name="newExpression" />.Type represents.
    /// </exception>
    public static MemberInitExpression MemberInit(
        NewExpression newExpression,
        params MemberBinding[] bindings)
    {
        return MemberInit(newExpression, (IEnumerable<MemberBinding>)bindings);
    }

    /// <summary>Represents an expression that creates a new object and initializes a property of the object.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberInitExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MemberInit" /> and the
    ///     <see cref="P:System.Linq.Expressions.MemberInitExpression.NewExpression" /> and
    ///     <see cref="P:System.Linq.Expressions.MemberInitExpression.Bindings" /> properties set to the specified values.
    /// </returns>
    /// <param name="newExpression">
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberInitExpression.NewExpression" /> property equal to.
    /// </param>
    /// <param name="bindings">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.MemberBinding" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.MemberInitExpression.Bindings" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="newExpression" /> or <paramref name="bindings" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The <see cref="P:System.Linq.Expressions.MemberBinding.Member" /> property
    ///     of an element of <paramref name="bindings" /> does not represent a member of the type that
    ///     <paramref name="newExpression" />.Type represents.
    /// </exception>
    public static MemberInitExpression MemberInit(
        NewExpression newExpression,
        IEnumerable<MemberBinding> bindings)
    {
        ContractUtils.RequiresNotNull(newExpression, nameof(newExpression));
        ContractUtils.RequiresNotNull(bindings, nameof(bindings));
        var bindings1 = bindings.ToReadOnly();
        ValidateMemberInitArgs(newExpression.Type, bindings1);
        return new MemberInitExpression(newExpression, bindings1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic remainder
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Modulo" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The modulus operator is not defined for <paramref name="left" />
    ///     .Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Modulo(Expression left, Expression right)
    {
        return Modulo(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic remainder
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Modulo" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the modulus operator is not defined for <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Modulo(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.Modulo, left, right, method, true);
        }

        return left.Type == right.Type && TypeUtils.IsArithmetic(left.Type)
            ? new SimpleBinaryExpression(ExpressionType.Modulo, left, right, left.Type)
            : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Modulo, "op_Modulus", left, right, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a remainder assignment
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ModuloAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression ModuloAssign(Expression left, Expression right)
    {
        return ModuloAssign(left, right, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a remainder assignment
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ModuloAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression ModuloAssign(Expression left, Expression right, MethodInfo method)
    {
        return ModuloAssign(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a remainder assignment
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.ModuloAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression ModuloAssign(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.ModuloAssign, left, right, method, conversion, true);
        }

        if (!(left.Type == right.Type) || !TypeUtils.IsArithmetic(left.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.ModuloAssign, "op_Modulus", left, right,
                conversion, true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        return new SimpleBinaryExpression(ExpressionType.ModuloAssign, left, right, left.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic
    ///     multiplication operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Multiply" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The multiplication operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Multiply(Expression left, Expression right)
    {
        return Multiply(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic
    ///     multiplication operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Multiply" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the multiplication operator is not defined for <paramref name="left" />.Type
    ///     and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Multiply(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.Multiply, left, right, method, true);
        }

        return left.Type == right.Type && TypeUtils.IsArithmetic(left.Type)
            ? new SimpleBinaryExpression(ExpressionType.Multiply, left, right, left.Type)
            : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Multiply, "op_Multiply", left, right, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a multiplication
    ///     assignment operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MultiplyAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression MultiplyAssign(Expression left, Expression right)
    {
        return MultiplyAssign(left, right, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a multiplication
    ///     assignment operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MultiplyAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression MultiplyAssign(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        return MultiplyAssign(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a multiplication
    ///     assignment operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MultiplyAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression MultiplyAssign(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.MultiplyAssign, left, right, method, conversion, true);
        }

        if (!(left.Type == right.Type) || !TypeUtils.IsArithmetic(left.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.MultiplyAssign, "op_Multiply", left, right,
                conversion, true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        return new SimpleBinaryExpression(ExpressionType.MultiplyAssign, left, right, left.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a multiplication
    ///     assignment operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MultiplyAssignChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression MultiplyAssignChecked(Expression left, Expression right)
    {
        return MultiplyAssignChecked(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a multiplication
    ///     assignment operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MultiplyAssignChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression MultiplyAssignChecked(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        return MultiplyAssignChecked(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a multiplication
    ///     assignment operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MultiplyAssignChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression MultiplyAssignChecked(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.MultiplyAssignChecked, left, right, method, conversion,
                true);
        }

        if (!(left.Type == right.Type) || !TypeUtils.IsArithmetic(left.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.MultiplyAssignChecked, "op_Multiply", left, right,
                conversion, true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        return new SimpleBinaryExpression(ExpressionType.MultiplyAssignChecked, left, right, left.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic
    ///     multiplication operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MultiplyChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The multiplication operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression MultiplyChecked(Expression left, Expression right)
    {
        return MultiplyChecked(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic
    ///     multiplication operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MultiplyChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the multiplication operator is not defined for <paramref name="left" />.Type
    ///     and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression MultiplyChecked(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.MultiplyChecked, left, right, method, true);
        }

        return left.Type == right.Type && TypeUtils.IsArithmetic(left.Type)
            ? new SimpleBinaryExpression(ExpressionType.MultiplyChecked, left, right, left.Type)
            : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.MultiplyChecked, "op_Multiply", left, right, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Negate" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The unary minus operator is not defined for
    ///     <paramref name="expression" />.Type.
    /// </exception>
    public static UnaryExpression Negate(Expression expression)
    {
        return Negate(expression, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Negate" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly one argument.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the unary minus operator is not defined for <paramref name="expression" />
    ///     .Type.-or-<paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value
    ///     type) is not assignable to the argument type of the method represented by <paramref name="method" />.
    /// </exception>
    public static UnaryExpression Negate(Expression expression, MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        if (!(method == null))
        {
            return GetMethodBasedUnaryOperator(ExpressionType.Negate, expression, method);
        }

        return TypeUtils.IsArithmetic(expression.Type) && !TypeUtils.IsUnsignedInt(expression.Type)
            ? new UnaryExpression(ExpressionType.Negate, expression, expression.Type, null)
            : GetUserDefinedUnaryOperatorOrThrow(ExpressionType.Negate, "op_UnaryNegation", expression);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation
    ///     operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.NegateChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The unary minus operator is not defined for
    ///     <paramref name="expression" />.Type.
    /// </exception>
    public static UnaryExpression NegateChecked(Expression expression)
    {
        return NegateChecked(expression, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an arithmetic negation
    ///     operation that has overflow checking. The implementing method can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.NegateChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly one argument.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the unary minus operator is not defined for <paramref name="expression" />
    ///     .Type.-or-<paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value
    ///     type) is not assignable to the argument type of the method represented by <paramref name="method" />.
    /// </exception>
    public static UnaryExpression NegateChecked(Expression expression, MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        if (!(method == null))
        {
            return GetMethodBasedUnaryOperator(ExpressionType.NegateChecked, expression, method);
        }

        return TypeUtils.IsArithmetic(expression.Type) && !TypeUtils.IsUnsignedInt(expression.Type)
            ? new UnaryExpression(ExpressionType.NegateChecked, expression, expression.Type, null)
            : GetUserDefinedUnaryOperatorOrThrow(ExpressionType.NegateChecked, "op_UnaryNegation", expression);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the specified
    ///     constructor that takes no arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property set to the specified value.
    /// </returns>
    /// <param name="constructor">
    ///     The <see cref="T:System.Reflection.ConstructorInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="constructor" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The constructor that <paramref name="constructor" /> represents has at
    ///     least one parameter.
    /// </exception>
    public static NewExpression New(ConstructorInfo constructor)
    {
        return New(constructor, (IEnumerable<Expression>)null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the specified
    ///     constructor with the specified arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> and
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> properties set to the specified values.
    /// </returns>
    /// <param name="constructor">
    ///     The <see cref="T:System.Reflection.ConstructorInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property equal to.
    /// </param>
    /// <param name="arguments">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="constructor" /> is null.-or-An element of <paramref name="arguments" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The length of <paramref name="arguments" /> does match the number of
    ///     parameters for the constructor that <paramref name="constructor" /> represents.-or-The
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is
    ///     not assignable to the type of the corresponding parameter of the constructor that <paramref name="constructor" />
    ///     represents.
    /// </exception>
    public static NewExpression New(ConstructorInfo constructor, params Expression[] arguments)
    {
        return New(constructor, (IEnumerable<Expression>)arguments);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the specified
    ///     constructor with the specified arguments.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> and
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> properties set to the specified values.
    /// </returns>
    /// <param name="constructor">
    ///     The <see cref="T:System.Reflection.ConstructorInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property equal to.
    /// </param>
    /// <param name="arguments">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="constructor" /> is null.-or-An element of <paramref name="arguments" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The <paramref name="arguments" /> parameter does not contain the same
    ///     number of elements as the number of parameters for the constructor that <paramref name="constructor" />
    ///     represents.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of
    ///     <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the constructor that
    ///     <paramref name="constructor" /> represents.
    /// </exception>
    public static NewExpression New(ConstructorInfo constructor, IEnumerable<Expression> arguments)
    {
        ContractUtils.RequiresNotNull(constructor, nameof(constructor));
        ContractUtils.RequiresNotNull(constructor.DeclaringType, "constructor.DeclaringType");
        TypeUtils.ValidateType(constructor.DeclaringType);
        var arguments1 = arguments.ToReadOnly();
        ValidateArgumentTypes(constructor, ExpressionType.New, ref arguments1);
        return new NewExpression(constructor, arguments1, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the specified
    ///     constructor with the specified arguments. The members that access the constructor initialized fields are specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Constructor" />,
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> and
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Members" /> properties set to the specified values.
    /// </returns>
    /// <param name="constructor">
    ///     The <see cref="T:System.Reflection.ConstructorInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property equal to.
    /// </param>
    /// <param name="arguments">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> collection.
    /// </param>
    /// <param name="members">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Reflection.MemberInfo" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Members" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="constructor" /> is null.-or-An element of <paramref name="arguments" /> is null.-or-An element of
    ///     <paramref name="members" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The <paramref name="arguments" /> parameter does not contain the same
    ///     number of elements as the number of parameters for the constructor that <paramref name="constructor" />
    ///     represents.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of
    ///     <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the constructor that
    ///     <paramref name="constructor" /> represents.-or-The <paramref name="members" /> parameter does not have the same
    ///     number of elements as <paramref name="arguments" />.-or-An element of <paramref name="arguments" /> has a
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property that represents a type that is not assignable to
    ///     the type of the member that is represented by the corresponding element of <paramref name="members" />.
    /// </exception>
    public static NewExpression New(
        ConstructorInfo constructor,
        IEnumerable<Expression> arguments,
        IEnumerable<MemberInfo> members)
    {
        ContractUtils.RequiresNotNull(constructor, nameof(constructor));
        var members1 = members.ToReadOnly();
        var arguments1 = arguments.ToReadOnly();
        ValidateNewArgs(constructor, ref arguments1, ref members1);
        return new NewExpression(constructor, arguments1, members1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the specified
    ///     constructor with the specified arguments. The members that access the constructor initialized fields are specified
    ///     as an array.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Constructor" />,
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> and
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Members" /> properties set to the specified values.
    /// </returns>
    /// <param name="constructor">
    ///     The <see cref="T:System.Reflection.ConstructorInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property equal to.
    /// </param>
    /// <param name="arguments">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> collection.
    /// </param>
    /// <param name="members">
    ///     An array of <see cref="T:System.Reflection.MemberInfo" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Members" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="constructor" /> is null.-or-An element of <paramref name="arguments" /> is null.-or-An element of
    ///     <paramref name="members" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The <paramref name="arguments" /> parameter does not contain the same
    ///     number of elements as the number of parameters for the constructor that <paramref name="constructor" />
    ///     represents.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of
    ///     <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the constructor that
    ///     <paramref name="constructor" /> represents.-or-The <paramref name="members" /> parameter does not have the same
    ///     number of elements as <paramref name="arguments" />.-or-An element of <paramref name="arguments" /> has a
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property that represents a type that is not assignable to
    ///     the type of the member that is represented by the corresponding element of <paramref name="members" />.
    /// </exception>
    public static NewExpression New(
        ConstructorInfo constructor,
        IEnumerable<Expression> arguments,
        params MemberInfo[] members)
    {
        return New(constructor, arguments, (IEnumerable<MemberInfo>)members);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.NewExpression" /> that represents calling the parameterless
    ///     constructor of the specified type.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.NewExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.New" /> and the
    ///     <see cref="P:System.Linq.Expressions.NewExpression.Constructor" /> property set to the
    ///     <see cref="T:System.Reflection.ConstructorInfo" /> that represents the constructor without parameters for the
    ///     specified type.
    /// </returns>
    /// <param name="type">A <see cref="T:System.Type" /> that has a constructor that takes no arguments.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="type" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The type that <paramref name="type" /> represents does not have a
    ///     constructor without parameters.
    /// </exception>
    public static NewExpression New(Type type)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        if (type == typeof(void))
        {
            throw Error.ArgumentCannotBeOfTypeVoid();
        }

        if (type.IsValueType)
        {
            return new NewValueTypeExpression(type, EmptyReadOnlyCollection<Expression>.Instance, null);
        }

        var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null);
        return !(constructor == null) ? New(constructor) : throw Error.TypeMissingDefaultConstructor(type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that represents creating an array that
    ///     has a specified rank.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.NewArrayBounds" /> and the
    ///     <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.
    /// </returns>
    /// <param name="type">A <see cref="T:System.Type" /> that represents the element type of the array.</param>
    /// <param name="bounds">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="type" /> or <paramref name="bounds" /> is null.-or-An element of <paramref name="bounds" /> is
    ///     null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of
    ///     an element of <paramref name="bounds" /> does not represent an integral type.
    /// </exception>
    public static NewArrayExpression NewArrayBounds(Type type, params Expression[] bounds)
    {
        return NewArrayBounds(type, (IEnumerable<Expression>)bounds);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that represents creating an array that
    ///     has a specified rank.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.NewArrayBounds" /> and the
    ///     <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.
    /// </returns>
    /// <param name="type">A <see cref="T:System.Type" /> that represents the element type of the array.</param>
    /// <param name="bounds">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="type" /> or <paramref name="bounds" /> is null.-or-An element of <paramref name="bounds" /> is
    ///     null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of
    ///     an element of <paramref name="bounds" /> does not represent an integral type.
    /// </exception>
    public static NewArrayExpression NewArrayBounds(Type type, IEnumerable<Expression> bounds)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        ContractUtils.RequiresNotNull(bounds, nameof(bounds));
        if (type.Equals(typeof(void)))
        {
            throw Error.ArgumentCannotBeOfTypeVoid();
        }

        var readOnlyCollection = bounds.ToReadOnly();
        var count = readOnlyCollection.Count;
        if (count <= 0)
        {
            throw Error.BoundsCannotBeLessThanOne();
        }

        for (var index = 0; index < count; ++index)
        {
            var expression = readOnlyCollection[index];
            RequiresCanRead(expression, nameof(bounds));
            if (!TypeUtils.IsInteger(expression.Type))
            {
                throw Error.ArgumentMustBeInteger();
            }
        }

        return NewArrayExpression.Make(ExpressionType.NewArrayBounds,
            count != 1 ? type.MakeArrayType(count) : type.MakeArrayType(), bounds.ToReadOnly());
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that represents creating a
    ///     one-dimensional array and initializing it from a list of elements.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.NewArrayInit" /> and the
    ///     <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.
    /// </returns>
    /// <param name="type">A <see cref="T:System.Type" /> that represents the element type of the array.</param>
    /// <param name="initializers">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate
    ///     the <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="type" /> or <paramref name="initializers" /> is null.-or-An element of
    ///     <paramref name="initializers" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property of an element of <paramref name="initializers" /> represents a type that is not assignable to the type
    ///     <paramref name="type" />.
    /// </exception>
    public static NewArrayExpression NewArrayInit(Type type, params Expression[] initializers)
    {
        return NewArrayInit(type, (IEnumerable<Expression>)initializers);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that represents creating a
    ///     one-dimensional array and initializing it from a list of elements.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.NewArrayExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.NewArrayInit" /> and the
    ///     <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> property set to the specified value.
    /// </returns>
    /// <param name="type">A <see cref="T:System.Type" /> that represents the element type of the array.</param>
    /// <param name="initializers">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> that contains
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects to use to populate the
    ///     <see cref="P:System.Linq.Expressions.NewArrayExpression.Expressions" /> collection.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="type" /> or <paramref name="initializers" /> is null.-or-An element of
    ///     <paramref name="initializers" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property of an element of <paramref name="initializers" /> represents a type that is not assignable to the type
    ///     that <paramref name="type" /> represents.
    /// </exception>
    public static NewArrayExpression NewArrayInit(Type type, IEnumerable<Expression> initializers)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        ContractUtils.RequiresNotNull(initializers, nameof(initializers));
        if (type.Equals(typeof(void)))
        {
            throw Error.ArgumentCannotBeOfTypeVoid();
        }

        var expressions = initializers.ToReadOnly();
        var list = (Expression[])null;
        var index1 = 0;
        for (var count = expressions.Count; index1 < count; ++index1)
        {
            var expression = expressions[index1];
            RequiresCanRead(expression, nameof(initializers));
            if (!TypeUtils.AreReferenceAssignable(type, expression.Type))
            {
                if (!TryQuote(type, ref expression))
                {
                    throw Error.ExpressionTypeCannotInitializeArrayType(expression.Type, type);
                }

                if (list == null)
                {
                    list = new Expression[expressions.Count];
                    for (var index2 = 0; index2 < index1; ++index2)
                    {
                        list[index2] = expressions[index2];
                    }
                }
            }

            if (list != null)
            {
                list[index1] = expression;
            }
        }

        if (list != null)
        {
            expressions = new TrueReadOnlyCollection<Expression>(list);
        }

        return NewArrayExpression.Make(ExpressionType.NewArrayInit, type.MakeArrayType(), expressions);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a bitwise complement
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Not" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The unary not operator is not defined for
    ///     <paramref name="expression" />.Type.
    /// </exception>
    public static UnaryExpression Not(Expression expression)
    {
        return Not(expression, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a bitwise complement
    ///     operation. The implementing method can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Not" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly one argument.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the unary not operator is not defined for <paramref name="expression" />
    ///     .Type.-or-<paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value
    ///     type) is not assignable to the argument type of the method represented by <paramref name="method" />.
    /// </exception>
    public static UnaryExpression Not(Expression expression, MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        if (!(method == null))
        {
            return GetMethodBasedUnaryOperator(ExpressionType.Not, expression, method);
        }

        return TypeUtils.IsIntegerOrBool(expression.Type)
            ? new UnaryExpression(ExpressionType.Not, expression, expression.Type, null)
            : GetUserDefinedUnaryOperator(ExpressionType.Not, "op_LogicalNot", expression) ??
              GetUserDefinedUnaryOperatorOrThrow(ExpressionType.Not, "op_OnesComplement", expression);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an inequality comparison.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.NotEqual" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The inequality operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression NotEqual(Expression left, Expression right)
    {
        return NotEqual(left, right, false, null);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an inequality comparison.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.NotEqual" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="liftToNull">
    ///     true to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to true;
    ///     false to set <see cref="P:System.Linq.Expressions.BinaryExpression.IsLiftedToNull" /> to false.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the inequality operator is not defined for <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression NotEqual(
        Expression left,
        Expression right,
        bool liftToNull,
        MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        return method == null
            ? GetEqualityComparisonOperator(ExpressionType.NotEqual, "op_Inequality", left, right, liftToNull)
            : GetMethodBasedBinaryOperator(ExpressionType.NotEqual, left, right, method, liftToNull);
    }

    /// <summary>Returns the expression representing the ones complement.</summary>
    /// <returns>An instance of <see cref="T:System.Linq.Expressions.UnaryExpression" />.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" />.</param>
    public static UnaryExpression OnesComplement(Expression expression)
    {
        return OnesComplement(expression, null);
    }

    /// <summary>Returns the expression representing the ones complement.</summary>
    /// <returns>An instance of <see cref="T:System.Linq.Expressions.UnaryExpression" />.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" />.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</param>
    public static UnaryExpression OnesComplement(Expression expression, MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        if (!(method == null))
        {
            return GetMethodBasedUnaryOperator(ExpressionType.OnesComplement, expression, method);
        }

        return TypeUtils.IsInteger(expression.Type)
            ? new UnaryExpression(ExpressionType.OnesComplement, expression, expression.Type, null)
            : GetUserDefinedUnaryOperatorOrThrow(ExpressionType.OnesComplement, "op_OnesComplement", expression);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise OR operation.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Or" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The bitwise OR operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Or(Expression left, Expression right)
    {
        return Or(left, right, null);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise OR operation.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Or" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the bitwise OR operator is not defined for <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Or(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.Or, left, right, method, true);
        }

        return left.Type == right.Type && TypeUtils.IsIntegerOrBool(left.Type)
            ? new SimpleBinaryExpression(ExpressionType.Or, left, right, left.Type)
            : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Or, "op_BitwiseOr", left, right, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise OR assignment
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.OrAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression OrAssign(Expression left, Expression right)
    {
        return OrAssign(left, right, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise OR assignment
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.OrAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression OrAssign(Expression left, Expression right, MethodInfo method)
    {
        return OrAssign(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise OR assignment
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.OrAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression OrAssign(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.OrAssign, left, right, method, conversion, true);
        }

        if (!(left.Type == right.Type) || !TypeUtils.IsIntegerOrBool(left.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.OrAssign, "op_BitwiseOr", left, right, conversion,
                true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        return new SimpleBinaryExpression(ExpressionType.OrAssign, left, right, left.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a conditional OR operation
    ///     that evaluates the second operand only if the first operand evaluates to false.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.OrElse" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The bitwise OR operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.-or-<paramref name="left" />.Type and
    ///     <paramref name="right" />.Type are not the same Boolean type.
    /// </exception>
    public static BinaryExpression OrElse(Expression left, Expression right)
    {
        return OrElse(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a conditional OR operation
    ///     that evaluates the second operand only if the first operand evaluates to false.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.OrElse" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the bitwise OR operator is not defined for <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type.-or-<paramref name="method" /> is null and <paramref name="left" />.Type and
    ///     <paramref name="right" />.Type are not the same Boolean type.
    /// </exception>
    public static BinaryExpression OrElse(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (method == null)
        {
            if (left.Type == right.Type)
            {
                if (left.Type == typeof(bool))
                {
                    return new LogicalBinaryExpression(ExpressionType.OrElse, left, right);
                }

                if (left.Type == typeof(bool?))
                {
                    return new SimpleBinaryExpression(ExpressionType.OrElse, left, right, left.Type);
                }
            }

            method = GetUserDefinedBinaryOperator(ExpressionType.OrElse, left.Type, right.Type, "op_BitwiseOr");
            if (!(method != null))
            {
                throw Error.BinaryOperatorNotDefined(ExpressionType.OrElse, left.Type, right.Type);
            }

            ValidateUserDefinedConditionalLogicOperator(ExpressionType.OrElse, left.Type, right.Type, method);
            var type = !left.Type.IsNullableType() || !(method.ReturnType == left.Type.GetNonNullableType())
                ? method.ReturnType
                : left.Type;
            return new MethodBinaryExpression(ExpressionType.OrElse, left, right, type, method);
        }

        ValidateUserDefinedConditionalLogicOperator(ExpressionType.OrElse, left.Type, right.Type, method);
        var type1 = !left.Type.IsNullableType() || !(method.ReturnType == left.Type.GetNonNullableType())
            ? method.ReturnType
            : left.Type;
        return new MethodBinaryExpression(ExpressionType.OrElse, left, right, type1, method);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ParameterExpression" /> node that can be used to identify a
    ///     parameter or a variable in an expression tree.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ParameterExpression" /> node with the specified name and type.</returns>
    /// <param name="type">The type of the parameter or variable.</param>
    public static ParameterExpression Parameter(Type type)
    {
        return Parameter(type, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ParameterExpression" /> node that can be used to identify a
    ///     parameter or a variable in an expression tree.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.ParameterExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Parameter" /> and the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> and
    ///     <see cref="P:System.Linq.Expressions.ParameterExpression.Name" /> properties set to the specified values.
    /// </returns>
    /// <param name="type">The type of the parameter or variable.</param>
    /// <param name="name">The name of the parameter or variable, used for debugging or printing purpose only.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="type" /> is null.
    /// </exception>
    public static ParameterExpression Parameter(Type type, string name)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        var isByRef = !(type == typeof(void)) ? type.IsByRef : throw Error.ArgumentCannotBeOfTypeVoid();
        if (isByRef)
        {
            type = type.GetElementType();
        }

        return ParameterExpression.Make(type, name, isByRef);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the assignment of the
    ///     expression followed by a subsequent decrement by 1 of the original expression.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to apply the operations on.</param>
    public static UnaryExpression PostDecrementAssign(Expression expression)
    {
        return MakeOpAssignUnary(ExpressionType.PostDecrementAssign, expression, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the assignment of the
    ///     expression followed by a subsequent decrement by 1 of the original expression.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to apply the operations on.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</param>
    public static UnaryExpression PostDecrementAssign(Expression expression, MethodInfo method)
    {
        return MakeOpAssignUnary(ExpressionType.PostDecrementAssign, expression, method);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the assignment of the
    ///     expression followed by a subsequent increment by 1 of the original expression.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to apply the operations on.</param>
    public static UnaryExpression PostIncrementAssign(Expression expression)
    {
        return MakeOpAssignUnary(ExpressionType.PostIncrementAssign, expression, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the assignment of the
    ///     expression followed by a subsequent increment by 1 of the original expression.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to apply the operations on.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</param>
    public static UnaryExpression PostIncrementAssign(Expression expression, MethodInfo method)
    {
        return MakeOpAssignUnary(ExpressionType.PostIncrementAssign, expression, method);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents raising a number to a
    ///     power.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Power" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The exponentiation operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.-or-<paramref name="left" />.Type and/or
    ///     <paramref name="right" />.Type are not <see cref="T:System.Double" />.
    /// </exception>
    public static BinaryExpression Power(Expression left, Expression right)
    {
        return Power(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents raising a number to a
    ///     power.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Power" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the exponentiation operator is not defined for <paramref name="left" />.Type
    ///     and <paramref name="right" />.Type.-or-<paramref name="method" /> is null and <paramref name="left" />.Type and/or
    ///     <paramref name="right" />.Type are not <see cref="T:System.Double" />.
    /// </exception>
    public static BinaryExpression Power(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (method == null)
        {
            method = typeof(Math).GetMethod("Pow", BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                throw Error.BinaryOperatorNotDefined(ExpressionType.Power, left.Type, right.Type);
            }
        }

        return GetMethodBasedBinaryOperator(ExpressionType.Power, left, right, method, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents raising an expression to a
    ///     power and assigning the result back to the expression.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.PowerAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression PowerAssign(Expression left, Expression right)
    {
        return PowerAssign(left, right, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents raising an expression to a
    ///     power and assigning the result back to the expression.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.PowerAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression PowerAssign(Expression left, Expression right, MethodInfo method)
    {
        return PowerAssign(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents raising an expression to a
    ///     power and assigning the result back to the expression.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.PowerAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression PowerAssign(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (method == null)
        {
            method = typeof(Math).GetMethod("Pow", BindingFlags.Static | BindingFlags.Public);
            if (method == null)
            {
                throw Error.BinaryOperatorNotDefined(ExpressionType.PowerAssign, left.Type, right.Type);
            }
        }

        return GetMethodBasedAssignOperator(ExpressionType.PowerAssign, left, right, method, conversion, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that decrements the expression by 1 and
    ///     assigns the result back to the expression.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to apply the operations on.</param>
    public static UnaryExpression PreDecrementAssign(Expression expression)
    {
        return MakeOpAssignUnary(ExpressionType.PreDecrementAssign, expression, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that decrements the expression by 1 and
    ///     assigns the result back to the expression.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to apply the operations on.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</param>
    public static UnaryExpression PreDecrementAssign(Expression expression, MethodInfo method)
    {
        return MakeOpAssignUnary(ExpressionType.PreDecrementAssign, expression, method);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that increments the expression by 1 and
    ///     assigns the result back to the expression.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to apply the operations on.</param>
    public static UnaryExpression PreIncrementAssign(Expression expression)
    {
        return MakeOpAssignUnary(ExpressionType.PreIncrementAssign, expression, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that increments the expression by 1 and
    ///     assigns the result back to the expression.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the resultant expression.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to apply the operations on.</param>
    /// <param name="method">A <see cref="T:System.Reflection.MethodInfo" /> that represents the implementing method.</param>
    public static UnaryExpression PreIncrementAssign(Expression expression, MethodInfo method)
    {
        return MakeOpAssignUnary(ExpressionType.PreIncrementAssign, expression, method);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.IndexExpression" /> representing the access to an indexed
    ///     property.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.IndexExpression" />.</returns>
    /// <param name="instance">The object to which the property belongs. If the property is static/shared, it must be null.</param>
    /// <param name="propertyName">The name of the indexer.</param>
    /// <param name="arguments">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects that are used to index
    ///     the property.
    /// </param>
    public static IndexExpression Property(
        Expression instance,
        string propertyName,
        params Expression[] arguments)
    {
        RequiresCanRead(instance, nameof(instance));
        ContractUtils.RequiresNotNull(propertyName, "indexerName");
        var instanceProperty = FindInstanceProperty(instance.Type, propertyName, arguments);
        return Property(instance, instanceProperty, arguments);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.IndexExpression" /> representing the access to an indexed
    ///     property.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.IndexExpression" />.</returns>
    /// <param name="instance">The object to which the property belongs. If the property is static/shared, it must be null.</param>
    /// <param name="indexer">The <see cref="T:System.Reflection.PropertyInfo" /> that represents the property to index.</param>
    /// <param name="arguments">
    ///     An array of <see cref="T:System.Linq.Expressions.Expression" /> objects that are used to index
    ///     the property.
    /// </param>
    public static IndexExpression Property(
        Expression instance,
        PropertyInfo indexer,
        params Expression[] arguments)
    {
        return Property(instance, indexer, (IEnumerable<Expression>)arguments);
    }

    /// <summary>
    ///     Creates an <see cref="T:System.Linq.Expressions.IndexExpression" /> representing the access to an indexed
    ///     property.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.IndexExpression" />.</returns>
    /// <param name="instance">The object to which the property belongs. If the property is static/shared, it must be null.</param>
    /// <param name="indexer">The <see cref="T:System.Reflection.PropertyInfo" /> that represents the property to index.</param>
    /// <param name="arguments">
    ///     An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects that are used to index the property.
    /// </param>
    public static IndexExpression Property(
        Expression instance,
        PropertyInfo indexer,
        IEnumerable<Expression> arguments)
    {
        var argList = arguments.ToReadOnly();
        ValidateIndexedProperty(instance, indexer, ref argList);
        return new IndexExpression(instance, indexer, argList);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a property.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" />, the
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" />
    ///     , and the <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property set to the
    ///     <see cref="T:System.Reflection.PropertyInfo" /> that represents the property denoted by
    ///     <paramref name="propertyName" />.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> whose
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> contains a property named <paramref name="propertyName" />
    ///     . This can be null for static properties.
    /// </param>
    /// <param name="propertyName">The name of a property to be accessed.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> or <paramref name="propertyName" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     No property named <paramref name="propertyName" /> is defined in
    ///     <paramref name="expression" />.Type or its base types.
    /// </exception>
    public static MemberExpression Property(Expression expression, string propertyName)
    {
        RequiresCanRead(expression, nameof(expression));
        ContractUtils.RequiresNotNull(propertyName, nameof(propertyName));
        var property = expression.Type.GetProperty(propertyName,
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        if (property == null)
        {
            property = expression.Type.GetProperty(propertyName,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.FlattenHierarchy);
        }

        return !(property == null)
            ? Property(expression, property)
            : throw Error.InstancePropertyNotDefinedForType(propertyName, expression.Type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> accessing a property.</summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.MemberExpression" />.</returns>
    /// <param name="expression">The containing object of the property. This can be null for static properties.</param>
    /// <param name="type">The <see cref="P:System.Linq.Expressions.Expression.Type" /> that contains the property.</param>
    /// <param name="propertyName">The property to be accessed.</param>
    public static MemberExpression Property(Expression expression, Type type, string propertyName)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        ContractUtils.RequiresNotNull(propertyName, nameof(propertyName));
        var property = type.GetProperty(propertyName,
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
            BindingFlags.FlattenHierarchy);
        if (property == null)
        {
            property = type.GetProperty(propertyName,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic |
                BindingFlags.FlattenHierarchy);
        }

        return !(property == null)
            ? Property(expression, property)
            : throw Error.PropertyNotDefinedForType(propertyName, type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a property.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" /> and the
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> and
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> properties set to the specified values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property equal to. This can be null for static
    ///     properties.
    /// </param>
    /// <param name="property">
    ///     The <see cref="T:System.Reflection.PropertyInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="property" /> is null.-or-The property that <paramref name="property" /> represents is not static
    ///     (Shared in Visual Basic) and <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="expression" />.Type is not assignable to the declaring type of the property that
    ///     <paramref name="property" /> represents.
    /// </exception>
    public static MemberExpression Property(Expression expression, PropertyInfo property)
    {
        ContractUtils.RequiresNotNull(property, nameof(property));
        var methodInfo1 = property.GetGetMethod(true);
        if (methodInfo1 == null)
        {
            methodInfo1 = property.GetSetMethod(true);
        }

        var methodInfo2 = methodInfo1;
        if (methodInfo2 == null)
        {
            throw Error.PropertyDoesNotHaveAccessor(property);
        }

        if (methodInfo2.IsStatic)
        {
            if (expression != null)
            {
                throw new ArgumentException(Strings.OnlyStaticPropertiesHaveNullInstance, nameof(expression));
            }
        }
        else
        {
            if (expression == null)
            {
                throw new ArgumentException(Strings.OnlyStaticPropertiesHaveNullInstance, nameof(property));
            }

            RequiresCanRead(expression, nameof(expression));
            if (!TypeUtils.IsValidInstanceType(property, expression.Type))
            {
                throw Error.PropertyNotDefinedForType(property, expression.Type);
            }
        }

        return MemberExpression.Make(expression, property);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a property by
    ///     using a property accessor method.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" />, the
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" />
    ///     and the <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property set to the
    ///     <see cref="T:System.Reflection.PropertyInfo" /> that represents the property accessed in
    ///     <paramref name="propertyAccessor" />.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property equal to. This can be null for static
    ///     properties.
    /// </param>
    /// <param name="propertyAccessor">
    ///     The <see cref="T:System.Reflection.MethodInfo" /> that represents a property accessor
    ///     method.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="propertyAccessor" /> is null.-or-The method that <paramref name="propertyAccessor" /> represents is
    ///     not static (Shared in Visual Basic) and <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="expression" />.Type is not assignable to the declaring type of the method represented by
    ///     <paramref name="propertyAccessor" />.-or-The method that <paramref name="propertyAccessor" /> represents is not a
    ///     property accessor method.
    /// </exception>
    public static MemberExpression Property(Expression expression, MethodInfo propertyAccessor)
    {
        ContractUtils.RequiresNotNull(propertyAccessor, nameof(propertyAccessor));
        ValidateMethodInfo(propertyAccessor);
        return Property(expression, GetProperty(propertyAccessor));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.MemberExpression" /> that represents accessing a property or
    ///     field.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.MemberExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.MemberAccess" />, the
    ///     <see cref="P:System.Linq.Expressions.MemberExpression.Expression" /> property set to <paramref name="expression" />
    ///     , and the <see cref="P:System.Linq.Expressions.MemberExpression.Member" /> property set to the
    ///     <see cref="T:System.Reflection.PropertyInfo" /> or <see cref="T:System.Reflection.FieldInfo" /> that represents the
    ///     property or field denoted by <paramref name="propertyOrFieldName" />.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> whose
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> contains a property or field named
    ///     <paramref name="propertyOrFieldName" />. This can be null for static members.
    /// </param>
    /// <param name="propertyOrFieldName">The name of a property or field to be accessed.</param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> or <paramref name="propertyOrFieldName" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     No property or field named <paramref name="propertyOrFieldName" /> is
    ///     defined in <paramref name="expression" />.Type or its base types.
    /// </exception>
    public static MemberExpression PropertyOrField(Expression expression, string propertyOrFieldName)
    {
        RequiresCanRead(expression, nameof(expression));
        var property1 = expression.Type.GetProperty(propertyOrFieldName,
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        if (property1 != null)
        {
            return Property(expression, property1);
        }

        var field1 = expression.Type.GetField(propertyOrFieldName,
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        if (field1 != null)
        {
            return Field(expression, field1);
        }

        var property2 = expression.Type.GetProperty(propertyOrFieldName,
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        if (property2 != null)
        {
            return Property(expression, property2);
        }

        var field2 = expression.Type.GetField(propertyOrFieldName,
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        return field2 != null
            ? Field(expression, field2)
            : throw Error.NotAMemberOfType(propertyOrFieldName, expression.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an expression that has a
    ///     constant value of type <see cref="T:System.Linq.Expressions.Expression" />.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Quote" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> is null.
    /// </exception>
    public static UnaryExpression Quote(Expression expression)
    {
        RequiresCanRead(expression, nameof(expression));
        return expression is LambdaExpression
            ? new UnaryExpression(ExpressionType.Quote, expression, expression.GetType(), null)
            : throw Error.QuotedExpressionMustBeLambda();
    }

    /// <summary>
    ///     Reduces this node to a simpler expression. If CanReduce returns true, this should return a valid expression.
    ///     This method can return another node which itself must be reduced.
    /// </summary>
    /// <returns>The reduced expression.</returns>
    public virtual Expression Reduce()
    {
        if (CanReduce)
        {
            throw Error.ReducibleMustOverrideReduce();
        }

        return this;
    }

    /// <summary>
    ///     Reduces this node to a simpler expression. If CanReduce returns true, this should return a valid expression.
    ///     This method can return another node which itself must be reduced.
    /// </summary>
    /// <returns>The reduced expression.</returns>
    public Expression ReduceAndCheck()
    {
        if (!CanReduce)
        {
            throw Error.MustBeReducible();
        }

        var expression = Reduce();
        if (expression == null || expression == this)
        {
            throw Error.MustReduceToDifferent();
        }

        return TypeUtils.AreReferenceAssignable(Type, expression.Type)
            ? expression
            : throw Error.ReducedNotCompatible();
    }

    /// <summary>
    ///     Reduces the expression to a known node type (that is not an Extension node) or just returns the expression if
    ///     it is already a known type.
    /// </summary>
    /// <returns>The reduced expression.</returns>
    public Expression ReduceExtensions()
    {
        var expression = this;
        while (expression.NodeType == ExpressionType.Extension)
        {
            expression = expression.ReduceAndCheck();
        }

        return expression;
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a reference equality
    ///     comparison.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Equal" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression ReferenceEqual(Expression left, Expression right)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (TypeUtils.HasReferenceEquality(left.Type, right.Type))
        {
            return new LogicalBinaryExpression(ExpressionType.Equal, left, right);
        }

        throw Error.ReferenceEqualityNotDefined(left.Type, right.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a reference inequality
    ///     comparison.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.NotEqual" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression ReferenceNotEqual(Expression left, Expression right)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (TypeUtils.HasReferenceEquality(left.Type, right.Type))
        {
            return new LogicalBinaryExpression(ExpressionType.NotEqual, left, right);
        }

        throw Error.ReferenceEqualityNotDefined(left.Type, right.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a rethrowing of an
    ///     exception.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a rethrowing of an exception.</returns>
    public static UnaryExpression Rethrow()
    {
        return Throw(null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a rethrowing of an
    ///     exception with a given type.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a rethrowing of an exception.</returns>
    /// <param name="type">The new <see cref="T:System.Type" /> of the expression.</param>
    public static UnaryExpression Rethrow(Type type)
    {
        return Throw(null, type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a return statement.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Return, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and a
    ///     null value to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    public static GotoExpression Return(LabelTarget target)
    {
        return MakeGoto(GotoExpressionKind.Return, target, null, typeof(void));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a return statement with the
    ///     specified type.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Return, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and a null value
    ///     to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    /// <param name="type">
    ///     An <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    public static GotoExpression Return(LabelTarget target, Type type)
    {
        return MakeGoto(GotoExpressionKind.Return, target, null, type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a return statement. The value
    ///     passed to the label upon jumping can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Continue, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and
    ///     <paramref name="value" /> to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
    public static GotoExpression Return(LabelTarget target, Expression value)
    {
        return MakeGoto(GotoExpressionKind.Return, target, value, typeof(void));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.GotoExpression" /> representing a return statement with the
    ///     specified type. The value passed to the label upon jumping can be specified.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.GotoExpression" /> with
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Kind" /> equal to Continue, the
    ///     <see cref="P:System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and
    ///     <paramref name="value" /> to be passed to the target label upon jumping.
    /// </returns>
    /// <param name="target">
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> that the
    ///     <see cref="T:System.Linq.Expressions.GotoExpression" /> will jump to.
    /// </param>
    /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
    /// <param name="type">
    ///     An <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    public static GotoExpression Return(LabelTarget target, Expression value, Type type)
    {
        return MakeGoto(GotoExpressionKind.Return, target, value, type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.RightShift" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The right-shift operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression RightShift(Expression left, Expression right)
    {
        return RightShift(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift
    ///     operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.RightShift" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the right-shift operator is not defined for <paramref name="left" />.Type
    ///     and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression RightShift(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.RightShift, left, right, method, true);
        }

        if (!IsSimpleShift(left.Type, right.Type))
        {
            return GetUserDefinedBinaryOperatorOrThrow(ExpressionType.RightShift, "op_RightShift", left, right, true);
        }

        var resultTypeOfShift = GetResultTypeOfShift(left.Type, right.Type);
        return new SimpleBinaryExpression(ExpressionType.RightShift, left, right, resultTypeOfShift);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift
    ///     assignment operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.RightShiftAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression RightShiftAssign(Expression left, Expression right)
    {
        return RightShiftAssign(left, right, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift
    ///     assignment operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.RightShiftAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression RightShiftAssign(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        return RightShiftAssign(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a bitwise right-shift
    ///     assignment operation.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.RightShiftAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression RightShiftAssign(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.RightShiftAssign, left, right, method, conversion, true);
        }

        if (!IsSimpleShift(left.Type, right.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.RightShiftAssign, "op_RightShift", left, right,
                conversion, true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        var resultTypeOfShift = GetResultTypeOfShift(left.Type, right.Type);
        return new SimpleBinaryExpression(ExpressionType.RightShiftAssign, left, right, resultTypeOfShift);
    }

    /// <summary>Creates an instance of <see cref="T:System.Linq.Expressions.RuntimeVariablesExpression" />.</summary>
    /// <returns>
    ///     An instance of <see cref="T:System.Linq.Expressions.RuntimeVariablesExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.RuntimeVariables" /> and the
    ///     <see cref="P:System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> property set to the specified value.
    /// </returns>
    /// <param name="variables">
    ///     An array of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to
    ///     populate the <see cref="P:System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> collection.
    /// </param>
    public static RuntimeVariablesExpression RuntimeVariables(params ParameterExpression[] variables)
    {
        return RuntimeVariables((IEnumerable<ParameterExpression>)variables);
    }

    /// <summary>Creates an instance of <see cref="T:System.Linq.Expressions.RuntimeVariablesExpression" />.</summary>
    /// <returns>
    ///     An instance of <see cref="T:System.Linq.Expressions.RuntimeVariablesExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.RuntimeVariables" /> and the
    ///     <see cref="P:System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> property set to the specified value.
    /// </returns>
    /// <param name="variables">
    ///     A collection of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to
    ///     populate the <see cref="P:System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> collection.
    /// </param>
    public static RuntimeVariablesExpression RuntimeVariables(
        IEnumerable<ParameterExpression> variables)
    {
        ContractUtils.RequiresNotNull(variables, nameof(variables));
        var variables1 = variables.ToReadOnly();
        for (var index = 0; index < variables1.Count; ++index)
        {
            if (variables1[index] == null)
            {
                throw new ArgumentNullException($"variables[{index.ToString()}]");
            }
        }

        return new RuntimeVariablesExpression(variables1);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Subtract" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The subtraction operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Subtract(Expression left, Expression right)
    {
        return Subtract(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Subtract" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the subtraction operator is not defined for <paramref name="left" />.Type
    ///     and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression Subtract(Expression left, Expression right, MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.Subtract, left, right, method, true);
        }

        return left.Type == right.Type && TypeUtils.IsArithmetic(left.Type)
            ? new SimpleBinaryExpression(ExpressionType.Subtract, left, right, left.Type)
            : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.Subtract, "op_Subtraction", left, right, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.SubtractAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression SubtractAssign(Expression left, Expression right)
    {
        return SubtractAssign(left, right, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.SubtractAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression SubtractAssign(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        return SubtractAssign(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment
    ///     operation that does not have overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.SubtractAssign" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression SubtractAssign(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.SubtractAssign, left, right, method, conversion, true);
        }

        if (!(left.Type == right.Type) || !TypeUtils.IsArithmetic(left.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.SubtractAssign, "op_Subtraction", left, right,
                conversion, true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        return new SimpleBinaryExpression(ExpressionType.SubtractAssign, left, right, left.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment
    ///     operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.SubtractAssignChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    public static BinaryExpression SubtractAssignChecked(Expression left, Expression right)
    {
        return SubtractAssignChecked(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment
    ///     operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.SubtractAssignChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    public static BinaryExpression SubtractAssignChecked(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        return SubtractAssignChecked(left, right, method, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents a subtraction assignment
    ///     operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.SubtractAssignChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <param name="conversion">
    ///     A <see cref="T:System.Linq.Expressions.LambdaExpression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Conversion" /> property equal to.
    /// </param>
    public static BinaryExpression SubtractAssignChecked(
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanWrite(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedAssignOperator(ExpressionType.SubtractAssignChecked, left, right, method, conversion,
                true);
        }

        if (!(left.Type == right.Type) || !TypeUtils.IsArithmetic(left.Type))
        {
            return GetUserDefinedAssignOperatorOrThrow(ExpressionType.SubtractAssignChecked, "op_Subtraction", left,
                right, conversion, true);
        }

        if (conversion != null)
        {
            throw Error.ConversionIsNotSupportedForArithmeticTypes();
        }

        return new SimpleBinaryExpression(ExpressionType.SubtractAssignChecked, left, right, left.Type);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction
    ///     operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.SubtractChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The subtraction operator is not defined for
    ///     <paramref name="left" />.Type and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression SubtractChecked(Expression left, Expression right)
    {
        return SubtractChecked(left, right, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.BinaryExpression" /> that represents an arithmetic subtraction
    ///     operation that has overflow checking.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.BinaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.SubtractChecked" /> and the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" />,
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" />, and
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="left">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Left" /> property equal to.
    /// </param>
    /// <param name="right">
    ///     A <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Right" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.BinaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="left" /> or <paramref name="right" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly two arguments.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the subtraction operator is not defined for <paramref name="left" />.Type
    ///     and <paramref name="right" />.Type.
    /// </exception>
    public static BinaryExpression SubtractChecked(
        Expression left,
        Expression right,
        MethodInfo method)
    {
        RequiresCanRead(left, nameof(left));
        RequiresCanRead(right, nameof(right));
        if (!(method == null))
        {
            return GetMethodBasedBinaryOperator(ExpressionType.SubtractChecked, left, right, method, true);
        }

        return left.Type == right.Type && TypeUtils.IsArithmetic(left.Type)
            ? new SimpleBinaryExpression(ExpressionType.SubtractChecked, left, right, left.Type)
            : GetUserDefinedBinaryOperatorOrThrow(ExpressionType.SubtractChecked, "op_Subtraction", left, right, true);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.SwitchExpression" /> that represents a switch statement without
    ///     a default case.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.SwitchExpression" />.</returns>
    /// <param name="switchValue">The value to be tested against each case.</param>
    /// <param name="cases">The set of cases for this switch expression.</param>
    public static SwitchExpression Switch(Expression switchValue, params SwitchCase[] cases)
    {
        return Switch(switchValue, null, null, (IEnumerable<SwitchCase>)cases);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.SwitchExpression" /> that represents a switch statement that
    ///     has a default case.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.SwitchExpression" />.</returns>
    /// <param name="switchValue">The value to be tested against each case.</param>
    /// <param name="defaultBody">The result of the switch if <paramref name="switchValue" /> does not match any of the cases.</param>
    /// <param name="cases">The set of cases for this switch expression.</param>
    public static SwitchExpression Switch(
        Expression switchValue,
        Expression defaultBody,
        params SwitchCase[] cases)
    {
        return Switch(switchValue, defaultBody, null, (IEnumerable<SwitchCase>)cases);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.SwitchExpression" /> that represents a switch statement that
    ///     has a default case.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.SwitchExpression" />.</returns>
    /// <param name="switchValue">The value to be tested against each case.</param>
    /// <param name="defaultBody">The result of the switch if <paramref name="switchValue" /> does not match any of the cases.</param>
    /// <param name="comparison">The equality comparison method to use.</param>
    /// <param name="cases">The set of cases for this switch expression.</param>
    public static SwitchExpression Switch(
        Expression switchValue,
        Expression defaultBody,
        MethodInfo comparison,
        params SwitchCase[] cases)
    {
        return Switch(switchValue, defaultBody, comparison, (IEnumerable<SwitchCase>)cases);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.SwitchExpression" /> that represents a switch statement that
    ///     has a default case..
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.SwitchExpression" />.</returns>
    /// <param name="type">The result type of the switch.</param>
    /// <param name="switchValue">The value to be tested against each case.</param>
    /// <param name="defaultBody">The result of the switch if <paramref name="switchValue" /> does not match any of the cases.</param>
    /// <param name="comparison">The equality comparison method to use.</param>
    /// <param name="cases">The set of cases for this switch expression.</param>
    public static SwitchExpression Switch(
        Type type,
        Expression switchValue,
        Expression defaultBody,
        MethodInfo comparison,
        params SwitchCase[] cases)
    {
        return Switch(type, switchValue, defaultBody, comparison, (IEnumerable<SwitchCase>)cases);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.SwitchExpression" /> that represents a switch statement that
    ///     has a default case.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.SwitchExpression" />.</returns>
    /// <param name="switchValue">The value to be tested against each case.</param>
    /// <param name="defaultBody">The result of the switch if <paramref name="switchValue" /> does not match any of the cases.</param>
    /// <param name="comparison">The equality comparison method to use.</param>
    /// <param name="cases">The set of cases for this switch expression.</param>
    public static SwitchExpression Switch(
        Expression switchValue,
        Expression defaultBody,
        MethodInfo comparison,
        IEnumerable<SwitchCase> cases)
    {
        return Switch(null, switchValue, defaultBody, comparison, cases);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.SwitchExpression" /> that represents a switch statement that
    ///     has a default case.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.SwitchExpression" />.</returns>
    /// <param name="type">The result type of the switch.</param>
    /// <param name="switchValue">The value to be tested against each case.</param>
    /// <param name="defaultBody">The result of the switch if <paramref name="switchValue" /> does not match any of the cases.</param>
    /// <param name="comparison">The equality comparison method to use.</param>
    /// <param name="cases">The set of cases for this switch expression.</param>
    public static SwitchExpression Switch(
        Type type,
        Expression switchValue,
        Expression defaultBody,
        MethodInfo comparison,
        IEnumerable<SwitchCase> cases)
    {
        RequiresCanRead(switchValue, nameof(switchValue));
        if (switchValue.Type == typeof(void))
        {
            throw Error.ArgumentCannotBeOfTypeVoid();
        }

        var readOnlyCollection = cases.ToReadOnly();
        ContractUtils.RequiresNotEmpty(readOnlyCollection, nameof(cases));
        ContractUtils.RequiresNotNullItems(readOnlyCollection, nameof(cases));
        var type1 = type;
        if (type1 == null)
        {
            type1 = readOnlyCollection[0].Body.Type;
        }

        var type2 = type1;
        var customType = type != null;
        if (comparison != null)
        {
            var parametersCached = comparison.GetParameters();
            var pi1 = parametersCached.Length == 2
                ? parametersCached[0]
                : throw Error.IncorrectNumberOfMethodCallArguments(comparison);
            var flag = false;
            if (!ParameterIsAssignable(pi1, switchValue.Type))
            {
                flag = ParameterIsAssignable(pi1, switchValue.Type.GetNonNullableType());
                if (!flag)
                {
                    throw Error.SwitchValueTypeDoesNotMatchComparisonMethodParameter(switchValue.Type,
                        pi1.ParameterType);
                }
            }

            var pi2 = parametersCached[1];
            foreach (var switchCase in readOnlyCollection)
            {
                ContractUtils.RequiresNotNull(switchCase, nameof(cases));
                ValidateSwitchCaseType(switchCase.Body, customType, type2, nameof(cases));
                for (var index = 0; index < switchCase.TestValues.Count; ++index)
                {
                    var type3 = switchCase.TestValues[index].Type;
                    if (flag)
                    {
                        type3 = type3.IsNullableType()
                            ? type3.GetNonNullableType()
                            : throw Error.TestValueTypeDoesNotMatchComparisonMethodParameter(type3, pi2.ParameterType);
                    }

                    if (!ParameterIsAssignable(pi2, type3))
                    {
                        throw Error.TestValueTypeDoesNotMatchComparisonMethodParameter(type3, pi2.ParameterType);
                    }
                }
            }
        }
        else
        {
            var testValue = readOnlyCollection[0].TestValues[0];
            foreach (var switchCase in readOnlyCollection)
            {
                ContractUtils.RequiresNotNull(switchCase, nameof(cases));
                ValidateSwitchCaseType(switchCase.Body, customType, type2, nameof(cases));
                for (var index = 0; index < switchCase.TestValues.Count; ++index)
                {
                    if (!TypeUtils.AreEquivalent(testValue.Type, switchCase.TestValues[index].Type))
                    {
                        throw new ArgumentException(Strings.AllTestValuesMustHaveSameType, nameof(cases));
                    }
                }
            }

            comparison = Equal(switchValue, testValue, false, comparison).Method;
        }

        if (defaultBody == null)
        {
            if (type2 != typeof(void))
            {
                throw Error.DefaultBodyMustBeSupplied();
            }
        }
        else
        {
            ValidateSwitchCaseType(defaultBody, customType, type2, nameof(defaultBody));
        }

        if (comparison != null && comparison.ReturnType != typeof(bool))
        {
            throw Error.EqualityMustReturnBoolean(comparison);
        }

        return new SwitchExpression(type2, switchValue, defaultBody, comparison, readOnlyCollection);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.SwitchCase" /> for use in a
    ///     <see cref="T:System.Linq.Expressions.SwitchExpression" />.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.SwitchCase" />.</returns>
    /// <param name="body">The body of the case.</param>
    /// <param name="testValues">The test values of the case.</param>
    public static SwitchCase SwitchCase(
        Expression body,
        params Expression[] testValues)
    {
        return SwitchCase(body, (IEnumerable<Expression>)testValues);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.SwitchCase" /> object to be used in a
    ///     <see cref="T:System.Linq.Expressions.SwitchExpression" /> object.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.SwitchCase" />.</returns>
    /// <param name="body">The body of the case.</param>
    /// <param name="testValues">The test values of the case.</param>
    public static SwitchCase SwitchCase(
        Expression body,
        IEnumerable<Expression> testValues)
    {
        RequiresCanRead(body, nameof(body));
        var readOnlyCollection = testValues.ToReadOnly();
        RequiresCanRead(readOnlyCollection, nameof(testValues));
        ContractUtils.RequiresNotEmpty(readOnlyCollection, nameof(testValues));
        return new SwitchCase(body, readOnlyCollection);
    }

    /// <summary>Creates an instance of <see cref="T:System.Linq.Expressions.SymbolDocumentInfo" />.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.SymbolDocumentInfo" /> that has the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.FileName" /> property set to the specified value.
    /// </returns>
    /// <param name="fileName">
    ///     A <see cref="T:System.String" /> to set the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.FileName" /> equal to.
    /// </param>
    public static SymbolDocumentInfo SymbolDocument(string fileName)
    {
        return new SymbolDocumentInfo(fileName);
    }

    /// <summary>Creates an instance of <see cref="T:System.Linq.Expressions.SymbolDocumentInfo" />.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.SymbolDocumentInfo" /> that has the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.FileName" /> and
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.Language" /> properties set to the specified value.
    /// </returns>
    /// <param name="fileName">
    ///     A <see cref="T:System.String" /> to set the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.FileName" /> equal to.
    /// </param>
    /// <param name="language">
    ///     A <see cref="T:System.Guid" /> to set the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.Language" /> equal to.
    /// </param>
    public static SymbolDocumentInfo SymbolDocument(string fileName, Guid language)
    {
        return new SymbolDocumentWithGuids(fileName, ref language);
    }

    /// <summary>Creates an instance of <see cref="T:System.Linq.Expressions.SymbolDocumentInfo" />.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.SymbolDocumentInfo" /> that has the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.FileName" /> and
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.Language" /> and
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.LanguageVendor" /> properties set to the specified value.
    /// </returns>
    /// <param name="fileName">
    ///     A <see cref="T:System.String" /> to set the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.FileName" /> equal to.
    /// </param>
    /// <param name="language">
    ///     A <see cref="T:System.Guid" /> to set the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.Language" /> equal to.
    /// </param>
    /// <param name="languageVendor">
    ///     A <see cref="T:System.Guid" /> to set the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.LanguageVendor" /> equal to.
    /// </param>
    public static SymbolDocumentInfo SymbolDocument(
        string fileName,
        Guid language,
        Guid languageVendor)
    {
        return new SymbolDocumentWithGuids(fileName, ref language, ref languageVendor);
    }

    /// <summary>Creates an instance of <see cref="T:System.Linq.Expressions.SymbolDocumentInfo" />.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.SymbolDocumentInfo" /> that has the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.FileName" /> and
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.Language" /> and
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.LanguageVendor" /> and
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.DocumentType" /> properties set to the specified value.
    /// </returns>
    /// <param name="fileName">
    ///     A <see cref="T:System.String" /> to set the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.FileName" /> equal to.
    /// </param>
    /// <param name="language">
    ///     A <see cref="T:System.Guid" /> to set the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.Language" /> equal to.
    /// </param>
    /// <param name="languageVendor">
    ///     A <see cref="T:System.Guid" /> to set the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.LanguageVendor" /> equal to.
    /// </param>
    /// <param name="documentType">
    ///     A <see cref="T:System.Guid" /> to set the
    ///     <see cref="P:System.Linq.Expressions.SymbolDocumentInfo.DocumentType" /> equal to.
    /// </param>
    public static SymbolDocumentInfo SymbolDocument(
        string fileName,
        Guid language,
        Guid languageVendor,
        Guid documentType)
    {
        return new SymbolDocumentWithGuids(fileName, ref language, ref languageVendor, ref documentType);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a throwing of an exception.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the exception.</returns>
    /// <param name="value">An <see cref="T:System.Linq.Expressions.Expression" />.</param>
    public static UnaryExpression Throw(Expression value)
    {
        return Throw(value, typeof(void));
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a throwing of an exception
    ///     with a given type.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents the exception.</returns>
    /// <param name="value">An <see cref="T:System.Linq.Expressions.Expression" />.</param>
    /// <param name="type">The new <see cref="T:System.Type" /> of the expression.</param>
    public static UnaryExpression Throw(Expression value, Type type)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        TypeUtils.ValidateType(type);
        if (value != null)
        {
            RequiresCanRead(value, nameof(value));
            if (value.Type.IsValueType)
            {
                throw Error.ArgumentMustNotHaveValueType();
            }
        }

        return new UnaryExpression(ExpressionType.Throw, value, type, null);
    }

    /// <summary>Returns a textual representation of the <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>A textual representation of the <see cref="T:System.Linq.Expressions.Expression" />.</returns>
    public override string ToString()
    {
        return ExpressionStringBuilder.ExpressionToString(this);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.TryExpression" /> representing a try block with any number of
    ///     catch statements and neither a fault nor finally block.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.TryExpression" />.</returns>
    /// <param name="body">The body of the try block.</param>
    /// <param name="handlers">
    ///     The array of zero or more <see cref="T:System.Linq.Expressions.CatchBlock" /> expressions
    ///     representing the catch statements to be associated with the try block.
    /// </param>
    public static TryExpression TryCatch(Expression body, params CatchBlock[] handlers)
    {
        return MakeTry(null, body, null, null, handlers);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.TryExpression" /> representing a try block with any number of
    ///     catch statements and a finally block.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.TryExpression" />.</returns>
    /// <param name="body">The body of the try block.</param>
    /// <param name="finally">The body of the finally block.</param>
    /// <param name="handlers">
    ///     The array of zero or more <see cref="T:System.Linq.Expressions.CatchBlock" /> expressions
    ///     representing the catch statements to be associated with the try block.
    /// </param>
    public static TryExpression TryCatchFinally(
        Expression body,
        Expression @finally,
        params CatchBlock[] handlers)
    {
        return MakeTry(null, body, @finally, null, handlers);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.TryExpression" /> representing a try block with a fault block
    ///     and no catch statements.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.TryExpression" />.</returns>
    /// <param name="body">The body of the try block.</param>
    /// <param name="fault">The body of the fault block.</param>
    public static TryExpression TryFault(Expression body, Expression fault)
    {
        return MakeTry(null, body, null, fault, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.TryExpression" /> representing a try block with a finally block
    ///     and no catch statements.
    /// </summary>
    /// <returns>The created <see cref="T:System.Linq.Expressions.TryExpression" />.</returns>
    /// <param name="body">The body of the try block.</param>
    /// <param name="finally">The body of the finally block.</param>
    public static TryExpression TryFinally(Expression body, Expression @finally)
    {
        return MakeTry(null, body, @finally, null, null);
    }

    /// <summary>
    ///     Creates a <see cref="P:System.Linq.Expressions.Expression.Type" /> object that represents a generic
    ///     System.Action delegate type that has specific type arguments.
    /// </summary>
    /// <returns>
    ///     true if generic System.Action delegate type was created for specific <paramref name="typeArgs" />; false
    ///     otherwise.
    /// </returns>
    /// <param name="typeArgs">An array of Type objects that specify the type arguments for the System.Action delegate type.</param>
    /// <param name="actionType">
    ///     When this method returns, contains the generic System.Action delegate type that has specific
    ///     type arguments. Contains null if there is no generic System.Action delegate that matches the
    ///     <paramref name="typeArgs" />.This parameter is passed uninitialized.
    /// </param>
    public static bool TryGetActionType(Type[] typeArgs, out Type actionType)
    {
        if (ValidateTryGetFuncActionArgs(typeArgs))
        {
            return (actionType = DelegateHelpers.GetActionType(typeArgs)) != null;
        }

        actionType = null;
        return false;
    }

    /// <summary>
    ///     Creates a <see cref="P:System.Linq.Expressions.Expression.Type" /> object that represents a generic
    ///     System.Func delegate type that has specific type arguments. The last type argument specifies the return type of the
    ///     created delegate.
    /// </summary>
    /// <returns>
    ///     true if generic System.Func delegate type was created for specific <paramref name="typeArgs" />; false
    ///     otherwise.
    /// </returns>
    /// <param name="typeArgs">An array of Type objects that specify the type arguments for the System.Func delegate type.</param>
    /// <param name="funcType">
    ///     When this method returns, contains the generic System.Func delegate type that has specific type
    ///     arguments. Contains null if there is no generic System.Func delegate that matches the <paramref name="typeArgs" />
    ///     .This parameter is passed uninitialized.
    /// </param>
    public static bool TryGetFuncType(Type[] typeArgs, out Type funcType)
    {
        if (ValidateTryGetFuncActionArgs(typeArgs))
        {
            return (funcType = DelegateHelpers.GetFuncType(typeArgs)) != null;
        }

        funcType = null;
        return false;
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an explicit reference or
    ///     boxing conversion where null is supplied if the conversion fails.
    /// </summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.TypeAs" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and
    ///     <see cref="P:System.Linq.Expressions.Expression.Type" /> properties set to the specified values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <param name="type">
    ///     A <see cref="T:System.Type" /> to set the <see cref="P:System.Linq.Expressions.Expression.Type" />
    ///     property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> or <paramref name="type" /> is null.
    /// </exception>
    public static UnaryExpression TypeAs(Expression expression, Type type)
    {
        RequiresCanRead(expression, nameof(expression));
        ContractUtils.RequiresNotNull(type, nameof(type));
        TypeUtils.ValidateType(type);
        return !type.IsValueType || type.IsNullableType()
            ? new UnaryExpression(ExpressionType.TypeAs, expression, type, null)
            : throw Error.IncorrectTypeForTypeAs(type);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.TypeBinaryExpression" /> that compares run-time type identity.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.TypeBinaryExpression" /> for which the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property is equal to
    ///     <see cref="M:System.Linq.Expressions.Expression.TypeEqual(System.Linq.Expressions.Expression,System.Type)" /> and
    ///     for which the <see cref="T:System.Linq.Expressions.Expression" /> and
    ///     <see cref="P:System.Linq.Expressions.TypeBinaryExpression.TypeOperand" /> properties are set to the specified
    ///     values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="T:System.Linq.Expressions.Expression" /> property equal to.
    /// </param>
    /// <param name="type">
    ///     A <see cref="P:System.Linq.Expressions.Expression.Type" /> to set the
    ///     <see cref="P:System.Linq.Expressions.TypeBinaryExpression.TypeOperand" /> property equal to.
    /// </param>
    public static TypeBinaryExpression TypeEqual(Expression expression, Type type)
    {
        RequiresCanRead(expression, nameof(expression));
        ContractUtils.RequiresNotNull(type, nameof(type));
        return !type.IsByRef
            ? new TypeBinaryExpression(expression, type, ExpressionType.TypeEqual)
            : throw Error.TypeMustNotBeByRef();
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.TypeBinaryExpression" />.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.TypeBinaryExpression" /> for which the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property is equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.TypeIs" /> and for which the
    ///     <see cref="P:System.Linq.Expressions.TypeBinaryExpression.Expression" /> and
    ///     <see cref="P:System.Linq.Expressions.TypeBinaryExpression.TypeOperand" /> properties are set to the specified
    ///     values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.TypeBinaryExpression.Expression" /> property equal to.
    /// </param>
    /// <param name="type">
    ///     A <see cref="P:System.Linq.Expressions.Expression.Type" /> to set the
    ///     <see cref="P:System.Linq.Expressions.TypeBinaryExpression.TypeOperand" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> or <paramref name="type" /> is null.
    /// </exception>
    public static TypeBinaryExpression TypeIs(Expression expression, Type type)
    {
        RequiresCanRead(expression, nameof(expression));
        ContractUtils.RequiresNotNull(type, nameof(type));
        return !type.IsByRef
            ? new TypeBinaryExpression(expression, type, ExpressionType.TypeIs)
            : throw Error.TypeMustNotBeByRef();
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a unary plus operation.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.UnaryPlus" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property set to the specified value.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     The unary plus operator is not defined for
    ///     <paramref name="expression" />.Type.
    /// </exception>
    public static UnaryExpression UnaryPlus(Expression expression)
    {
        return UnaryPlus(expression, null);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents a unary plus operation.</summary>
    /// <returns>
    ///     A <see cref="T:System.Linq.Expressions.UnaryExpression" /> that has the
    ///     <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.UnaryPlus" /> and the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> and
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> properties set to the specified values.
    /// </returns>
    /// <param name="expression">
    ///     An <see cref="T:System.Linq.Expressions.Expression" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Operand" /> property equal to.
    /// </param>
    /// <param name="method">
    ///     A <see cref="T:System.Reflection.MethodInfo" /> to set the
    ///     <see cref="P:System.Linq.Expressions.UnaryExpression.Method" /> property equal to.
    /// </param>
    /// <exception cref="T:System.ArgumentNullException">
    ///     <paramref name="expression" /> is null.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    ///     <paramref name="method" /> is not null and the method it represents returns void, is not static (Shared in Visual
    ///     Basic), or does not take exactly one argument.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    ///     <paramref name="method" /> is null and the unary plus operator is not defined for <paramref name="expression" />
    ///     .Type.-or-<paramref name="expression" />.Type (or its corresponding non-nullable type if it is a nullable value
    ///     type) is not assignable to the argument type of the method represented by <paramref name="method" />.
    /// </exception>
    public static UnaryExpression UnaryPlus(Expression expression, MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        if (!(method == null))
        {
            return GetMethodBasedUnaryOperator(ExpressionType.UnaryPlus, expression, method);
        }

        return TypeUtils.IsArithmetic(expression.Type)
            ? new UnaryExpression(ExpressionType.UnaryPlus, expression, expression.Type, null)
            : GetUserDefinedUnaryOperatorOrThrow(ExpressionType.UnaryPlus, "op_UnaryPlus", expression);
    }

    /// <summary>Creates a <see cref="T:System.Linq.Expressions.UnaryExpression" /> that represents an explicit unboxing.</summary>
    /// <returns>An instance of <see cref="T:System.Linq.Expressions.UnaryExpression" />.</returns>
    /// <param name="expression">An <see cref="T:System.Linq.Expressions.Expression" /> to unbox.</param>
    /// <param name="type">The new <see cref="T:System.Type" /> of the expression.</param>
    public static UnaryExpression Unbox(Expression expression, Type type)
    {
        RequiresCanRead(expression, nameof(expression));
        ContractUtils.RequiresNotNull(type, nameof(type));
        if (!expression.Type.IsInterface && expression.Type != typeof(object))
        {
            throw Error.InvalidUnboxType();
        }

        if (!type.IsValueType)
        {
            throw Error.InvalidUnboxType();
        }

        TypeUtils.ValidateType(type);
        return new UnaryExpression(ExpressionType.Unbox, expression, type, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ParameterExpression" /> node that can be used to identify a
    ///     parameter or a variable in an expression tree.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ParameterExpression" /> node with the specified name and type</returns>
    /// <param name="type">The type of the parameter or variable.</param>
    public static ParameterExpression Variable(Type type)
    {
        return Variable(type, null);
    }

    /// <summary>
    ///     Creates a <see cref="T:System.Linq.Expressions.ParameterExpression" /> node that can be used to identify a
    ///     parameter or a variable in an expression tree.
    /// </summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.ParameterExpression" /> node with the specified name and type.</returns>
    /// <param name="type">The type of the parameter or variable.</param>
    /// <param name="name">The name of the parameter or variable. This name is used for debugging or printing purpose only.</param>
    public static ParameterExpression Variable(Type type, string name)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        if (type == typeof(void))
        {
            throw Error.ArgumentCannotBeOfTypeVoid();
        }

        return !type.IsByRef ? ParameterExpression.Make(type, name, false) : throw Error.TypeMustNotBeByRef();
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
    protected internal virtual Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitExtension(this);
    }

    /// <summary>
    ///     Reduces the node and then calls the visitor delegate on the reduced expression. The method throws an exception
    ///     if the node is not reducible.
    /// </summary>
    /// <returns>The expression being visited, or an expression which should replace it in the tree.</returns>
    /// <param name="visitor">An instance of <see cref="T:System.Func`2" />.</param>
    protected internal virtual Expression VisitChildren(ExpressionVisitor visitor)
    {
        if (!CanReduce)
        {
            throw Error.MustBeReducible();
        }

        return visitor.Visit(ReduceAndCheck());
    }

    internal static LambdaExpression CreateLambda(
        Type delegateType,
        Expression body,
        string name,
        bool tailCall,
        ReadOnlyCollection<ParameterExpression> parameters)
    {
        var cacheDict = _LambdaFactories;
        if (cacheDict == null)
        {
            _LambdaFactories = cacheDict = new CacheDict<Type, LambdaFactory>(50);
        }

        var method = (MethodInfo)null;
        LambdaFactory lambdaFactory;
        if (!cacheDict.TryGetValue(delegateType, out lambdaFactory))
        {
            method = typeof(Expression<>).MakeGenericType(delegateType)
                .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic);
            if (delegateType.CanCache())
            {
                cacheDict[delegateType] =
                    lambdaFactory = (LambdaFactory)Delegate.CreateDelegate(typeof(LambdaFactory), method);
            }
        }

        if (lambdaFactory != null)
        {
            return lambdaFactory(body, name, tailCall, parameters);
        }

        return (LambdaExpression)method.Invoke(null, new object[4]
        {
            body,
            name,
            tailCall,
            parameters
        });
    }

    internal static MethodInfo GetInvokeMethod(Expression expression)
    {
        var type = expression.Type;
        if (!expression.Type.IsSubclassOf(typeof(MulticastDelegate)))
        {
            var genericType = TypeUtils.FindGenericType(typeof(Expression<>), expression.Type);
            type = !(genericType == null)
                ? genericType.GetGenericArguments()[0]
                : throw Error.ExpressionTypeNotInvocable(expression.Type);
        }

        return type.GetMethod("Invoke");
    }

    internal static bool ParameterIsAssignable(ParameterInfo pi, Type argType)
    {
        var dest = pi.ParameterType;
        if (dest.IsByRef)
        {
            dest = dest.GetElementType();
        }

        return TypeUtils.AreReferenceAssignable(dest, argType);
    }

    internal static T ReturnObject<T>(object collectionOrT) where T : class
    {
        return collectionOrT is T obj ? obj : ((ReadOnlyCollection<T>)collectionOrT)[0];
    }

    internal static ReadOnlyCollection<T> ReturnReadOnly<T>(ref IList<T> collection)
    {
        var objList = collection;
        if (objList is ReadOnlyCollection<T> readOnlyCollection)
        {
            return readOnlyCollection;
        }

        Interlocked.CompareExchange(ref collection, objList.ToReadOnly(), objList);
        return (ReadOnlyCollection<T>)collection;
    }

    internal static ReadOnlyCollection<Expression> ReturnReadOnly(
        IArgumentProvider provider,
        ref object collection)
    {
        if (collection is Expression comparand)
        {
            Interlocked.CompareExchange(ref collection,
                new ReadOnlyCollection<Expression>(new ListArgumentProvider(provider, comparand)), comparand);
        }

        return (ReadOnlyCollection<Expression>)collection;
    }

    internal static void ValidateVariables(
        ReadOnlyCollection<ParameterExpression> varList,
        string collectionName)
    {
        if (varList.Count == 0)
        {
            return;
        }

        var count = varList.Count;
        var set = new Set<ParameterExpression>(count);
        for (var index = 0; index < count; ++index)
        {
            var var = varList[index];
            if (var == null)
            {
                throw new ArgumentNullException(string.Format(CultureInfo.CurrentCulture, "{0}[{1}]", collectionName,
                    set.Count));
            }

            if (var.IsByRef)
            {
                throw Error.VariableMustNotBeByRef(var, var.Type);
            }

            if (set.Contains(var))
            {
                throw Error.DuplicateVariable(var);
            }

            set.Add(var);
        }
    }

    private static MethodInfo ApplyTypeArgs(MethodInfo m, Type[] typeArgs)
    {
        if (typeArgs == null || typeArgs.Length == 0)
        {
            if (!m.IsGenericMethodDefinition)
            {
                return m;
            }
        }
        else if (m.IsGenericMethodDefinition && m.GetGenericArguments().Length == typeArgs.Length)
        {
            return m.MakeGenericMethod(typeArgs);
        }

        return null;
    }

    private static bool CheckMethod(MethodInfo method, MethodInfo propertyMethod)
    {
        if (method == propertyMethod)
        {
            return true;
        }

        var declaringType = method.DeclaringType;
        return declaringType.IsInterface && method.Name == propertyMethod.Name &&
               declaringType.GetMethod(method.Name) == propertyMethod;
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
                if (method == null || (!method.IsPublic && m.IsPublic))
                {
                    method = m;
                    bestMethod = 1;
                }
                else if (method.IsPublic == m.IsPublic)
                {
                    ++bestMethod;
                }
            }
        }

        return bestMethod;
    }

    private static int FindBestProperty(
        IEnumerable<PropertyInfo> properties,
        Expression[] args,
        out PropertyInfo property)
    {
        var bestProperty = 0;
        property = null;
        foreach (var property1 in properties)
        {
            if (property1 != null && IsCompatible(property1, args))
            {
                if (property == null)
                {
                    property = property1;
                    bestProperty = 1;
                }
                else
                {
                    ++bestProperty;
                }
            }
        }

        return bestProperty;
    }

    private static PropertyInfo FindInstanceProperty(
        Type type,
        string propertyName,
        Expression[] arguments)
    {
        var flags1 = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public |
                     BindingFlags.FlattenHierarchy;
        var property = FindProperty(type, propertyName, arguments, flags1);
        if (property == null)
        {
            var flags2 = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic |
                         BindingFlags.FlattenHierarchy;
            property = FindProperty(type, propertyName, arguments, flags2);
        }

        if (!(property == null))
        {
            return property;
        }

        if (arguments == null || arguments.Length == 0)
        {
            throw Error.InstancePropertyWithoutParameterNotDefinedForType(propertyName, type);
        }

        throw Error.InstancePropertyWithSpecifiedParametersNotDefinedForType(propertyName, GetArgTypesString(arguments),
            type);
    }

    private static MethodInfo FindMethod(
        Type type,
        string methodName,
        Type[] typeArgs,
        Expression[] args,
        BindingFlags flags)
    {
        var members = type.FindMembers(MemberTypes.Method, flags, Type.FilterNameIgnoreCase, methodName);
        if (members == null || members.Length == 0)
        {
            throw Error.MethodDoesNotExistOnType(methodName, type);
        }

        MethodInfo method;
        var bestMethod = FindBestMethod(members.Map((Func<MemberInfo, MethodInfo>)(t => (MethodInfo)t)), typeArgs, args,
            out method);
        if (bestMethod == 0)
        {
            if (typeArgs != null && typeArgs.Length != 0)
            {
                throw Error.GenericMethodWithArgsDoesNotExistOnType(methodName, type);
            }

            throw Error.MethodWithArgsDoesNotExistOnType(methodName, type);
        }

        if (bestMethod > 1)
        {
            throw Error.MethodWithMoreThanOneMatch(methodName, type);
        }

        return method;
    }

    private static PropertyInfo FindProperty(
        Type type,
        string propertyName,
        Expression[] arguments,
        BindingFlags flags)
    {
        var members = type.FindMembers(MemberTypes.Property, flags, Type.FilterNameIgnoreCase, propertyName);
        if (members == null || members.Length == 0)
        {
            return null;
        }

        PropertyInfo property;
        var bestProperty = FindBestProperty(members.Map((Func<MemberInfo, PropertyInfo>)(t => (PropertyInfo)t)),
            arguments, out property);
        if (bestProperty == 0)
        {
            return null;
        }

        if (bestProperty > 1)
        {
            throw Error.PropertyWithMoreThanOneMatch(propertyName, type);
        }

        return property;
    }

    private static string GetArgTypesString(Expression[] arguments)
    {
        var stringBuilder = new StringBuilder();
        var flag = true;
        stringBuilder.Append("(");
        foreach (var type in arguments.Select(arg => arg.Type))
        {
            if (!flag)
            {
                stringBuilder.Append(", ");
            }

            stringBuilder.Append(type.Name);
            flag = false;
        }

        stringBuilder.Append(")");
        return stringBuilder.ToString();
    }

    private static BinaryExpression GetComparisonOperator(
        ExpressionType binaryType,
        string opName,
        Expression left,
        Expression right,
        bool liftToNull)
    {
        if (!(left.Type == right.Type) || !TypeUtils.IsNumeric(left.Type))
        {
            return GetUserDefinedBinaryOperatorOrThrow(binaryType, opName, left, right, liftToNull);
        }

        return left.Type.IsNullableType() & liftToNull
            ? new SimpleBinaryExpression(binaryType, left, right, typeof(bool?))
            : new LogicalBinaryExpression(binaryType, left, right);
    }

    private static BinaryExpression GetEqualityComparisonOperator(
        ExpressionType binaryType,
        string opName,
        Expression left,
        Expression right,
        bool liftToNull)
    {
        if (left.Type == right.Type && (TypeUtils.IsNumeric(left.Type) || left.Type == typeof(object) ||
                                        TypeUtils.IsBool(left.Type) || left.Type.GetNonNullableType().IsEnum))
        {
            return left.Type.IsNullableType() & liftToNull
                ? new SimpleBinaryExpression(binaryType, left, right, typeof(bool?))
                : new LogicalBinaryExpression(binaryType, left, right);
        }

        var definedBinaryOperator = GetUserDefinedBinaryOperator(binaryType, opName, left, right, liftToNull);
        if (definedBinaryOperator != null)
        {
            return definedBinaryOperator;
        }

        if (!TypeUtils.HasBuiltInEqualityOperator(left.Type, right.Type) && !IsNullComparison(left, right))
        {
            throw Error.BinaryOperatorNotDefined(binaryType, left.Type, right.Type);
        }

        return left.Type.IsNullableType() & liftToNull
            ? new SimpleBinaryExpression(binaryType, left, right, typeof(bool?))
            : new LogicalBinaryExpression(binaryType, left, right);
    }

    private static BinaryExpression GetMethodBasedAssignOperator(
        ExpressionType binaryType,
        Expression left,
        Expression right,
        MethodInfo method,
        LambdaExpression conversion,
        bool liftToNull)
    {
        var basedAssignOperator = GetMethodBasedBinaryOperator(binaryType, left, right, method, liftToNull);
        if (conversion == null)
        {
            if (!TypeUtils.AreReferenceAssignable(left.Type, basedAssignOperator.Type))
            {
                throw Error.UserDefinedOpMustHaveValidReturnType(binaryType, basedAssignOperator.Method.Name);
            }
        }
        else
        {
            ValidateOpAssignConversionLambda(conversion, basedAssignOperator.Left, basedAssignOperator.Method,
                basedAssignOperator.NodeType);
            basedAssignOperator = new OpAssignMethodConversionBinaryExpression(basedAssignOperator.NodeType,
                basedAssignOperator.Left, basedAssignOperator.Right, basedAssignOperator.Left.Type,
                basedAssignOperator.Method, conversion);
        }

        return basedAssignOperator;
    }

    private static BinaryExpression GetMethodBasedBinaryOperator(
        ExpressionType binaryType,
        Expression left,
        Expression right,
        MethodInfo method,
        bool liftToNull)
    {
        ValidateOperator(method);
        var parametersCached = method.GetParameters();
        if (parametersCached.Length != 2)
        {
            throw Error.IncorrectNumberOfMethodCallArguments(method);
        }

        if (ParameterIsAssignable(parametersCached[0], left.Type) &&
            ParameterIsAssignable(parametersCached[1], right.Type))
        {
            ValidateParamswithOperandsOrThrow(parametersCached[0].ParameterType, left.Type, binaryType, method.Name);
            ValidateParamswithOperandsOrThrow(parametersCached[1].ParameterType, right.Type, binaryType, method.Name);
            return new MethodBinaryExpression(binaryType, left, right, method.ReturnType, method);
        }

        if (!left.Type.IsNullableType() || !right.Type.IsNullableType() ||
            !ParameterIsAssignable(parametersCached[0], left.Type.GetNonNullableType()) ||
            !ParameterIsAssignable(parametersCached[1], right.Type.GetNonNullableType()) ||
            !method.ReturnType.IsValueType || method.ReturnType.IsNullableType())
        {
            throw Error.OperandTypesDoNotMatchParameters(binaryType, method.Name);
        }

        return (method.ReturnType != typeof(bool)) | liftToNull
            ? new MethodBinaryExpression(binaryType, left, right, TypeUtils.GetNullableType(method.ReturnType), method)
            : (BinaryExpression)new MethodBinaryExpression(binaryType, left, right, typeof(bool), method);
    }

    private static UnaryExpression GetMethodBasedCoercionOperator(
        ExpressionType unaryType,
        Expression operand,
        Type convertToType,
        MethodInfo method)
    {
        ValidateOperator(method);
        var parametersCached = method.GetParameters();
        if (parametersCached.Length != 1)
        {
            throw Error.IncorrectNumberOfMethodCallArguments(method);
        }

        if (ParameterIsAssignable(parametersCached[0], operand.Type) &&
            TypeUtils.AreEquivalent(method.ReturnType, convertToType))
        {
            return new UnaryExpression(unaryType, operand, method.ReturnType, method);
        }

        if ((operand.Type.IsNullableType() || convertToType.IsNullableType()) &&
            ParameterIsAssignable(parametersCached[0], operand.Type.GetNonNullableType()) &&
            TypeUtils.AreEquivalent(method.ReturnType, convertToType.GetNonNullableType()))
        {
            return new UnaryExpression(unaryType, operand, convertToType, method);
        }

        throw Error.OperandTypesDoNotMatchParameters(unaryType, method.Name);
    }

    private static UnaryExpression GetMethodBasedUnaryOperator(
        ExpressionType unaryType,
        Expression operand,
        MethodInfo method)
    {
        ValidateOperator(method);
        var parametersCached = method.GetParameters();
        if (parametersCached.Length != 1)
        {
            throw Error.IncorrectNumberOfMethodCallArguments(method);
        }

        if (ParameterIsAssignable(parametersCached[0], operand.Type))
        {
            ValidateParamswithOperandsOrThrow(parametersCached[0].ParameterType, operand.Type, unaryType, method.Name);
            return new UnaryExpression(unaryType, operand, method.ReturnType, method);
        }

        if (operand.Type.IsNullableType() &&
            ParameterIsAssignable(parametersCached[0], operand.Type.GetNonNullableType()) &&
            method.ReturnType.IsValueType && !method.ReturnType.IsNullableType())
        {
            return new UnaryExpression(unaryType, operand, TypeUtils.GetNullableType(method.ReturnType), method);
        }

        throw Error.OperandTypesDoNotMatchParameters(unaryType, method.Name);
    }

    private static ParameterInfo[] GetParametersForValidation(
        MethodBase method,
        ExpressionType nodeKind)
    {
        var array = method.GetParameters();
        if (nodeKind == ExpressionType.Dynamic)
        {
            array = array.RemoveFirst();
        }

        return array;
    }

    private static PropertyInfo GetProperty(MethodInfo mi)
    {
        foreach (var property in mi.DeclaringType.GetProperties((BindingFlags)(48 /*0x30*/ | (mi.IsStatic ? 8 : 4))))
        {
            if ((property.CanRead && CheckMethod(mi, property.GetGetMethod(true))) ||
                (property.CanWrite && CheckMethod(mi, property.GetSetMethod(true))))
            {
                return property;
            }
        }

        throw Error.MethodNotPropertyAccessor(mi.DeclaringType, mi.Name);
    }

    private static Type GetResultTypeOfShift(Type left, Type right)
    {
        if (left.IsNullableType() || !right.IsNullableType())
        {
            return left;
        }

        return typeof(Nullable<>).MakeGenericType(left);
    }

    private static BinaryExpression GetUserDefinedAssignOperatorOrThrow(
        ExpressionType binaryType,
        string name,
        Expression left,
        Expression right,
        LambdaExpression conversion,
        bool liftToNull)
    {
        var assignOperatorOrThrow = GetUserDefinedBinaryOperatorOrThrow(binaryType, name, left, right, liftToNull);
        if (conversion == null)
        {
            if (!TypeUtils.AreReferenceAssignable(left.Type, assignOperatorOrThrow.Type))
            {
                throw Error.UserDefinedOpMustHaveValidReturnType(binaryType, assignOperatorOrThrow.Method.Name);
            }
        }
        else
        {
            ValidateOpAssignConversionLambda(conversion, assignOperatorOrThrow.Left, assignOperatorOrThrow.Method,
                assignOperatorOrThrow.NodeType);
            assignOperatorOrThrow = new OpAssignMethodConversionBinaryExpression(assignOperatorOrThrow.NodeType,
                assignOperatorOrThrow.Left, assignOperatorOrThrow.Right, assignOperatorOrThrow.Left.Type,
                assignOperatorOrThrow.Method, conversion);
        }

        return assignOperatorOrThrow;
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
        {
            return new MethodBinaryExpression(binaryType, left, right, definedBinaryOperator1.ReturnType,
                definedBinaryOperator1);
        }

        if (left.Type.IsNullableType() && right.Type.IsNullableType())
        {
            var nonNullableType1 = left.Type.GetNonNullableType();
            var nonNullableType2 = right.Type.GetNonNullableType();
            var definedBinaryOperator2 =
                GetUserDefinedBinaryOperator(binaryType, nonNullableType1, nonNullableType2, name);
            if (definedBinaryOperator2 != null && definedBinaryOperator2.ReturnType.IsValueType &&
                !definedBinaryOperator2.ReturnType.IsNullableType())
            {
                return (definedBinaryOperator2.ReturnType != typeof(bool)) | liftToNull
                    ? new MethodBinaryExpression(binaryType, left, right,
                        TypeUtils.GetNullableType(definedBinaryOperator2.ReturnType), definedBinaryOperator2)
                    : (BinaryExpression)new MethodBinaryExpression(binaryType, left, right, typeof(bool),
                        definedBinaryOperator2);
            }
        }

        return null;
    }

    private static MethodInfo GetUserDefinedBinaryOperator(
        ExpressionType binaryType,
        Type leftType,
        Type rightType,
        string name)
    {
        var types = new Type[2] { leftType, rightType };
        var nonNullableType1 = leftType.GetNonNullableType();
        var nonNullableType2 = rightType.GetNonNullableType();
        var bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var method = nonNullableType1.GetMethod(name, bindingAttr, null, types, null);
        if (method == null && !TypeUtils.AreEquivalent(leftType, rightType))
        {
            method = nonNullableType2.GetMethod(name, bindingAttr, null, types, null);
        }

        if (IsLiftingConditionalLogicalOperator(leftType, rightType, method, binaryType))
        {
            method = GetUserDefinedBinaryOperator(binaryType, nonNullableType1, nonNullableType2, name);
        }

        return method;
    }

    private static BinaryExpression GetUserDefinedBinaryOperatorOrThrow(
        ExpressionType binaryType,
        string name,
        Expression left,
        Expression right,
        bool liftToNull)
    {
        var definedBinaryOperator = GetUserDefinedBinaryOperator(binaryType, name, left, right, liftToNull);
        var parameterInfoArray = definedBinaryOperator != null
            ? definedBinaryOperator.Method.GetParameters()
            : throw Error.BinaryOperatorNotDefined(binaryType, left.Type, right.Type);
        ValidateParamswithOperandsOrThrow(parameterInfoArray[0].ParameterType, left.Type, binaryType, name);
        ValidateParamswithOperandsOrThrow(parameterInfoArray[1].ParameterType, right.Type, binaryType, name);
        return definedBinaryOperator;
    }

    private static UnaryExpression GetUserDefinedCoercion(
        ExpressionType coercionType,
        Expression expression,
        Type convertToType)
    {
        var definedCoercionMethod = TypeUtils.GetUserDefinedCoercionMethod(expression.Type, convertToType, false);
        return definedCoercionMethod != null
            ? new UnaryExpression(coercionType, expression, convertToType, definedCoercionMethod)
            : null;
    }

    private static UnaryExpression GetUserDefinedCoercionOrThrow(
        ExpressionType coercionType,
        Expression expression,
        Type convertToType)
    {
        return GetUserDefinedCoercion(coercionType, expression, convertToType) ??
               throw Error.CoercionOperatorNotDefined(expression.Type, convertToType);
    }

    private static UnaryExpression GetUserDefinedUnaryOperator(
        ExpressionType unaryType,
        string name,
        Expression operand)
    {
        var type = operand.Type;
        var types = new Type[1] { type };
        var nonNullableType = type.GetNonNullableType();
        var bindingAttr = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        var methodValidated1 = nonNullableType.GetMethod(name, bindingAttr, null, types, null);
        if (methodValidated1 != null)
        {
            return new UnaryExpression(unaryType, operand, methodValidated1.ReturnType, methodValidated1);
        }

        if (type.IsNullableType())
        {
            types[0] = nonNullableType;
            var methodValidated2 = nonNullableType.GetMethod(name, bindingAttr, null, types, null);
            if (methodValidated2 != null && methodValidated2.ReturnType.IsValueType &&
                !methodValidated2.ReturnType.IsNullableType())
            {
                return new UnaryExpression(unaryType, operand, TypeUtils.GetNullableType(methodValidated2.ReturnType),
                    methodValidated2);
            }
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
        {
            throw Error.UnaryOperatorNotDefined(unaryType, operand.Type);
        }

        ValidateParamswithOperandsOrThrow(definedUnaryOperator.Method.GetParameters()[0].ParameterType, operand.Type,
            unaryType, name);
        return definedUnaryOperator;
    }

    private static MethodInfo GetValidMethodForDynamic(Type delegateType)
    {
        var method = delegateType.GetMethod("Invoke");
        var parametersCached = method.GetParameters();
        if (parametersCached.Length == 0 || parametersCached[0].ParameterType != typeof(CallSite))
        {
            throw Error.FirstArgumentMustBeCallSite();
        }

        return method;
    }

    private static bool IsCompatible(PropertyInfo pi, Expression[] args)
    {
        var method = pi.GetGetMethod(true);
        ParameterInfo[] parameterInfoArray;
        if (method != null)
        {
            parameterInfoArray = method.GetParameters();
        }
        else
        {
            method = pi.GetSetMethod(true);
            parameterInfoArray = method.GetParameters().RemoveLast();
        }

        if (method == null)
        {
            return false;
        }

        if (args == null)
        {
            return parameterInfoArray.Length == 0;
        }

        if (parameterInfoArray.Length != args.Length)
        {
            return false;
        }

        for (var index = 0; index < args.Length; ++index)
        {
            if (args[index] == null ||
                !TypeUtils.AreReferenceAssignable(parameterInfoArray[index].ParameterType, args[index].Type))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCompatible(MethodBase m, Expression[] args)
    {
        var parametersCached = m.GetParameters();
        if (parametersCached.Length != args.Length)
        {
            return false;
        }

        for (var index = 0; index < args.Length; ++index)
        {
            var expression = args[index];
            ContractUtils.RequiresNotNull(expression, "argument");
            var type1 = expression.Type;
            var type2 = parametersCached[index].ParameterType;
            if (type2.IsByRef)
            {
                type2 = type2.GetElementType();
            }

            if (!TypeUtils.AreReferenceAssignable(type2, type1) &&
                (!TypeUtils.IsSameOrSubclass(typeof(LambdaExpression), type2) ||
                 !type2.IsAssignableFrom(expression.GetType())))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsLiftingConditionalLogicalOperator(
        Type left,
        Type right,
        MethodInfo method,
        ExpressionType binaryType)
    {
        if (!right.IsNullableType() || !left.IsNullableType() || !(method == null))
        {
            return false;
        }

        return binaryType == ExpressionType.AndAlso || binaryType == ExpressionType.OrElse;
    }

    private static bool IsNullComparison(Expression left, Expression right)
    {
        return (IsNullConstant(left) && !IsNullConstant(right) && right.Type.IsNullableType()) ||
               (IsNullConstant(right) && !IsNullConstant(left) && left.Type.IsNullableType());
    }

    private static bool IsNullConstant(Expression e)
    {
        return e is ConstantExpression constantExpression && constantExpression.Value == null;
    }

    private static bool IsSimpleShift(Type left, Type right)
    {
        return TypeUtils.IsInteger(left) && right.GetNonNullableType() == typeof(int);
    }

    private static bool IsValidLiftedConditionalLogicalOperator(
        Type left,
        Type right,
        ParameterInfo[] pms)
    {
        return TypeUtils.AreEquivalent(left, right) && right.IsNullableType() &&
               TypeUtils.AreEquivalent(pms[1].ParameterType, right.GetNonNullableType());
    }

    private static DynamicExpression MakeDynamic(
        CallSiteBinder binder,
        Type returnType,
        ReadOnlyCollection<Expression> args)
    {
        ContractUtils.RequiresNotNull(binder, nameof(binder));
        for (var index = 0; index < args.Count; ++index)
        {
            ValidateDynamicArgument(args[index]);
        }

        var delegateType = DelegateHelpers.MakeCallSiteDelegate(args, returnType);
        switch (args.Count)
        {
            case 1:
                return DynamicExpression.Make(returnType, delegateType, binder, args[0]);
            case 2:
                return DynamicExpression.Make(returnType, delegateType, binder, args[0], args[1]);
            case 3:
                return DynamicExpression.Make(returnType, delegateType, binder, args[0], args[1], args[2]);
            case 4:
                return DynamicExpression.Make(returnType, delegateType, binder, args[0], args[1], args[2], args[3]);
            default:
                return DynamicExpression.Make(returnType, delegateType, binder, args);
        }
    }

    private static UnaryExpression MakeOpAssignUnary(
        ExpressionType kind,
        Expression expression,
        MethodInfo method)
    {
        RequiresCanRead(expression, nameof(expression));
        RequiresCanWrite(expression, nameof(expression));
        UnaryExpression unaryExpression;
        if (method == null)
        {
            if (TypeUtils.IsArithmetic(expression.Type))
            {
                return new UnaryExpression(kind, expression, expression.Type, null);
            }

            var name = kind == ExpressionType.PreIncrementAssign || kind == ExpressionType.PostIncrementAssign
                ? "op_Increment"
                : "op_Decrement";
            unaryExpression = GetUserDefinedUnaryOperatorOrThrow(kind, name, expression);
        }
        else
        {
            unaryExpression = GetMethodBasedUnaryOperator(kind, expression, method);
        }

        if (!TypeUtils.AreReferenceAssignable(expression.Type, unaryExpression.Type))
        {
            throw Error.UserDefinedOpMustHaveValidReturnType(kind, method.Name);
        }

        return unaryExpression;
    }

    private static void RequiresCanRead(Expression expression, string paramName)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(paramName);
        }

        switch (expression.NodeType)
        {
            case ExpressionType.MemberAccess:
                var member = ((MemberExpression)expression).Member;
                if (member.MemberType != MemberTypes.Property || ((PropertyInfo)member).CanRead)
                {
                    break;
                }

                throw new ArgumentException(Strings.ExpressionMustBeReadable, paramName);
            case ExpressionType.Index:
                var indexExpression = (IndexExpression)expression;
                if (!(indexExpression.Indexer != null) || indexExpression.Indexer.CanRead)
                {
                    break;
                }

                throw new ArgumentException(Strings.ExpressionMustBeReadable, paramName);
        }
    }

    private static void RequiresCanRead(IEnumerable<Expression> items, string paramName)
    {
        if (items == null)
        {
            return;
        }

        if (items is IList<Expression> expressionList)
        {
            for (var index = 0; index < expressionList.Count; ++index)
            {
                RequiresCanRead(expressionList[index], paramName);
            }
        }
        else
        {
            foreach (var expression in items)
            {
                RequiresCanRead(expression, paramName);
            }
        }
    }

    private static void RequiresCanWrite(Expression expression, string paramName)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(paramName);
        }

        var flag = false;
        switch (expression.NodeType)
        {
            case ExpressionType.MemberAccess:
                var memberExpression = (MemberExpression)expression;
                switch (memberExpression.Member.MemberType)
                {
                    case MemberTypes.Field:
                        var member = (FieldInfo)memberExpression.Member;
                        flag = !member.IsInitOnly && !member.IsLiteral;
                        break;
                    case MemberTypes.Property:
                        flag = ((PropertyInfo)memberExpression.Member).CanWrite;
                        break;
                }

                break;
            case ExpressionType.Parameter:
                flag = true;
                break;
            case ExpressionType.Index:
                var indexExpression = (IndexExpression)expression;
                flag = !(indexExpression.Indexer != null) || indexExpression.Indexer.CanWrite;
                break;
        }

        if (!flag)
        {
            throw new ArgumentException(Strings.ExpressionMustBeWriteable, paramName);
        }
    }

    private static bool TryQuote(Type parameterType, ref Expression argument)
    {
        if (!TypeUtils.IsSameOrSubclass(typeof(LambdaExpression), parameterType) ||
            !parameterType.IsAssignableFrom(argument.GetType()))
        {
            return false;
        }

        argument = Quote(argument);
        return true;
    }

    private static void ValidateAccessor(
        Expression instance,
        MethodInfo method,
        ParameterInfo[] indexes,
        ref ReadOnlyCollection<Expression> arguments)
    {
        ContractUtils.RequiresNotNull(arguments, nameof(arguments));
        ValidateMethodInfo(method);
        if ((method.CallingConvention & CallingConventions.VarArgs) != 0)
        {
            throw Error.AccessorsCannotHaveVarArgs();
        }

        if (method.IsStatic)
        {
            if (instance != null)
            {
                throw Error.OnlyStaticMethodsHaveNullInstance();
            }
        }
        else
        {
            if (instance == null)
            {
                throw Error.OnlyStaticMethodsHaveNullInstance();
            }

            RequiresCanRead(instance, nameof(instance));
            ValidateCallInstanceType(instance.Type, method);
        }

        ValidateAccessorArgumentTypes(method, indexes, ref arguments);
    }

    private static void ValidateAccessorArgumentTypes(
        MethodInfo method,
        ParameterInfo[] indexes,
        ref ReadOnlyCollection<Expression> arguments)
    {
        if (indexes.Length != 0)
        {
            if (indexes.Length != arguments.Count)
            {
                throw Error.IncorrectNumberOfMethodCallArguments(method);
            }

            var list = (Expression[])null;
            var index1 = 0;
            for (var length = indexes.Length; index1 < length; ++index1)
            {
                var expression = arguments[index1];
                var index2 = indexes[index1];
                RequiresCanRead(expression, nameof(arguments));
                var parameterType = index2.ParameterType;
                if (parameterType.IsByRef)
                {
                    throw Error.AccessorsCannotHaveByRefArgs();
                }

                TypeUtils.ValidateType(parameterType);
                if (!TypeUtils.AreReferenceAssignable(parameterType, expression.Type) &&
                    !TryQuote(parameterType, ref expression))
                {
                    throw Error.ExpressionTypeDoesNotMatchMethodParameter(expression.Type, parameterType, method);
                }

                if (list == null && expression != arguments[index1])
                {
                    list = new Expression[arguments.Count];
                    for (var index3 = 0; index3 < index1; ++index3)
                    {
                        list[index3] = arguments[index3];
                    }
                }

                if (list != null)
                {
                    list[index1] = expression;
                }
            }

            if (list == null)
            {
                return;
            }

            arguments = new TrueReadOnlyCollection<Expression>(list);
        }
        else if (arguments.Count > 0)
        {
            throw Error.IncorrectNumberOfMethodCallArguments(method);
        }
    }

    private static void ValidateAnonymousTypeMember(ref MemberInfo member, out Type memberType)
    {
        switch (member.MemberType)
        {
            case MemberTypes.Field:
                var fieldInfo = member as FieldInfo;
                memberType = !fieldInfo.IsStatic ? fieldInfo.FieldType : throw Error.ArgumentMustBeInstanceMember();
                break;
            case MemberTypes.Method:
                var mi = member as MethodInfo;
                var propertyInfo = !mi.IsStatic ? GetProperty(mi) : throw Error.ArgumentMustBeInstanceMember();
                member = propertyInfo;
                memberType = propertyInfo.PropertyType;
                break;
            case MemberTypes.Property:
                var p0 = member as PropertyInfo;
                if (!p0.CanRead)
                {
                    throw Error.PropertyDoesNotHaveGetter(p0);
                }

                memberType = !p0.GetGetMethod().IsStatic ? p0.PropertyType : throw Error.ArgumentMustBeInstanceMember();
                break;
            default:
                throw Error.ArgumentMustBeFieldInfoOrPropertInfoOrMethod();
        }
    }

    private static void ValidateArgumentCount(
        MethodBase method,
        ExpressionType nodeKind,
        int count,
        ParameterInfo[] pis)
    {
        if (pis.Length != count)
        {
            if (nodeKind <= ExpressionType.Invoke)
            {
                if (nodeKind != ExpressionType.Call)
                {
                    if (nodeKind == ExpressionType.Invoke)
                    {
                        throw Error.IncorrectNumberOfLambdaArguments();
                    }

                    goto label_10;
                }
            }
            else
            {
                if (nodeKind == ExpressionType.New)
                {
                    throw Error.IncorrectNumberOfConstructorArguments();
                }

                if (nodeKind != ExpressionType.Dynamic)
                {
                    goto label_10;
                }
            }

            throw Error.IncorrectNumberOfMethodCallArguments(method);
            label_10:
            throw ContractUtils.Unreachable;
        }
    }

    private static void ValidateArgumentTypes(
        MethodBase method,
        ExpressionType nodeKind,
        ref ReadOnlyCollection<Expression> arguments)
    {
        var parametersForValidation = GetParametersForValidation(method, nodeKind);
        ValidateArgumentCount(method, nodeKind, arguments.Count, parametersForValidation);
        var list = (Expression[])null;
        var index1 = 0;
        for (var length = parametersForValidation.Length; index1 < length; ++index1)
        {
            var expression1 = arguments[index1];
            var pi = parametersForValidation[index1];
            var expression2 = ValidateOneArgument(method, nodeKind, expression1, pi);
            if (list == null && expression2 != arguments[index1])
            {
                list = new Expression[arguments.Count];
                for (var index2 = 0; index2 < index1; ++index2)
                {
                    list[index2] = arguments[index2];
                }
            }

            if (list != null)
            {
                list[index1] = expression2;
            }
        }

        if (list == null)
        {
            return;
        }

        arguments = new TrueReadOnlyCollection<Expression>(list);
    }

    private static void ValidateCallInstanceType(Type instanceType, MethodInfo method)
    {
        if (!TypeUtils.IsValidInstanceType(method, instanceType))
        {
            throw Error.InstanceAndMethodTypeMismatch(method, method.DeclaringType, instanceType);
        }
    }

    private static Type ValidateCoalesceArgTypes(Type left, Type right)
    {
        var nonNullableType = left.GetNonNullableType();
        if (left.IsValueType && !left.IsNullableType())
        {
            throw Error.CoalesceUsedOnNonNullType();
        }

        if (left.IsNullableType() && TypeUtils.IsImplicitlyConvertible(right, nonNullableType))
        {
            return nonNullableType;
        }

        if (TypeUtils.IsImplicitlyConvertible(right, left))
        {
            return left;
        }

        return TypeUtils.IsImplicitlyConvertible(nonNullableType, right) ? right : throw Error.ArgumentTypesMustMatch();
    }

    private static void ValidateDynamicArgument(Expression arg)
    {
        RequiresCanRead(arg, "arguments");
        var type = arg.Type;
        ContractUtils.RequiresNotNull(type, "type");
        TypeUtils.ValidateType(type);
        if (type == typeof(void))
        {
            throw Error.ArgumentTypeCannotBeVoid();
        }
    }

    private static void ValidateElementInitAddMethodInfo(MethodInfo addMethod)
    {
        ValidateMethodInfo(addMethod);
        var parametersCached = addMethod.GetParameters();
        if (parametersCached.Length == 0)
        {
            throw Error.ElementInitializerMethodWithZeroArgs();
        }

        if (!addMethod.Name.Equals("Add", StringComparison.OrdinalIgnoreCase))
        {
            throw Error.ElementInitializerMethodNotAdd();
        }

        if (addMethod.IsStatic)
        {
            throw Error.ElementInitializerMethodStatic();
        }

        foreach (var parameterInfo in parametersCached)
        {
            if (parameterInfo.ParameterType.IsByRef)
            {
                throw Error.ElementInitializerMethodNoRefOutParam(parameterInfo.Name, addMethod.Name);
            }
        }
    }

    private static void ValidateGettableFieldOrPropertyMember(MemberInfo member, out Type memberType)
    {
        var fieldInfo = member as FieldInfo;
        if (fieldInfo == null)
        {
            var p0 = member as PropertyInfo;
            if (p0 == null)
            {
                throw Error.ArgumentMustBeFieldInfoOrPropertInfo();
            }

            memberType = p0.CanRead ? p0.PropertyType : throw Error.PropertyDoesNotHaveGetter(p0);
        }
        else
        {
            memberType = fieldInfo.FieldType;
        }
    }

    private static void ValidateGoto(
        LabelTarget target,
        ref Expression value,
        string targetParameter,
        string valueParameter)
    {
        ContractUtils.RequiresNotNull(target, targetParameter);
        if (value == null)
        {
            if (target.Type != typeof(void))
            {
                throw Error.LabelMustBeVoidOrHaveExpression();
            }
        }
        else
        {
            ValidateGotoType(target.Type, ref value, valueParameter);
        }
    }

    private static void ValidateGotoType(Type expectedType, ref Expression value, string paramName)
    {
        RequiresCanRead(value, paramName);
        if (expectedType != typeof(void) && !TypeUtils.AreReferenceAssignable(expectedType, value.Type) &&
            !TryQuote(expectedType, ref value))
        {
            throw Error.ExpressionTypeDoesNotMatchLabel(value.Type, expectedType);
        }
    }

    private static void ValidateIndexedProperty(
        Expression instance,
        PropertyInfo property,
        ref ReadOnlyCollection<Expression> argList)
    {
        ContractUtils.RequiresNotNull(property, nameof(property));
        if (property.PropertyType.IsByRef)
        {
            throw Error.PropertyCannotHaveRefType();
        }

        if (property.PropertyType == typeof(void))
        {
            throw Error.PropertyTypeCannotBeVoid();
        }

        var indexes = (ParameterInfo[])null;
        var getMethod = property.GetGetMethod(true);
        if (getMethod != null)
        {
            indexes = getMethod.GetParameters();
            ValidateAccessor(instance, getMethod, indexes, ref argList);
        }

        var setMethod = property.GetSetMethod(true);
        if (setMethod != null)
        {
            var parametersCached = setMethod.GetParameters();
            if (parametersCached.Length == 0)
            {
                throw Error.SetterHasNoParams();
            }

            var parameterType = parametersCached[parametersCached.Length - 1].ParameterType;
            if (parameterType.IsByRef)
            {
                throw Error.PropertyCannotHaveRefType();
            }

            if (setMethod.ReturnType != typeof(void))
            {
                throw Error.SetterMustBeVoid();
            }

            if (property.PropertyType != parameterType)
            {
                throw Error.PropertyTyepMustMatchSetter();
            }

            if (getMethod != null)
            {
                if (getMethod.IsStatic ^ setMethod.IsStatic)
                {
                    throw Error.BothAccessorsMustBeStatic();
                }

                if (indexes.Length != parametersCached.Length - 1)
                {
                    throw Error.IndexesOfSetGetMustMatch();
                }

                for (var index = 0; index < indexes.Length; ++index)
                {
                    if (indexes[index].ParameterType != parametersCached[index].ParameterType)
                    {
                        throw Error.IndexesOfSetGetMustMatch();
                    }
                }
            }
            else
            {
                ValidateAccessor(instance, setMethod, parametersCached.RemoveLast(), ref argList);
            }
        }

        if (getMethod == null && setMethod == null)
        {
            throw Error.PropertyDoesNotHaveAccessor(property);
        }
    }

    private static void ValidateLambdaArgs(
        Type delegateType,
        ref Expression body,
        ReadOnlyCollection<ParameterExpression> parameters)
    {
        ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
        RequiresCanRead(body, nameof(body));
        if (!typeof(MulticastDelegate).IsAssignableFrom(delegateType) || delegateType == typeof(MulticastDelegate))
        {
            throw Error.LambdaTypeMustBeDerivedFromSystemDelegate();
        }

        var lambdaDelegateCache = _LambdaDelegateCache;
        MethodInfo method;
        if (!lambdaDelegateCache.TryGetValue(delegateType, out method))
        {
            method = delegateType.GetMethod("Invoke");
            if (delegateType.CanCache())
            {
                lambdaDelegateCache[delegateType] = method;
            }
        }

        var parametersCached = method.GetParameters();
        if (parametersCached.Length != 0)
        {
            if (parametersCached.Length != parameters.Count)
            {
                throw Error.IncorrectNumberOfLambdaDeclarationParameters();
            }

            var set = new Set<ParameterExpression>(parametersCached.Length);
            var index = 0;
            for (var length = parametersCached.Length; index < length; ++index)
            {
                var parameter = parameters[index];
                var parameterInfo = parametersCached[index];
                RequiresCanRead(parameter, nameof(parameters));
                var type = parameterInfo.ParameterType;
                if (parameter.IsByRef)
                {
                    type = type.IsByRef
                        ? type.GetElementType()
                        : throw Error.ParameterExpressionNotValidAsDelegate(parameter.Type.MakeByRefType(), type);
                }

                if (!TypeUtils.AreReferenceAssignable(parameter.Type, type))
                {
                    throw Error.ParameterExpressionNotValidAsDelegate(parameter.Type, type);
                }

                if (set.Contains(parameter))
                {
                    throw Error.DuplicateVariable(parameter);
                }

                set.Add(parameter);
            }
        }
        else if (parameters.Count > 0)
        {
            throw Error.IncorrectNumberOfLambdaDeclarationParameters();
        }

        if (method.ReturnType != typeof(void) && !TypeUtils.AreReferenceAssignable(method.ReturnType, body.Type) &&
            !TryQuote(method.ReturnType, ref body))
        {
            throw Error.ExpressionTypeDoesNotMatchReturn(body.Type, method.ReturnType);
        }
    }

    private static void ValidateListInitArgs(
        Type listType,
        ReadOnlyCollection<ElementInit> initializers)
    {
        if (!typeof(IEnumerable).IsAssignableFrom(listType))
        {
            throw Error.TypeNotIEnumerable(listType);
        }

        var index = 0;
        for (var count = initializers.Count; index < count; ++index)
        {
            var initializer = initializers[index];
            ContractUtils.RequiresNotNull(initializer, nameof(initializers));
            ValidateCallInstanceType(listType, initializer.AddMethod);
        }
    }

    private static void ValidateMemberInitArgs(Type type, ReadOnlyCollection<MemberBinding> bindings)
    {
        var index = 0;
        for (var count = bindings.Count; index < count; ++index)
        {
            var binding = bindings[index];
            ContractUtils.RequiresNotNull(binding, nameof(bindings));
            if (!binding.Member.DeclaringType.IsAssignableFrom(type))
            {
                throw Error.NotAMemberOfType(binding.Member.Name, type);
            }
        }
    }

    private static ParameterInfo[] ValidateMethodAndGetParameters(
        Expression instance,
        MethodInfo method)
    {
        ValidateMethodInfo(method);
        ValidateStaticOrInstanceMethod(instance, method);
        return GetParametersForValidation(method, ExpressionType.Call);
    }

    private static void ValidateMethodInfo(MethodInfo method)
    {
        if (method.IsGenericMethodDefinition)
        {
            throw Error.MethodIsGeneric(method);
        }

        if (method.ContainsGenericParameters)
        {
            throw Error.MethodContainsGenericParameters(method);
        }
    }

    private static void ValidateNewArgs(
        ConstructorInfo constructor,
        ref ReadOnlyCollection<Expression> arguments,
        ref ReadOnlyCollection<MemberInfo> members)
    {
        ParameterInfo[] parametersCached;
        if ((parametersCached = constructor.GetParameters()).Length != 0)
        {
            if (arguments.Count != parametersCached.Length)
            {
                throw Error.IncorrectNumberOfConstructorArguments();
            }

            if (arguments.Count != members.Count)
            {
                throw Error.IncorrectNumberOfArgumentsForMembers();
            }

            var list1 = (Expression[])null;
            var list2 = (MemberInfo[])null;
            var index1 = 0;
            for (var count = arguments.Count; index1 < count; ++index1)
            {
                var expression = arguments[index1];
                RequiresCanRead(expression, "argument");
                var member = members[index1];
                ContractUtils.RequiresNotNull(member, "member");
                if (!TypeUtils.AreEquivalent(member.DeclaringType, constructor.DeclaringType))
                {
                    throw Error.ArgumentMemberNotDeclOnType(member.Name, constructor.DeclaringType.Name);
                }

                Type memberType;
                ValidateAnonymousTypeMember(ref member, out memberType);
                if (!TypeUtils.AreReferenceAssignable(memberType, expression.Type) &&
                    !TryQuote(memberType, ref expression))
                {
                    throw Error.ArgumentTypeDoesNotMatchMember(expression.Type, memberType);
                }

                var type = parametersCached[index1].ParameterType;
                if (type.IsByRef)
                {
                    type = type.GetElementType();
                }

                if (!TypeUtils.AreReferenceAssignable(type, expression.Type) && !TryQuote(type, ref expression))
                {
                    throw Error.ExpressionTypeDoesNotMatchConstructorParameter(expression.Type, type);
                }

                if (list1 == null && expression != arguments[index1])
                {
                    list1 = new Expression[arguments.Count];
                    for (var index2 = 0; index2 < index1; ++index2)
                    {
                        list1[index2] = arguments[index2];
                    }
                }

                if (list1 != null)
                {
                    list1[index1] = expression;
                }

                if (list2 == null && member != members[index1])
                {
                    list2 = new MemberInfo[members.Count];
                    for (var index3 = 0; index3 < index1; ++index3)
                    {
                        list2[index3] = members[index3];
                    }
                }

                if (list2 != null)
                {
                    list2[index1] = member;
                }
            }

            if (list1 != null)
            {
                arguments = new TrueReadOnlyCollection<Expression>(list1);
            }

            if (list2 == null)
            {
                return;
            }

            members = new TrueReadOnlyCollection<MemberInfo>(list2);
        }
        else
        {
            if (arguments != null && arguments.Count > 0)
            {
                throw Error.IncorrectNumberOfConstructorArguments();
            }

            if (members != null && members.Count > 0)
            {
                throw Error.IncorrectNumberOfMembersForGivenConstructor();
            }
        }
    }

    private static Expression ValidateOneArgument(
        MethodBase method,
        ExpressionType nodeKind,
        Expression arg,
        ParameterInfo pi)
    {
        RequiresCanRead(arg, "arguments");
        var type = pi.ParameterType;
        if (type.IsByRef)
        {
            type = type.GetElementType();
        }

        TypeUtils.ValidateType(type);
        if (!TypeUtils.AreReferenceAssignable(type, arg.Type) && !TryQuote(type, ref arg))
        {
            if (nodeKind <= ExpressionType.Invoke)
            {
                if (nodeKind != ExpressionType.Call)
                {
                    if (nodeKind == ExpressionType.Invoke)
                    {
                        throw Error.ExpressionTypeDoesNotMatchParameter(arg.Type, type);
                    }

                    goto label_11;
                }
            }
            else
            {
                if (nodeKind == ExpressionType.New)
                {
                    throw Error.ExpressionTypeDoesNotMatchConstructorParameter(arg.Type, type);
                }

                if (nodeKind != ExpressionType.Dynamic)
                {
                    goto label_11;
                }
            }

            throw Error.ExpressionTypeDoesNotMatchMethodParameter(arg.Type, type, method);
            label_11:
            throw ContractUtils.Unreachable;
        }

        return arg;
    }

    private static void ValidateOpAssignConversionLambda(
        LambdaExpression conversion,
        Expression left,
        MethodInfo method,
        ExpressionType nodeType)
    {
        var method1 = conversion.Type.GetMethod("Invoke");
        var parametersCached = method1.GetParameters();
        if (parametersCached.Length != 1)
        {
            throw Error.IncorrectNumberOfMethodCallArguments(conversion);
        }

        if (!TypeUtils.AreEquivalent(method1.ReturnType, left.Type))
        {
            throw Error.OperandTypesDoNotMatchParameters(nodeType, conversion.ToString());
        }

        if (method != null && !TypeUtils.AreEquivalent(parametersCached[0].ParameterType, method.ReturnType))
        {
            throw Error.OverloadOperatorTypeDoesNotMatchConversionType(nodeType, conversion.ToString());
        }
    }

    private static void ValidateOperator(MethodInfo method)
    {
        ValidateMethodInfo(method);
        if (!method.IsStatic)
        {
            throw Error.UserDefinedOperatorMustBeStatic(method);
        }

        if (method.ReturnType == typeof(void))
        {
            throw Error.UserDefinedOperatorMustNotBeVoid(method);
        }
    }

    private static void ValidateParamswithOperandsOrThrow(
        Type paramType,
        Type operandType,
        ExpressionType exprType,
        string name)
    {
        if (paramType.IsNullableType() && !operandType.IsNullableType())
        {
            throw Error.OperandTypesDoNotMatchParameters(exprType, name);
        }
    }

    private static void ValidateSettableFieldOrPropertyMember(MemberInfo member, out Type memberType)
    {
        var fieldInfo = member as FieldInfo;
        if (fieldInfo == null)
        {
            var p0 = member as PropertyInfo;
            if (p0 == null)
            {
                throw Error.ArgumentMustBeFieldInfoOrPropertInfo();
            }

            memberType = p0.CanWrite ? p0.PropertyType : throw Error.PropertyDoesNotHaveSetter(p0);
        }
        else
        {
            memberType = fieldInfo.FieldType;
        }
    }

    private static void ValidateSpan(int startLine, int startColumn, int endLine, int endColumn)
    {
        if (startLine < 1)
        {
            throw Error.OutOfRange(nameof(startLine), 1);
        }

        if (startColumn < 1)
        {
            throw Error.OutOfRange(nameof(startColumn), 1);
        }

        if (endLine < 1)
        {
            throw Error.OutOfRange(nameof(endLine), 1);
        }

        if (endColumn < 1)
        {
            throw Error.OutOfRange(nameof(endColumn), 1);
        }

        if (startLine > endLine)
        {
            throw Error.StartEndMustBeOrdered();
        }

        if (startLine == endLine && startColumn > endColumn)
        {
            throw Error.StartEndMustBeOrdered();
        }
    }

    private static void ValidateStaticOrInstanceMethod(Expression instance, MethodInfo method)
    {
        if (method.IsStatic)
        {
            if (instance != null)
            {
                throw new ArgumentException(Strings.OnlyStaticMethodsHaveNullInstance, nameof(instance));
            }
        }
        else
        {
            if (instance == null)
            {
                throw new ArgumentException(Strings.OnlyStaticMethodsHaveNullInstance, nameof(method));
            }

            RequiresCanRead(instance, nameof(instance));
            ValidateCallInstanceType(instance.Type, method);
        }
    }

    private static void ValidateSwitchCaseType(
        Expression @case,
        bool customType,
        Type resultType,
        string parameterName)
    {
        if (customType)
        {
            if (resultType != typeof(void) && !TypeUtils.AreReferenceAssignable(resultType, @case.Type))
            {
                throw new ArgumentException(Strings.ArgumentTypesMustMatch, parameterName);
            }
        }
        else if (!TypeUtils.AreEquivalent(resultType, @case.Type))
        {
            throw new ArgumentException(Strings.AllCaseBodiesMustHaveSameType, parameterName);
        }
    }

    private static void ValidateTryAndCatchHaveSameType(
        Type type,
        Expression tryBody,
        ReadOnlyCollection<CatchBlock> handlers)
    {
        if (type != null)
        {
            if (!(type != typeof(void)))
            {
                return;
            }

            if (!TypeUtils.AreReferenceAssignable(type, tryBody.Type))
            {
                throw Error.ArgumentTypesMustMatch();
            }

            foreach (var handler in handlers)
            {
                if (!TypeUtils.AreReferenceAssignable(type, handler.Body.Type))
                {
                    throw Error.ArgumentTypesMustMatch();
                }
            }
        }
        else if (tryBody == null || tryBody.Type == typeof(void))
        {
            foreach (var handler in handlers)
            {
                if (handler.Body != null && handler.Body.Type != typeof(void))
                {
                    throw Error.BodyOfCatchMustHaveSameTypeAsBodyOfTry();
                }
            }
        }
        else
        {
            type = tryBody.Type;
            foreach (var handler in handlers)
            {
                if (handler.Body == null || !TypeUtils.AreEquivalent(handler.Body.Type, type))
                {
                    throw Error.BodyOfCatchMustHaveSameTypeAsBodyOfTry();
                }
            }
        }
    }

    private static bool ValidateTryGetFuncActionArgs(Type[] typeArgs)
    {
        if (typeArgs == null)
        {
            throw new ArgumentNullException(nameof(typeArgs));
        }

        var index = 0;
        for (var length = typeArgs.Length; index < length; ++index)
        {
            var typeArg = typeArgs[index];
            if (typeArg == null)
            {
                throw new ArgumentNullException(nameof(typeArgs));
            }

            if (typeArg.IsByRef)
            {
                return false;
            }
        }

        return true;
    }

    private static void ValidateUserDefinedConditionalLogicOperator(
        ExpressionType nodeType,
        Type left,
        Type right,
        MethodInfo method)
    {
        ValidateOperator(method);
        var parametersCached = method.GetParameters();
        if (parametersCached.Length != 2)
        {
            throw Error.IncorrectNumberOfMethodCallArguments(method);
        }

        if (!ParameterIsAssignable(parametersCached[0], left) && (!left.IsNullableType() ||
                                                                  !ParameterIsAssignable(parametersCached[0],
                                                                      left.GetNonNullableType())))
        {
            throw Error.OperandTypesDoNotMatchParameters(nodeType, method.Name);
        }

        if (!ParameterIsAssignable(parametersCached[1], right) && (!right.IsNullableType() ||
                                                                   !ParameterIsAssignable(parametersCached[1],
                                                                       right.GetNonNullableType())))
        {
            throw Error.OperandTypesDoNotMatchParameters(nodeType, method.Name);
        }

        if (parametersCached[0].ParameterType != parametersCached[1].ParameterType)
        {
            throw Error.UserDefinedOpMustHaveConsistentTypes(nodeType, method.Name);
        }

        if (method.ReturnType != parametersCached[0].ParameterType)
        {
            throw Error.UserDefinedOpMustHaveConsistentTypes(nodeType, method.Name);
        }

        if (IsValidLiftedConditionalLogicalOperator(left, right, parametersCached))
        {
            left = left.GetNonNullableType();
            right = left.GetNonNullableType();
        }

        var booleanOperator1 = TypeUtils.GetBooleanOperator(method.DeclaringType, "op_True");
        var booleanOperator2 = TypeUtils.GetBooleanOperator(method.DeclaringType, "op_False");
        if (booleanOperator1 == null || booleanOperator1.ReturnType != typeof(bool) || booleanOperator2 == null ||
            booleanOperator2.ReturnType != typeof(bool))
        {
            throw Error.LogicalOperatorMustHaveBooleanOperators(nodeType, method.Name);
        }

        VerifyOpTrueFalse(nodeType, left, booleanOperator2);
        VerifyOpTrueFalse(nodeType, left, booleanOperator1);
    }

    private static void VerifyOpTrueFalse(ExpressionType nodeType, Type left, MethodInfo opTrue)
    {
        var parametersCached = opTrue.GetParameters();
        if (parametersCached.Length != 1)
        {
            throw Error.IncorrectNumberOfMethodCallArguments(opTrue);
        }

        if (!ParameterIsAssignable(parametersCached[0], left) && (!left.IsNullableType() ||
                                                                  !ParameterIsAssignable(parametersCached[0],
                                                                      left.GetNonNullableType())))
        {
            throw Error.OperandTypesDoNotMatchParameters(nodeType, opTrue.Name);
        }
    }

    private delegate LambdaExpression LambdaFactory(
        Expression body,
        string name,
        bool tailCall,
        ReadOnlyCollection<ParameterExpression> parameters);

    private class ExtensionInfo
    {
        internal readonly ExpressionType NodeType;
        internal readonly Type Type;

        public ExtensionInfo(ExpressionType nodeType, Type type)
        {
            NodeType = nodeType;
            Type = type;
        }
    }

    internal class BinaryExpressionProxy
    {
        private readonly BinaryExpression _node;

        public BinaryExpressionProxy(BinaryExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public LambdaExpression Conversion => _node.Conversion;

        public string DebugView => _node.DebugView;

        public bool IsLifted => _node.IsLifted;

        public bool IsLiftedToNull => _node.IsLiftedToNull;

        public Expression Left => _node.Left;

        public MethodInfo Method => _node.Method;

        public ExpressionType NodeType => _node.NodeType;

        public Expression Right => _node.Right;

        public Type Type => _node.Type;
    }

    internal class BlockExpressionProxy
    {
        private readonly BlockExpression _node;

        public BlockExpressionProxy(BlockExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public ReadOnlyCollection<Expression> Expressions => _node.Expressions;

        public ExpressionType NodeType => _node.NodeType;

        public Expression Result => _node.Result;

        public Type Type => _node.Type;

        public ReadOnlyCollection<ParameterExpression> Variables => _node.Variables;
    }

    internal class CatchBlockProxy
    {
        private readonly CatchBlock _node;

        public CatchBlockProxy(CatchBlock node)
        {
            _node = node;
        }

        public Expression Body => _node.Body;

        public Expression Filter => _node.Filter;

        public Type Test => _node.Test;

        public ParameterExpression Variable => _node.Variable;
    }

    internal class ConditionalExpressionProxy
    {
        private readonly ConditionalExpression _node;

        public ConditionalExpressionProxy(ConditionalExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public Expression IfFalse => _node.IfFalse;

        public Expression IfTrue => _node.IfTrue;

        public ExpressionType NodeType => _node.NodeType;

        public Expression Test => _node.Test;

        public Type Type => _node.Type;
    }

    internal class ConstantExpressionProxy
    {
        private readonly ConstantExpression _node;

        public ConstantExpressionProxy(ConstantExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;

        public object Value => _node.Value;
    }

    internal class DebugInfoExpressionProxy
    {
        private readonly DebugInfoExpression _node;

        public DebugInfoExpressionProxy(DebugInfoExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public SymbolDocumentInfo Document => _node.Document;

        public int EndColumn => _node.EndColumn;

        public int EndLine => _node.EndLine;

        public bool IsClear => _node.IsClear;

        public ExpressionType NodeType => _node.NodeType;

        public int StartColumn => _node.StartColumn;

        public int StartLine => _node.StartLine;

        public Type Type => _node.Type;
    }

    internal class DefaultExpressionProxy
    {
        private readonly DefaultExpression _node;

        public DefaultExpressionProxy(DefaultExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;
    }

    internal class DynamicExpressionProxy
    {
        private readonly DynamicExpression _node;

        public DynamicExpressionProxy(DynamicExpression node)
        {
            _node = node;
        }

        public ReadOnlyCollection<Expression> Arguments => _node.Arguments;

        public CallSiteBinder Binder => _node.Binder;

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public Type DelegateType => _node.DelegateType;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;
    }

    internal class GotoExpressionProxy
    {
        private readonly GotoExpression _node;

        public GotoExpressionProxy(GotoExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public GotoExpressionKind Kind => _node.Kind;

        public ExpressionType NodeType => _node.NodeType;

        public LabelTarget Target => _node.Target;

        public Type Type => _node.Type;

        public Expression Value => _node.Value;
    }

    internal class IndexExpressionProxy
    {
        private readonly IndexExpression _node;

        public IndexExpressionProxy(IndexExpression node)
        {
            _node = node;
        }

        public ReadOnlyCollection<Expression> Arguments => _node.Arguments;

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public PropertyInfo Indexer => _node.Indexer;

        public ExpressionType NodeType => _node.NodeType;

        public Expression Object => _node.Object;

        public Type Type => _node.Type;
    }

    internal class InvocationExpressionProxy
    {
        private readonly InvocationExpression _node;

        public InvocationExpressionProxy(InvocationExpression node)
        {
            _node = node;
        }

        public ReadOnlyCollection<Expression> Arguments => _node.Arguments;

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public Expression Expression => _node.Expression;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;
    }

    internal class LabelExpressionProxy
    {
        private readonly LabelExpression _node;

        public LabelExpressionProxy(LabelExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public Expression DefaultValue => _node.DefaultValue;

        public ExpressionType NodeType => _node.NodeType;

        public LabelTarget Target => _node.Target;

        public Type Type => _node.Type;
    }

    internal class LambdaExpressionProxy
    {
        private readonly LambdaExpression _node;

        public LambdaExpressionProxy(LambdaExpression node)
        {
            _node = node;
        }

        public Expression Body => _node.Body;

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public string Name => _node.Name;

        public ExpressionType NodeType => _node.NodeType;

        public ReadOnlyCollection<ParameterExpression> Parameters => _node.Parameters;

        public Type ReturnType => _node.ReturnType;

        public bool TailCall => _node.TailCall;

        public Type Type => _node.Type;
    }

    internal class ListInitExpressionProxy
    {
        private readonly ListInitExpression _node;

        public ListInitExpressionProxy(ListInitExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public ReadOnlyCollection<ElementInit> Initializers => _node.Initializers;

        public NewExpression NewExpression => _node.NewExpression;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;
    }

    internal class LoopExpressionProxy
    {
        private readonly LoopExpression _node;

        public LoopExpressionProxy(LoopExpression node)
        {
            _node = node;
        }

        public Expression Body => _node.Body;

        public LabelTarget BreakLabel => _node.BreakLabel;

        public bool CanReduce => _node.CanReduce;

        public LabelTarget ContinueLabel => _node.ContinueLabel;

        public string DebugView => _node.DebugView;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;
    }

    internal class MemberExpressionProxy
    {
        private readonly MemberExpression _node;

        public MemberExpressionProxy(MemberExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public Expression Expression => _node.Expression;

        public MemberInfo Member => _node.Member;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;
    }

    internal class MemberInitExpressionProxy
    {
        private readonly MemberInitExpression _node;

        public MemberInitExpressionProxy(MemberInitExpression node)
        {
            _node = node;
        }

        public ReadOnlyCollection<MemberBinding> Bindings => _node.Bindings;

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public NewExpression NewExpression => _node.NewExpression;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;
    }

    internal class MethodCallExpressionProxy
    {
        private readonly MethodCallExpression _node;

        public MethodCallExpressionProxy(MethodCallExpression node)
        {
            _node = node;
        }

        public ReadOnlyCollection<Expression> Arguments => _node.Arguments;

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public MethodInfo Method => _node.Method;

        public ExpressionType NodeType => _node.NodeType;

        public Expression Object => _node.Object;

        public Type Type => _node.Type;
    }

    internal class NewArrayExpressionProxy
    {
        private readonly NewArrayExpression _node;

        public NewArrayExpressionProxy(NewArrayExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public ReadOnlyCollection<Expression> Expressions => _node.Expressions;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;
    }

    internal class NewExpressionProxy
    {
        private readonly NewExpression _node;

        public NewExpressionProxy(NewExpression node)
        {
            _node = node;
        }

        public ReadOnlyCollection<Expression> Arguments => _node.Arguments;

        public bool CanReduce => _node.CanReduce;

        public ConstructorInfo Constructor => _node.Constructor;

        public string DebugView => _node.DebugView;

        public ReadOnlyCollection<MemberInfo> Members => _node.Members;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;
    }

    internal class ParameterExpressionProxy
    {
        private readonly ParameterExpression _node;

        public ParameterExpressionProxy(ParameterExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public bool IsByRef => _node.IsByRef;

        public string Name => _node.Name;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;
    }

    internal class RuntimeVariablesExpressionProxy
    {
        private readonly RuntimeVariablesExpression _node;

        public RuntimeVariablesExpressionProxy(RuntimeVariablesExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;

        public ReadOnlyCollection<ParameterExpression> Variables => _node.Variables;
    }

    internal class SwitchCaseProxy
    {
        private readonly SwitchCase _node;

        public SwitchCaseProxy(SwitchCase node)
        {
            _node = node;
        }

        public Expression Body => _node.Body;

        public ReadOnlyCollection<Expression> TestValues => _node.TestValues;
    }

    internal class SwitchExpressionProxy
    {
        private readonly SwitchExpression _node;

        public SwitchExpressionProxy(SwitchExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public ReadOnlyCollection<SwitchCase> Cases => _node.Cases;

        public MethodInfo Comparison => _node.Comparison;

        public string DebugView => _node.DebugView;

        public Expression DefaultBody => _node.DefaultBody;

        public ExpressionType NodeType => _node.NodeType;

        public Expression SwitchValue => _node.SwitchValue;

        public Type Type => _node.Type;
    }

    internal class TryExpressionProxy
    {
        private readonly TryExpression _node;

        public TryExpressionProxy(TryExpression node)
        {
            _node = node;
        }

        public Expression Body => _node.Body;

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public Expression Fault => _node.Fault;

        public Expression Finally => _node.Finally;

        public ReadOnlyCollection<CatchBlock> Handlers => _node.Handlers;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;
    }

    internal class TypeBinaryExpressionProxy
    {
        private readonly TypeBinaryExpression _node;

        public TypeBinaryExpressionProxy(TypeBinaryExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public Expression Expression => _node.Expression;

        public ExpressionType NodeType => _node.NodeType;

        public Type Type => _node.Type;

        public Type TypeOperand => _node.TypeOperand;
    }

    internal class UnaryExpressionProxy
    {
        private readonly UnaryExpression _node;

        public UnaryExpressionProxy(UnaryExpression node)
        {
            _node = node;
        }

        public bool CanReduce => _node.CanReduce;

        public string DebugView => _node.DebugView;

        public bool IsLifted => _node.IsLifted;

        public bool IsLiftedToNull => _node.IsLiftedToNull;

        public MethodInfo Method => _node.Method;

        public ExpressionType NodeType => _node.NodeType;

        public Expression Operand => _node.Operand;

        public Type Type => _node.Type;
    }
}