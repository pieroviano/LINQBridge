#nullable disable
using System.Dynamic.Utils;

namespace System.Linq.Expressions;

internal static class ConstantCheck
{
    internal static AnalyzeTypeIsResult AnalyzeTypeIs(TypeBinaryExpression typeIs)
    {
        return AnalyzeTypeIs(typeIs.Expression, typeIs.TypeOperand);
    }

    internal static bool IsNull(Expression e)
    {
        return e.NodeType == ExpressionType.Constant && ((ConstantExpression)e).Value == null;
    }

    private static AnalyzeTypeIsResult AnalyzeTypeIs(Expression operand, Type testType)
    {
        var type = operand.Type;
        if (type == typeof(void))
        {
            return AnalyzeTypeIsResult.KnownFalse;
        }

        var nonNullableType = type.GetNonNullableType();
        if (!testType.GetNonNullableType().IsAssignableFrom(nonNullableType))
        {
            return AnalyzeTypeIsResult.Unknown;
        }

        return type.IsValueType && !type.IsNullableType()
            ? AnalyzeTypeIsResult.KnownTrue
            : AnalyzeTypeIsResult.KnownAssignable;
    }
}