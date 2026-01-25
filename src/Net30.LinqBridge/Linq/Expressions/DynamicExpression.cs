#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

/// <summary>Represents a dynamic operation.</summary>
[DebuggerTypeProxy(typeof(DynamicExpressionProxy))]
public class DynamicExpression : Expression, IDynamicExpression, IArgumentProvider
{
    internal DynamicExpression(Type delegateType, CallSiteBinder binder)
    {
        DelegateType = delegateType;
        Binder = binder;
    }

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.DynamicExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type => typeof(object);

    /// <summary>
    ///     Returns the node type of this expression. Extension nodes should return
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.
    /// </summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> of the expression.</returns>
    public sealed override ExpressionType NodeType => ExpressionType.Dynamic;

    /// <summary>
    ///     Gets the <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />, which determines the runtime
    ///     behavior of the dynamic site.
    /// </summary>
    /// <returns>
    ///     The <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" />, which determines the runtime behavior of
    ///     the dynamic site.
    /// </returns>
    public CallSiteBinder Binder { get; }

    /// <summary>Gets the arguments to the dynamic operation.</summary>
    /// <returns>The read-only collections containing the arguments to the dynamic operation.</returns>
    public ReadOnlyCollection<Expression> Arguments => GetOrMakeArguments();

    /// <summary>Gets the type of the delegate used by the <see cref="T:System.Runtime.CompilerServices.CallSite" />.</summary>
    /// <returns>
    ///     The <see cref="T:System.Type" /> object representing the type of the delegate used by the
    ///     <see cref="T:System.Runtime.CompilerServices.CallSite" />.
    /// </returns>
    public Type DelegateType { get; }

    Expression IArgumentProvider.GetArgument(int index)
    {
        throw ContractUtils.Unreachable;
    }

    int IArgumentProvider.ArgumentCount => throw ContractUtils.Unreachable;

    Expression IDynamicExpression.Rewrite(Expression[] args)
    {
        return Rewrite(args);
    }

    object IDynamicExpression.CreateCallSite()
    {
        return CallSite.Create(DelegateType, Binder);
    }

    public new static DynamicExpression Dynamic(
        CallSiteBinder binder,
        Type returnType,
        params Expression[] arguments)
    {
        return Expression.Dynamic(binder, returnType, arguments);
    }

    public new static DynamicExpression Dynamic(
        CallSiteBinder binder,
        Type returnType,
        IEnumerable<Expression> arguments)
    {
        return Expression.Dynamic(binder, returnType, arguments);
    }

    public new static DynamicExpression Dynamic(
        CallSiteBinder binder,
        Type returnType,
        Expression arg0)
    {
        return Expression.Dynamic(binder, returnType, arg0);
    }

    public new static DynamicExpression Dynamic(
        CallSiteBinder binder,
        Type returnType,
        Expression arg0,
        Expression arg1)
    {
        return Expression.Dynamic(binder, returnType, arg0, arg1);
    }

    public new static DynamicExpression Dynamic(
        CallSiteBinder binder,
        Type returnType,
        Expression arg0,
        Expression arg1,
        Expression arg2)
    {
        return Expression.Dynamic(binder, returnType, arg0, arg1, arg2);
    }

    public new static DynamicExpression Dynamic(
        CallSiteBinder binder,
        Type returnType,
        Expression arg0,
        Expression arg1,
        Expression arg2,
        Expression arg3)
    {
        return Expression.Dynamic(binder, returnType, arg0, arg1, arg2, arg3);
    }

    public new static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        IEnumerable<Expression> arguments)
    {
        return Expression.MakeDynamic(delegateType, binder, arguments);
    }

    public new static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        params Expression[] arguments)
    {
        return Expression.MakeDynamic(delegateType, binder, arguments);
    }

    public new static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0)
    {
        return Expression.MakeDynamic(delegateType, binder, arg0);
    }

    public new static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0,
        Expression arg1)
    {
        return Expression.MakeDynamic(delegateType, binder, arg0, arg1);
    }

    public new static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0,
        Expression arg1,
        Expression arg2)
    {
        return Expression.MakeDynamic(delegateType, binder, arg0, arg1, arg2);
    }

    public new static DynamicExpression MakeDynamic(
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0,
        Expression arg1,
        Expression arg2,
        Expression arg3)
    {
        return Expression.MakeDynamic(delegateType, binder, arg0, arg1, arg2, arg3);
    }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="arguments">
    ///     The <see cref="P:System.Linq.Expressions.DynamicExpression.Arguments" /> property of the
    ///     result.
    /// </param>
    public DynamicExpression Update(IEnumerable<Expression> arguments)
    {
        return arguments == Arguments ? this : Expression.MakeDynamic(DelegateType, Binder, arguments);
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
        return visitor.VisitDynamic(this);
    }

    internal virtual ReadOnlyCollection<Expression> GetOrMakeArguments()
    {
        throw ContractUtils.Unreachable;
    }

    internal static DynamicExpression Make(
        Type returnType,
        Type delegateType,
        CallSiteBinder binder,
        ReadOnlyCollection<Expression> arguments)
    {
        return returnType == typeof(object)
            ? new DynamicExpressionN(delegateType, binder, arguments)
            : (DynamicExpression)new TypedDynamicExpressionN(returnType, delegateType, binder, arguments);
    }

    internal static DynamicExpression Make(
        Type returnType,
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0)
    {
        return returnType == typeof(object)
            ? new DynamicExpression1(delegateType, binder, arg0)
            : (DynamicExpression)new TypedDynamicExpression1(returnType, delegateType, binder, arg0);
    }

    internal static DynamicExpression Make(
        Type returnType,
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0,
        Expression arg1)
    {
        return returnType == typeof(object)
            ? new DynamicExpression2(delegateType, binder, arg0, arg1)
            : (DynamicExpression)new TypedDynamicExpression2(returnType, delegateType, binder, arg0, arg1);
    }

    internal static DynamicExpression Make(
        Type returnType,
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0,
        Expression arg1,
        Expression arg2)
    {
        return returnType == typeof(object)
            ? new DynamicExpression3(delegateType, binder, arg0, arg1, arg2)
            : (DynamicExpression)new TypedDynamicExpression3(returnType, delegateType, binder, arg0, arg1, arg2);
    }

    internal static DynamicExpression Make(
        Type returnType,
        Type delegateType,
        CallSiteBinder binder,
        Expression arg0,
        Expression arg1,
        Expression arg2,
        Expression arg3)
    {
        return returnType == typeof(object)
            ? new DynamicExpression4(delegateType, binder, arg0, arg1, arg2, arg3)
            : (DynamicExpression)new TypedDynamicExpression4(returnType, delegateType, binder, arg0, arg1, arg2, arg3);
    }

    internal virtual DynamicExpression Rewrite(Expression[] args)
    {
        throw ContractUtils.Unreachable;
    }
}