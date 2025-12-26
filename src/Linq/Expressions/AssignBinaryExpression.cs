using System;

namespace System.Linq.Expressions;

internal sealed class AssignBinaryExpression : BinaryExpression
{
    public sealed override ExpressionType NodeType => ExpressionType.Assign;

    public sealed override Type Type => Left.Type;

    internal AssignBinaryExpression(Expression left, Expression right) : base(ExpressionType.Assign,left, right, left.Type)
    {
    }
}