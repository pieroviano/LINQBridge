#nullable disable
using System.Diagnostics;

namespace System.Linq.Expressions;

/// <summary>Represents a named parameter expression.</summary>
[DebuggerTypeProxy(typeof(ParameterExpressionProxy))]
public class ParameterExpression : Expression
{
    internal ParameterExpression(string name)
    {
        Name = name;
    }

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.ParameterExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type => typeof(object);

    /// <summary>Returns the node type of this <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> that represents this expression.</returns>
    public sealed override ExpressionType NodeType => ExpressionType.Parameter;

    /// <summary>Gets the name of the parameter or variable.</summary>
    /// <returns>A <see cref="T:System.String" /> that contains the name of the parameter.</returns>
    public string Name { get; }

    /// <summary>Indicates that this ParameterExpression is to be treated as a ByRef parameter.</summary>
    /// <returns>True if this ParameterExpression is a ByRef parameter, otherwise false.</returns>
    public bool IsByRef => GetIsByRef();

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
        return visitor.VisitParameter(this);
    }

    internal virtual bool GetIsByRef()
    {
        return false;
    }

    internal static ParameterExpression Make(Type type, string name, bool isByRef)
    {
        if (isByRef)
        {
            return new ByRefParameterExpression(type, name);
        }

        if (!type.IsEnum)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    if (type == typeof(object))
                    {
                        return new ParameterExpression(name);
                    }

                    if (type == typeof(Exception))
                    {
                        return new PrimitiveParameterExpression<Exception>(name);
                    }

                    if (type == typeof(object[]))
                    {
                        return new PrimitiveParameterExpression<object[]>(name);
                    }

                    break;
                case TypeCode.DBNull:
                    return new PrimitiveParameterExpression<DBNull>(name);
                case TypeCode.Boolean:
                    return new PrimitiveParameterExpression<bool>(name);
                case TypeCode.Char:
                    return new PrimitiveParameterExpression<char>(name);
                case TypeCode.SByte:
                    return new PrimitiveParameterExpression<sbyte>(name);
                case TypeCode.Byte:
                    return new PrimitiveParameterExpression<byte>(name);
                case TypeCode.Int16:
                    return new PrimitiveParameterExpression<short>(name);
                case TypeCode.UInt16:
                    return new PrimitiveParameterExpression<ushort>(name);
                case TypeCode.Int32:
                    return new PrimitiveParameterExpression<int>(name);
                case TypeCode.UInt32:
                    return new PrimitiveParameterExpression<uint>(name);
                case TypeCode.Int64:
                    return new PrimitiveParameterExpression<long>(name);
                case TypeCode.UInt64:
                    return new PrimitiveParameterExpression<ulong>(name);
                case TypeCode.Single:
                    return new PrimitiveParameterExpression<float>(name);
                case TypeCode.Double:
                    return new PrimitiveParameterExpression<double>(name);
                case TypeCode.Decimal:
                    return new PrimitiveParameterExpression<decimal>(name);
                case TypeCode.DateTime:
                    return new PrimitiveParameterExpression<DateTime>(name);
                case TypeCode.String:
                    return new PrimitiveParameterExpression<string>(name);
            }
        }

        return new TypedParameterExpression(type, name);
    }
}