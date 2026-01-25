using System;
using System.Text;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a default value expression (ExpressionType.Default).
    /// Minimal implementation to support Expression.Empty() and Expression.Default(type).
    /// </summary>
    public sealed class DefaultExpression : Expression
    {
        internal DefaultExpression(Type type)
            : base(ExpressionType.Default, type)
        {
        }

        /// <summary>
        /// Gets the node type for this expression (Default).
        /// </summary>
        public override ExpressionType NodeType => ExpressionType.Default;

        internal override void BuildString(StringBuilder builder)
        {
            if (builder == null) throw Error.ArgumentNull(nameof(builder));
            builder.Append("Default(");
            builder.Append(Type?.Name ?? "void");
            builder.Append(")");
        }
    }
}