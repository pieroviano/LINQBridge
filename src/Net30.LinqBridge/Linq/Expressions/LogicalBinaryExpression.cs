#nullable disable
namespace System.Linq.Expressions;

internal sealed class LogicalBinaryExpression : BinaryExpression
{
    internal LogicalBinaryExpression(ExpressionType nodeType, Expression left, Expression right)
        : base(left, right)
    {
        NodeType = nodeType;
    }

    public override Type Type => typeof(bool);

    public override ExpressionType NodeType { get; }
}