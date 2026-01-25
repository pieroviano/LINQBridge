#nullable disable
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions.Compiler;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions;

/// <summary>
///     Represents a strongly typed lambda expression as a data structure in the form of an expression tree. This
///     class cannot be inherited.
/// </summary>
/// <typeparam name="TDelegate">
///     The type of the delegate that the <see cref="T:System.Linq.Expressions.Expression`1" />
///     represents.
/// </typeparam>
public sealed class Expression<TDelegate> : LambdaExpression
{
    internal Expression(
        Expression body,
        string name,
        bool tailCall,
        ReadOnlyCollection<ParameterExpression> parameters)
        : base(typeof(TDelegate), name, body, tailCall, parameters)
    {
    }

    /// <summary>
    ///     Compiles the lambda expression described by the expression tree into executable code and produces a delegate
    ///     that represents the lambda expression.
    /// </summary>
    /// <returns>
    ///     A delegate of type <paramref name="TDelegate" /> that represents the compiled lambda expression described by
    ///     the <see cref="T:System.Linq.Expressions.Expression`1" />.
    /// </returns>
    public new TDelegate Compile()
    {
        return (TDelegate)(object)LambdaCompiler.Compile(this, null);
    }

    /// <summary>Produces a delegate that represents the lambda expression.</summary>
    /// <returns>A delegate containing the compiled version of the lambda.</returns>
    /// <param name="debugInfoGenerator">
    ///     Debugging information generator used by the compiler to mark sequence points and
    ///     annotate local variables.
    /// </param>
    public new TDelegate Compile(DebugInfoGenerator debugInfoGenerator)
    {
        ContractUtils.RequiresNotNull(debugInfoGenerator, nameof(debugInfoGenerator));
        return (TDelegate)(object)LambdaCompiler.Compile(this, debugInfoGenerator);
    }

    public new TDelegate Compile(bool preferInterpretation)
    {
        return Compile();
    }

    /// <summary>
    ///     Creates a new expression that is like this one, but using the supplied children. If all of the children are
    ///     the same, it will return this expression.
    /// </summary>
    /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
    /// <param name="body">The <see cref="P:System.Linq.Expressions.LambdaExpression.Body" /> property of the result.</param>
    /// <param name="parameters">
    ///     The <see cref="P:System.Linq.Expressions.LambdaExpression.Parameters" /> property of the
    ///     result.
    /// </param>
    public Expression<TDelegate> Update(Expression body, IEnumerable<ParameterExpression> parameters)
    {
        return body == Body && parameters == Parameters ? this : Lambda<TDelegate>(body, Name, TailCall, parameters);
    }

    protected internal override Expression Accept(ExpressionVisitor visitor)
    {
        return visitor.VisitLambda(this);
    }

    internal override LambdaExpression Accept(StackSpiller spiller)
    {
        return spiller.Rewrite(this);
    }

    internal static LambdaExpression Create(
        Expression body,
        string name,
        bool tailCall,
        ReadOnlyCollection<ParameterExpression> parameters)
    {
        return new Expression<TDelegate>(body, name, tailCall, parameters);
    }
}