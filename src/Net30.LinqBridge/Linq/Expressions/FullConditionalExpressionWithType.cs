#nullable disable
namespace System.Linq.Expressions;

internal class FullConditionalExpressionWithType : FullConditionalExpression
{
    internal FullConditionalExpressionWithType(
        Expression test,
        Expression ifTrue,
        Expression ifFalse,
        Type type)
        : base(test, ifTrue, ifFalse)
    {
        Type = type;
    }

    public sealed override Type Type { get; }
}