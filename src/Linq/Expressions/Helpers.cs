using System;
using System.Linq.Expressions;

#nullable disable
namespace System.Linq.Expressions
{
    internal static class Helpers
    {
        // Minimal Convert helper used across the codebase to coerce expressions to a target type.
        internal static Expression Convert(Expression expression, Type type)
        {
                    if (expression == null) throw new ArgumentNullException("expression");
            if (type == null) throw new ArgumentNullException("type");

            // If already assignable, return as-is to avoid unnecessary Convert nodes.
            if (type.IsAssignableFrom(expression.Type))
            {
                return expression;
            }

            // Fallback: emit a Convert expression.
            return Expression.Convert(expression, type);
        }
    }
}