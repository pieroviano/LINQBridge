using System.Text;

namespace System.Linq.Expressions;

/// <summary>Represents an expression that has a conditional operator.</summary>
public sealed class ConditionalExpression : Expression
{
    private readonly Expression test;
    private readonly Expression ifTrue;
    private readonly Expression ifFalse;

    internal ConditionalExpression(
        Expression test,
        Expression ifTrue,
        Expression ifFalse,
        Type type)
        : base(ExpressionType.Conditional, type)
    {
        this.test = test;
        this.ifTrue = ifTrue;
        this.ifFalse = ifFalse;
    }

    /// <summary>Gets the test of the conditional operation.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression" /> that represents the test of the conditional operation.</returns>
    public Expression Test => test;

    /// <summary>Gets the expression to execute if the test evaluates to true.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression" /> that represents the expression to execute if the test is true.</returns>
    public Expression IfTrue => ifTrue;

    /// <summary>Gets the expression to execute if the test evaluates to false.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression" /> that represents the expression to execute if the test is false.</returns>
    public Expression IfFalse => ifFalse;

    internal override void BuildString(StringBuilder builder)
    {
        if (builder == null)
            throw Error.ArgumentNull(nameof(builder));
        builder.Append("IIF(");
        test.BuildString(builder);
        builder.Append(", ");
        ifTrue.BuildString(builder);
        builder.Append(", ");
        ifFalse.BuildString(builder);
        builder.Append(")");
    }
}