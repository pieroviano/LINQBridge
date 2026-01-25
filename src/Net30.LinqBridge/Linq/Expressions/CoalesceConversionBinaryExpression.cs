#nullable disable
namespace System.Linq.Expressions;

internal sealed class CoalesceConversionBinaryExpression : BinaryExpression
{
    private readonly LambdaExpression _conversion;

    internal CoalesceConversionBinaryExpression(
        Expression left,
        Expression right,
        LambdaExpression conversion)
        : base(left, right)
    {
        _conversion = conversion;
    }

    public override ExpressionType NodeType => ExpressionType.Coalesce;

    public override Type Type => Right.Type;

    internal override LambdaExpression GetConversion()
    {
        return _conversion;
    }
}