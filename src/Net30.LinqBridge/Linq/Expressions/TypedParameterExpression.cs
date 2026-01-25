#nullable disable
namespace System.Linq.Expressions;

internal class TypedParameterExpression : ParameterExpression
{
    internal TypedParameterExpression(Type type, string name)
        : base(name)
    {
        Type = type;
    }

    public sealed override Type Type { get; }
}