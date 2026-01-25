using System.Text;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a goto/return expression that targets a LabelTarget.
    /// Minimal implementation to support Expression.Return(...) factories.
    /// </summary>
    public sealed class GotoExpression : Expression
    {
        private readonly LabelTarget _target;
        private readonly Expression _value;

        internal GotoExpression(LabelTarget target, Expression value, Type type)
            : base(ExpressionType.Goto, type)
        {
            _target = target ?? throw Error.ArgumentNull(nameof(target));
            _value = value;
        }

        /// <summary>Gets the label target.</summary>
        public LabelTarget Target => _target;

        /// <summary>Gets the value associated with the goto/return (may be null for void).</summary>
        public Expression Value => _value;

        /// <summary>Node type is Goto.</summary>
        public override ExpressionType NodeType => ExpressionType.Goto;

        /// <summary>Type of the expression equals target.Type (or void for void).</summary>
        public override Type Type => _target?.Type ?? base.Type;

        internal override void BuildString(StringBuilder builder)
        {
            if (builder == null) throw Error.ArgumentNull(nameof(builder));
            builder.Append("Goto(");
            builder.Append(_target?.Name ?? "label");
            if (_value != null)
            {
                builder.Append(", ");
                _value.BuildString(builder);
            }
            builder.Append(")");
        }
    }
}