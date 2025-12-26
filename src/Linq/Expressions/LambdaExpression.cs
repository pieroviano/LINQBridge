using System.Collections.ObjectModel;
using System.Text;

namespace System.Linq.Expressions;

/// <summary>Describes a lambda expression.</summary>
public class LambdaExpression : Expression
{
    private readonly ReadOnlyCollection<ParameterExpression> parameters;
    private readonly Expression body;

    internal LambdaExpression(
        Expression body,
        Type type,
        ReadOnlyCollection<ParameterExpression> parameters)
        : base(ExpressionType.Lambda, type)
    {
        this.body = body;
        this.parameters = parameters;
    }

    /// <summary>Gets the body of the lambda expression.</summary>
    /// <returns>An <see cref="T:System.Linq.Expressions.Expression" /> that represents the body of the lambda expression.</returns>
    public Expression Body => body;

    /// <summary>Gets the parameters of the lambda expression.</summary>
    /// <returns>A <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects that represent the parameters of the lambda expression.</returns>
    public ReadOnlyCollection<ParameterExpression> Parameters => parameters;

    internal override void BuildString(StringBuilder builder)
    {
        if (Parameters.Count == 1)
        {
            Parameters[0].BuildString(builder);
        }
        else
        {
            builder.Append("(");
            var index = 0;
            for (var count = Parameters.Count; index < count; ++index)
            {
                if (index > 0)
                    builder.Append(", ");
                Parameters[index].BuildString(builder);
            }
            builder.Append(")");
        }
        builder.Append(" => ");
        body.BuildString(builder);
    }

    /// <summary>Produces a delegate that represents the lambda expression.</summary>
    /// <returns>A <see cref="T:System.Delegate" /> that, when it is executed, has the behavior described by the semantics of the <see cref="T:System.Linq.Expressions.LambdaExpression" />.</returns>
    public Delegate Compile() => new ExpressionCompiler().Compile(this);
}