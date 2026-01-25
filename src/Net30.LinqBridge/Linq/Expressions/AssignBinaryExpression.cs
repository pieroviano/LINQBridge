#nullable disable
namespace System.Linq.Expressions;

internal sealed class AssignBinaryExpression : BinaryExpression
{
    internal AssignBinaryExpression(Expression left, Expression right)
        : base(left, right)
    {
    }

    public override Type Type => Left.Type;

    public override ExpressionType NodeType => ExpressionType.Assign;
}