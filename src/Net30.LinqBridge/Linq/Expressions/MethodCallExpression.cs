#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

/// <summary>Represents a call to either static or an instance method.</summary>
[DebuggerTypeProxy(typeof(MethodCallExpressionProxy))]
public class MethodCallExpression : Expression, IArgumentProvider
{
    internal MethodCallExpression(MethodInfo method)
    {
        Method = method;
    }

    /// <summary>Returns the node type of this <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> that represents this expression.</returns>
    public sealed override ExpressionType NodeType => ExpressionType.Call;

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.MethodCallExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public sealed override Type Type => Method.ReturnType;

    /// <summary>Gets the <see cref="T:System.Reflection.MethodInfo" /> for the method to be called.</summary>
    /// <returns>The <see cref="T:System.Reflection.MethodInfo" /> that represents the called method.</returns>
    public MethodInfo Method { get; }

    /// <summary>
    ///     Gets the <see cref="T:System.Linq.Expressions.Expression" /> that represents the instance for instance method
    ///     calls or null for static method calls.
    /// </summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression" /> that represents the receiving object of the method.</returns>
    public Expression Object => GetInstance();

    /// <summary>Gets a collection of expressions that represent arguments of the called method.</summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of
    ///     <see cref="T:System.Linq.Expressions.Expression" /> objects which represent the arguments to the called method.
    /// </returns>
    public ReadOnlyCollection<Expression> Arguments => GetOrMakeArguments();

    Expression IArgumentProvider.GetArgument(int index)
    {
        throw ContractUtils.Unreachable;
    }

    int IArgumentProvider.ArgumentCount => throw ContractUtils.Unreachable;

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="object">The <see cref="P:System.Linq.Expressions.MethodCallExpression.Object" /> property of the result.</param>
    /// <param name="arguments">
    ///     The <see cref="P:System.Linq.Expressions.MethodCallExpression.Arguments" /> property of the
    ///     result.
    /// </param>
    public MethodCallExpression Update(Expression @object, IEnumerable<Expression> arguments)
    {
        return @object == Object && arguments == Arguments ? this : Call(@object, Method, arguments);
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
        return visitor.VisitMethodCall(this);
    }

    internal virtual Expression GetInstance()
    {
        return null;
    }

    internal virtual ReadOnlyCollection<Expression> GetOrMakeArguments()
    {
        throw ContractUtils.Unreachable;
    }

    internal virtual MethodCallExpression Rewrite(Expression instance, IList<Expression> args)
    {
        throw ContractUtils.Unreachable;
    }
}