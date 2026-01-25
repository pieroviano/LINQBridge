#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

/// <summary>Represents calling a constructor and initializing one or more members of the new object.</summary>
[DebuggerTypeProxy(typeof(MemberInitExpressionProxy))]
public sealed class MemberInitExpression : Expression
{
    internal MemberInitExpression(
        NewExpression newExpression,
        ReadOnlyCollection<MemberBinding> bindings)
    {
        NewExpression = newExpression;
        Bindings = bindings;
    }

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.MemberInitExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type => NewExpression.Type;

    /// <summary>Gets a value that indicates whether the expression tree node can be reduced.</summary>
    /// <returns>True if the node can be reduced, otherwise false.</returns>
    public override bool CanReduce => true;

    /// <summary>
    ///     Returns the node type of this Expression. Extension nodes should return
    ///     <see cref="F:System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.
    /// </summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> of the expression.</returns>
    public override ExpressionType NodeType => ExpressionType.MemberInit;

    /// <summary>Gets the expression that represents the constructor call.</summary>
    /// <returns>A <see cref="T:System.Linq.Expressions.NewExpression" /> that represents the constructor call.</returns>
    public NewExpression NewExpression { get; }

    /// <summary>Gets the bindings that describe how to initialize the members of the newly created object.</summary>
    /// <returns>
    ///     A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of
    ///     <see cref="T:System.Linq.Expressions.MemberBinding" /> objects which describe how to initialize the members.
    /// </returns>
    public ReadOnlyCollection<MemberBinding> Bindings { get; }

    /// <summary>Reduces the <see cref="T:System.Linq.Expressions.MemberInitExpression" /> to a simpler expression. </summary>
    /// <returns>The reduced expression.</returns>
    public override Expression Reduce()
    {
        return ReduceMemberInit(NewExpression, Bindings, true);
    }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="newExpression">
    ///     The <see cref="P:System.Linq.Expressions.MemberInitExpression.NewExpression" /> property of
    ///     the result.
    /// </param>
    /// <param name="bindings">
    ///     The <see cref="P:System.Linq.Expressions.MemberInitExpression.Bindings" /> property of the
    ///     result.
    /// </param>
    public MemberInitExpression Update(
        NewExpression newExpression,
        IEnumerable<MemberBinding> bindings)
    {
        return newExpression == NewExpression && bindings == Bindings ? this : MemberInit(newExpression, bindings);
    }

    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitMemberInit(this);
    }

    internal static Expression ReduceListInit(
        Expression listExpression,
        ReadOnlyCollection<ElementInit> initializers,
        bool keepOnStack)
    {
        var parameterExpression = Variable(listExpression.Type, null);
        var count = initializers.Count;
        var list = new Expression[count + 2];
        list[0] = Assign(parameterExpression, listExpression);
        for (var index = 0; index < count; ++index)
        {
            var initializer = initializers[index];
            list[index + 1] = Call(parameterExpression, initializer.AddMethod, initializer.Arguments);
        }

        list[count + 1] = keepOnStack ? parameterExpression : Empty();
        return Block(new TrueReadOnlyCollection<Expression>(list));
    }

    internal static Expression ReduceMemberBinding(ParameterExpression objVar, MemberBinding binding)
    {
        var memberExpression = MakeMemberAccess(objVar, binding.Member);
        switch (binding.BindingType)
        {
            case MemberBindingType.Assignment:
                return Assign(memberExpression, ((MemberAssignment)binding).Expression);
            case MemberBindingType.MemberBinding:
                return ReduceMemberInit(memberExpression, ((MemberMemberBinding)binding).Bindings, false);
            case MemberBindingType.ListBinding:
                return ReduceListInit(memberExpression, ((MemberListBinding)binding).Initializers, false);
            default:
                throw ContractUtils.Unreachable;
        }
    }

    internal static Expression ReduceMemberInit(
        Expression objExpression,
        ReadOnlyCollection<MemberBinding> bindings,
        bool keepOnStack)
    {
        var parameterExpression = Variable(objExpression.Type, null);
        var count = bindings.Count;
        var list = new Expression[count + 2];
        list[0] = Assign(parameterExpression, objExpression);
        for (var index = 0; index < count; ++index)
        {
            list[index + 1] = ReduceMemberBinding(parameterExpression, bindings[index]);
        }

        list[count + 1] = keepOnStack ? parameterExpression : Empty();
        return Block(new TrueReadOnlyCollection<Expression>(list));
    }
}