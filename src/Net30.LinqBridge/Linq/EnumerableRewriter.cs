#nullable disable
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq;

internal class EnumerableRewriter : OldExpressionVisitor
{
    private static volatile ILookup<string, MethodInfo> _seqMethods;

    internal static MethodInfo FindMethod(
        Type type,
        string name,
        ReadOnlyCollection<Expression> args,
        Type[] typeArgs,
        BindingFlags flags)
    {
        var array = type.GetMethods(flags).Where(m => m.Name == name).ToArray();
        if (array.Length == 0)
        {
            throw Error.NoMethodOnType(name, type);
        }

        var methodInfo = array.FirstOrDefault(m => ArgsMatch(m, args, typeArgs));
        if (methodInfo == null)
        {
            throw Error.NoMethodOnTypeMatchingArguments(name, type);
        }

        return typeArgs != null ? methodInfo.MakeGenericMethod(typeArgs) : methodInfo;
    }

    internal override Expression VisitConstant(ConstantExpression c)
    {
        if (!(c.Value is EnumerableQuery enumerableQuery))
        {
            return c;
        }

        if (enumerableQuery.Enumerable == null)
        {
            return Visit(enumerableQuery.Expression);
        }

        var publicType = GetPublicType(enumerableQuery.Enumerable.GetType());
        return Expression.Constant(enumerableQuery.Enumerable, publicType);
    }

    internal override Expression VisitLambda(LambdaExpression lambda)
    {
        return lambda;
    }

    internal override Expression VisitMethodCall(MethodCallExpression m)
    {
        var instance = Visit(m.Object);
        var readOnlyCollection = VisitExpressionList(m.Arguments);
        if (instance == m.Object && readOnlyCollection == m.Arguments)
        {
            return m;
        }

        readOnlyCollection.ToArray();
        var genericArguments = m.Method.IsGenericMethod ? m.Method.GetGenericArguments() : null;
        if ((m.Method.IsStatic || m.Method.DeclaringType.IsAssignableFrom(instance.Type)) &&
            ArgsMatch(m.Method, readOnlyCollection, genericArguments))
        {
            return Expression.Call(instance, m.Method, readOnlyCollection);
        }

        if (m.Method.DeclaringType == typeof(Queryable))
        {
            var enumerableMethod = FindEnumerableMethod(m.Method.Name, readOnlyCollection, genericArguments);
            var arguments = FixupQuotedArgs(enumerableMethod, readOnlyCollection);
            return Expression.Call(instance, enumerableMethod, arguments);
        }

        var flags = (BindingFlags)(8 | (m.Method.IsPublic ? 16 /*0x10*/ : 32 /*0x20*/));
        var method = FindMethod(m.Method.DeclaringType, m.Method.Name, readOnlyCollection, genericArguments, flags);
        var arguments1 = FixupQuotedArgs(method, readOnlyCollection);
        return Expression.Call(instance, method, arguments1);
    }

    internal override Expression VisitParameter(ParameterExpression p)
    {
        return p;
    }

    private static bool ArgsMatch(MethodInfo m, ReadOnlyCollection<Expression> args, Type[] typeArgs)
    {
        var parameters = m.GetParameters();
        if (parameters.Length != args.Count || (!m.IsGenericMethod && typeArgs != null && typeArgs.Length != 0))
        {
            return false;
        }

        if (!m.IsGenericMethodDefinition && m.IsGenericMethod && m.ContainsGenericParameters)
        {
            m = m.GetGenericMethodDefinition();
        }

        if (m.IsGenericMethodDefinition)
        {
            if (typeArgs == null || typeArgs.Length == 0 || m.GetGenericArguments().Length != typeArgs.Length)
            {
                return false;
            }

            m = m.MakeGenericMethod(typeArgs);
            parameters = m.GetParameters();
        }

        var index = 0;
        for (var count = args.Count; index < count; ++index)
        {
            var type = parameters[index].ParameterType;
            if (type == null)
            {
                return false;
            }

            if (type.IsByRef)
            {
                type = type.GetElementType();
            }

            var operand = args[index];
            if (!type.IsAssignableFrom(operand.Type))
            {
                if (operand.NodeType == ExpressionType.Quote)
                {
                    operand = ((UnaryExpression)operand).Operand;
                }

                if (!type.IsAssignableFrom(operand.Type) && !type.IsAssignableFrom(StripExpression(operand.Type)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static MethodInfo FindEnumerableMethod(
        string name,
        ReadOnlyCollection<Expression> args,
        params Type[] typeArgs)
    {
        if (_seqMethods == null)
        {
            _seqMethods = typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public)
                .ToLookup(m => m.Name);
        }

        var methodInfo = _seqMethods[name].FirstOrDefault(m => ArgsMatch(m, args, typeArgs));
        if (methodInfo == null)
        {
            throw Error.NoMethodOnTypeMatchingArguments(name, typeof(Enumerable));
        }

        return typeArgs != null ? methodInfo.MakeGenericMethod(typeArgs) : methodInfo;
    }

    private ReadOnlyCollection<Expression> FixupQuotedArgs(
        MethodInfo mi,
        ReadOnlyCollection<Expression> argList)
    {
        var parameters = mi.GetParameters();
        if (parameters.Length != 0)
        {
            var sequence = (List<Expression>)null;
            var index1 = 0;
            for (var length = parameters.Length; index1 < length; ++index1)
            {
                var expression1 = argList[index1];
                var expression2 = FixupQuotedExpression(parameters[index1].ParameterType, expression1);
                if (sequence == null && expression2 != argList[index1])
                {
                    sequence = new List<Expression>(argList.Count);
                    for (var index2 = 0; index2 < index1; ++index2)
                    {
                        sequence.Add(argList[index2]);
                    }
                }

                sequence?.Add(expression2);
            }

            if (sequence != null)
            {
                argList = sequence.ToReadOnly();
            }
        }

        return argList;
    }

    private Expression FixupQuotedExpression(Type type, Expression expression)
    {
        Expression expression1;
        for (expression1 = expression;
             !type.IsAssignableFrom(expression1.Type);
             expression1 = ((UnaryExpression)expression1).Operand)
        {
            if (expression1.NodeType != ExpressionType.Quote)
            {
                if (!type.IsAssignableFrom(expression1.Type) && type.IsArray &&
                    expression1.NodeType == ExpressionType.NewArrayInit)
                {
                    var c = StripExpression(expression1.Type);
                    if (type.IsAssignableFrom(c))
                    {
                        var elementType = type.GetElementType();
                        var newArrayExpression = (NewArrayExpression)expression1;
                        var initializers = new List<Expression>(newArrayExpression.Expressions.Count);
                        var index = 0;
                        for (var count = newArrayExpression.Expressions.Count; index < count; ++index)
                        {
                            initializers.Add(FixupQuotedExpression(elementType, newArrayExpression.Expressions[index]));
                        }

                        expression = Expression.NewArrayInit(elementType, initializers);
                    }
                }

                return expression;
            }
        }

        return expression1;
    }

    private static Type GetPublicType(Type t)
    {
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Lookup<,>.Grouping))
        {
            return typeof(IGrouping<,>).MakeGenericType(t.GetGenericArguments());
        }

        if (!t.IsNestedPrivate)
        {
            return t;
        }

        foreach (var publicType in t.GetInterfaces())
        {
            if (publicType.IsGenericType && publicType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return publicType;
            }
        }

        return typeof(IEnumerable).IsAssignableFrom(t) ? typeof(IEnumerable) : t;
    }

    private static Type StripExpression(Type type)
    {
        var isArray = type.IsArray;
        var type1 = isArray ? type.GetElementType() : type;
        var genericType = TypeHelper.FindGenericType(typeof(Expression<>), type1);
        if (genericType != null)
        {
            type1 = genericType.GetGenericArguments()[0];
        }

        if (!isArray)
        {
            return type;
        }

        var arrayRank = type.GetArrayRank();
        return arrayRank != 1 ? type1.MakeArrayType(arrayRank) : type1.MakeArrayType();
    }
}