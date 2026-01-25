using System;
using System.Text;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a try/finally expression (ExpressionType.Try).
    /// Minimal implementation to support Expression.TryFinally(tryBody, finallyBody).
    /// </summary>
    public sealed class TryExpression : Expression
    {
        internal TryExpression(Expression tryBody, Expression finallyBody)
            : base(ExpressionType.Try, tryBody?.Type ?? typeof(void))
        {
            Try = tryBody ?? throw Error.ArgumentNull(nameof(tryBody));
            Finally = finallyBody ?? throw Error.ArgumentNull(nameof(finallyBody));
        }

        /// <summary>Gets the expression representing the try block.</summary>
        public Expression Try { get; }

        /// <summary>Gets the expression representing the finally block (always void).</summary>
        public Expression Finally { get; }

        public override ExpressionType NodeType => ExpressionType.Try;

        internal override void BuildString(StringBuilder builder)
        {
            if (builder == null) throw Error.ArgumentNull(nameof(builder));
            builder.Append("Try(");
            Try?.BuildString(builder);
            builder.Append(", Finally: ");
            Finally?.BuildString(builder);
            builder.Append(")");
        }
    }
}