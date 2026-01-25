#nullable disable
namespace System.Linq.Expressions;

public abstract class DynamicExpressionVisitor : ExpressionVisitor
{
    protected internal override Expression VisitDynamic(DynamicExpression node)
    {
        return base.VisitDynamic(node);
    }
}