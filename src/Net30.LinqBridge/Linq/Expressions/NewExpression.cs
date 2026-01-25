#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace System.Linq.Expressions;

/// <summary>Represents a constructor call.</summary>
[DebuggerTypeProxy(typeof(NewExpressionProxy))]
public class NewExpression : Expression, IArgumentProvider
{
    private IList<Expression> _arguments;

    internal NewExpression(
        ConstructorInfo constructor,
        IList<Expression> arguments,
        ReadOnlyCollection<MemberInfo> members)
    {
        Constructor = constructor;
        _arguments = arguments;
        Members = members;
    }

    /// <summary>
    ///     Gets the static type of the expression that this <see cref="T:System.Linq.Expressions.Expression" />
    ///     represents.
    /// </summary>
    /// <returns>
    ///     The <see cref="P:System.Linq.Expressions.NewExpression.Type" /> that represents the static type of the
    ///     expression.
    /// </returns>
    public override Type Type => Constructor.DeclaringType;

    /// <summary>Returns the node type of this <see cref="T:System.Linq.Expressions.Expression" />.</summary>
    /// <returns>The <see cref="T:System.Linq.Expressions.ExpressionType" /> that represents this expression.</returns>
    public sealed override ExpressionType NodeType => ExpressionType.New;

    /// <summary>Gets the called constructor.</summary>
    /// <returns>The <see cref="T:System.Reflection.ConstructorInfo" /> that represents the called constructor.</returns>
    public ConstructorInfo Constructor { get; }

    /// <summary>Gets the arguments to the constructor.</summary>
    /// <returns>
    ///     A collection of <see cref="T:System.Linq.Expressions.Expression" /> objects that represent the arguments to
    ///     the constructor.
    /// </returns>
    public ReadOnlyCollection<Expression> Arguments => ReturnReadOnly(ref _arguments);

    /// <summary>Gets the members that can retrieve the values of the fields that were initialized with constructor arguments.</summary>
    /// <returns>
    ///     A collection of <see cref="T:System.Reflection.MemberInfo" /> objects that represent the members that can
    ///     retrieve the values of the fields that were initialized with constructor arguments.
    /// </returns>
    public ReadOnlyCollection<MemberInfo> Members { get; }

    Expression IArgumentProvider.GetArgument(int index)
    {
        return _arguments[index];
    }

    int IArgumentProvider.ArgumentCount => _arguments.Count;

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="arguments">The <see cref="P:System.Linq.Expressions.NewExpression.Arguments" /> property of the result.</param>
    public NewExpression Update(IEnumerable<Expression> arguments)
    {
        if (arguments == Arguments)
        {
            return this;
        }

        return Members != null ? New(Constructor, arguments, Members) : New(Constructor, arguments);
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
        return visitor.VisitNew(this);
    }
}