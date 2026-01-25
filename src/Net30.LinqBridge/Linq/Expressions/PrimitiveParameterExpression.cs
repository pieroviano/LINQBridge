#nullable disable
namespace System.Linq.Expressions;

internal sealed class PrimitiveParameterExpression<T> : ParameterExpression
{
    internal PrimitiveParameterExpression(string name)
        : base(name)
    {
    }

    public override Type Type => typeof(T);
}