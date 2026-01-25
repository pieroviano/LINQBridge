using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Expressions;

public abstract partial class Expression
{
    private static void RequiresCanRead(Expression expression, string paramName)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(paramName);
        }
        var nodeType = expression.NodeType;
        if (nodeType == ExpressionType.MemberAccess)
        {
            var member = ((MemberExpression)expression).Member;
            if (member.MemberType == MemberTypes.Property && !((PropertyInfo)member).CanRead)
            {
                throw new ArgumentException(paramName);
            }
        }
        else if (nodeType == ExpressionType.Index)
        {
            var indexExpression = (IndexExpression)expression;
            if (indexExpression.Indexer != null && !indexExpression.Indexer.CanRead)
            {
                throw new ArgumentException(paramName);
            }
        }
    }

    private static void RequiresCanRead(IEnumerable<Expression> items, string paramName)
    {
        if (items != null)
        {
            var expressions = items as IList<Expression>;
            if (expressions != null)
            {
                for (var i = 0; i < expressions.Count; i++)
                {
                    RequiresCanRead(expressions[i], paramName);
                }
                return;
            }
            foreach (var item in items)
            {
                RequiresCanRead(item, paramName);
            }
        }
    }

    private static void RequiresCanWrite(Expression expression, string paramName)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(paramName);
        }

        var canWrite = false;
        var nodeType = expression.NodeType;
        if (nodeType == ExpressionType.MemberAccess)
        {
            var memberExpression = (MemberExpression)expression;
            var memberType = memberExpression.Member.MemberType;
            if (memberType == MemberTypes.Field)
            {
                var member = (FieldInfo)memberExpression.Member;
                canWrite = (member.IsInitOnly ? false : !member.IsLiteral);
            }
            else if (memberType == MemberTypes.Property)
            {
                canWrite = ((PropertyInfo)memberExpression.Member).CanWrite;
            }
        }
        else if (nodeType == ExpressionType.Parameter)
        {
            canWrite = true;
        }
        else if (nodeType == ExpressionType.Index)
        {
            var indexExpression = (IndexExpression)expression;
            canWrite = (indexExpression.Indexer == null ? true : indexExpression.Indexer.CanWrite);
        }

        if (!canWrite)
        {
            throw new ArgumentException(paramName);
        }
    }
}