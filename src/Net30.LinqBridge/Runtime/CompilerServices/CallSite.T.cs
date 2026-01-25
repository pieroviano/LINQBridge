using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Dynamic.Utils;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Expressions.Compiler;
using System.Reflection;

namespace System.Runtime.CompilerServices;

/// <summary>Dynamic site type.</summary>
/// <typeparam name="T">The delegate type.</typeparam>
public class CallSite<T> : CallSite
    where T : class
{
    private const int MaxRules = 10;

    private static T _CachedUpdate;

    private static volatile T _CachedNoMatch;

    internal T[] Rules;

    /// <summary>The Level 0 cache - a delegate specialized based on the site history.</summary>
    public T Target;

    private CallSite(CallSiteBinder binder) : base(binder)
    {
        Target = GetUpdateDelegate();
    }

    private CallSite() : base(null)
    {
    }

    /// <summary>The update delegate. Called when the dynamic site experiences cache miss.</summary>
    /// <returns>The update delegate.</returns>
    public T Update
    {
        get
        {
            if (!_match)
            {
                return _CachedUpdate;
            }

            return _CachedNoMatch;
        }
    }

    /// <summary>
    ///     Creates an instance of the dynamic call site, initialized with the binder responsible for the runtime binding
    ///     of the dynamic operations at this call site.
    /// </summary>
    /// <returns>The new instance of dynamic call site.</returns>
    /// <param name="binder">The binder responsible for the runtime binding of the dynamic operations at this call site.</param>
    public static CallSite<T> Create(CallSiteBinder binder)
    {
        if (!typeof(T).IsSubclassOf(typeof(MulticastDelegate)))
        {
            throw Error.TypeMustBeDerivedFromSystemDelegate();
        }

        return new CallSite<T>(binder);
    }

    internal void AddRule(T newRule)
    {
        T[] tArray;
        var rules = Rules;
        if (rules == null)
        {
            Rules = new[] { newRule };
            return;
        }

        if (rules.Length >= 9)
        {
            tArray = new T[10];
            Array.Copy(rules, 0, tArray, 1, 9);
        }
        else
        {
            tArray = new T[rules.Length + 1];
            Array.Copy(rules, 0, tArray, 1, rules.Length);
        }

        tArray[0] = newRule;
        Rules = tArray;
    }

    internal CallSite<T> CreateMatchMaker()
    {
        return new CallSite<T>();
    }

    internal T MakeUpdateDelegate()
    {
        Type[] typeArray;
        int length;
        var type = typeof(T);
        var method = type.GetMethod("Invoke");
        if (type.IsGenericType && IsSimpleSignature(method, out typeArray))
        {
            MethodInfo methodInfo = null;
            MethodInfo method1 = null;
            if (method.ReturnType == typeof(void))
            {
                if (type == DelegateHelpers.GetActionType(typeArray.AddFirst(typeof(CallSite))))
                {
                    var type1 = typeof(UpdateDelegates);
                    length = typeArray.Length;
                    methodInfo = type1.GetMethod(string.Concat("UpdateAndExecuteVoid", length.ToString()),
                        BindingFlags.Static | BindingFlags.NonPublic);
                    var type2 = typeof(UpdateDelegates);
                    length = typeArray.Length;
                    method1 = type2.GetMethod(string.Concat("NoMatchVoid", length.ToString()),
                        BindingFlags.Static | BindingFlags.NonPublic);
                }
            }
            else if (type == DelegateHelpers.GetFuncType(typeArray.AddFirst<Type>(typeof(CallSite))))
            {
                length = typeArray.Length - 1;
                methodInfo = typeof(UpdateDelegates).GetMethod(string.Concat("UpdateAndExecute", length.ToString()),
                    BindingFlags.Static | BindingFlags.NonPublic);
                length = typeArray.Length - 1;
                method1 = typeof(UpdateDelegates).GetMethod(string.Concat("NoMatch", length.ToString()),
                    BindingFlags.Static | BindingFlags.NonPublic);
            }

            if (methodInfo != null)
            {
                _CachedNoMatch = (T)(object)CreateDelegateHelper(type, method1.MakeGenericMethod(typeArray));
                return (T)(object)CreateDelegateHelper(type, methodInfo.MakeGenericMethod(typeArray));
            }
        }

        _CachedNoMatch = CreateCustomNoMatchDelegate(method);
        return CreateCustomUpdateDelegate(method);
    }

    internal void MoveRule(int i)
    {
        var rules = Rules;
        var t = rules[i];
        rules[i] = rules[i - 1];
        rules[i - 1] = rules[i - 2];
        rules[i - 2] = t;
    }

    private void ClearRuleCache()
    {
        Binder.GetRuleCache<T>();
        var cache = Binder.Cache;
        if (cache != null)
        {
            lock (cache)
            {
                cache.Clear();
            }
        }
    }

    private static Expression Convert(Expression arg, Type type)
    {
        if (TypeUtils.AreReferenceAssignable(type, arg.Type))
        {
            return arg;
        }

        return Expression.Convert(arg, type);
    }

    private T CreateCustomNoMatchDelegate(MethodInfo invoke)
    {
        var parameterExpressionArray = invoke.GetParameters().Map(p => Expression.Parameter(p.ParameterType, p.Name));
        return Expression
            .Lambda<T>(
                Expression.Block(
                    Expression.Call(typeof(CallSiteOps).GetMethod("SetNotMatched"),
                        Enumerable.First<ParameterExpression>(parameterExpressionArray)),
                    Expression.Default(invoke.GetReturnType())), parameterExpressionArray).Compile();
    }

    private T CreateCustomUpdateDelegate(MethodInfo invoke)
    {
        Expression expression;
        Expression[] expressionArray;
        var expressions = new List<Expression>();
        var parameterExpressions = new List<ParameterExpression>();
        var parameterExpressionArray = invoke.GetParameters().Map(p => Expression.Parameter(p.ParameterType, p.Name));
        var labelTarget = Expression.Label(invoke.GetReturnType());
        var typeArray = new[] { typeof(T) };
        var parameterExpression = parameterExpressionArray[0];
        var parameterExpressionArray1 = parameterExpressionArray.RemoveFirst();
        var parameterExpression1 = Expression.Variable(typeof(CallSite<T>), "this");
        parameterExpressions.Add(parameterExpression1);
        expressions.Add(Expression.Assign(parameterExpression1,
            Expression.Convert(parameterExpression, parameterExpression1.Type)));
        var parameterExpression2 = Expression.Variable(typeof(T[]), "applicable");
        parameterExpressions.Add(parameterExpression2);
        var parameterExpression3 = Expression.Variable(typeof(T), "rule");
        parameterExpressions.Add(parameterExpression3);
        var parameterExpression4 = Expression.Variable(typeof(T), "originalRule");
        parameterExpressions.Add(parameterExpression4);
        expressions.Add(Expression.Assign(parameterExpression4, Expression.Field(parameterExpression1, "Target")));
        ParameterExpression parameterExpression5 = null;
        if (labelTarget.Type != typeof(void))
        {
            var parameterExpression6 = Expression.Variable(labelTarget.Type, "result");
            parameterExpression5 = parameterExpression6;
            parameterExpressions.Add(parameterExpression6);
        }

        var parameterExpression7 = Expression.Variable(typeof(int), "count");
        parameterExpressions.Add(parameterExpression7);
        var parameterExpression8 = Expression.Variable(typeof(int), "index");
        parameterExpressions.Add(parameterExpression8);
        expressions.Add(Expression.Assign(parameterExpression,
            Expression.Call(typeof(CallSiteOps), "CreateMatchmaker", typeArray, parameterExpression1)));
        Expression expression1 = Expression.Call(typeof(CallSiteOps).GetMethod("GetMatch"), parameterExpression);
        Expression expression2 = Expression.Call(typeof(CallSiteOps).GetMethod("ClearMatch"), parameterExpression);
        var methodCallExpression = Expression.Call(typeof(CallSiteOps), "UpdateRules", typeArray, parameterExpression1,
            parameterExpression8);
        if (labelTarget.Type != typeof(void))
        {
            expressionArray = parameterExpressionArray;
            expression = Expression.Block(
                Expression.Assign(parameterExpression5,
                    Expression.Invoke(parameterExpression3, new TrueReadOnlyCollection<Expression>(expressionArray))),
                Expression.IfThen(expression1,
                    Expression.Block(methodCallExpression, Expression.Return(labelTarget, parameterExpression5))));
        }
        else
        {
            expressionArray = parameterExpressionArray;
            expression =
                Expression.Block(
                    Expression.Invoke(parameterExpression3, new TrueReadOnlyCollection<Expression>(expressionArray)),
                    Expression.IfThen(expression1,
                        Expression.Block(methodCallExpression, Expression.Return(labelTarget))));
        }

        Expression expression3 = Expression.Assign(parameterExpression3,
            Expression.ArrayAccess(parameterExpression2, parameterExpression8));
        var labelTarget1 = Expression.Label();
        var conditionalExpression = Expression.IfThen(Expression.Equal(parameterExpression8, parameterExpression7),
            Expression.Break(labelTarget1));
        var unaryExpression = Expression.PreIncrementAssign(parameterExpression8);
        expressions.Add(Expression.IfThen(
            Expression.NotEqual(
                Expression.Assign(parameterExpression2,
                    Expression.Call(typeof(CallSiteOps), "GetRules", typeArray, parameterExpression1)),
                Expression.Constant(null, parameterExpression2.Type)),
            Expression.Block(Expression.Assign(parameterExpression7, Expression.ArrayLength(parameterExpression2)),
                Expression.Assign(parameterExpression8, Expression.Constant(0)),
                Expression.Loop(
                    Expression.Block(conditionalExpression, expression3,
                        Expression.IfThen(
                            Expression.NotEqual(Expression.Convert(parameterExpression3, typeof(object)),
                                Expression.Convert(parameterExpression4, typeof(object))),
                            Expression.Block(
                                Expression.Assign(Expression.Field(parameterExpression1, "Target"),
                                    parameterExpression3), expression, expression2)), unaryExpression), labelTarget1,
                    null))));
        var parameterExpression9 = Expression.Variable(typeof(RuleCache<T>), "cache");
        parameterExpressions.Add(parameterExpression9);
        expressions.Add(Expression.Assign(parameterExpression9,
            Expression.Call(typeof(CallSiteOps), "GetRuleCache", typeArray, parameterExpression1)));
        expressions.Add(Expression.Assign(parameterExpression2,
            Expression.Call(typeof(CallSiteOps), "GetCachedRules", typeArray, parameterExpression9)));
        if (labelTarget.Type != typeof(void))
        {
            expressionArray = parameterExpressionArray;
            expression = Expression.Block(
                Expression.Assign(parameterExpression5,
                    Expression.Invoke(parameterExpression3, new TrueReadOnlyCollection<Expression>(expressionArray))),
                Expression.IfThen(expression1, Expression.Return(labelTarget, parameterExpression5)));
        }
        else
        {
            expressionArray = parameterExpressionArray;
            expression =
                Expression.Block(
                    Expression.Invoke(parameterExpression3, new TrueReadOnlyCollection<Expression>(expressionArray)),
                    Expression.IfThen(expression1, Expression.Return(labelTarget)));
        }

        var tryExpression = Expression.TryFinally(expression,
            Expression.IfThen(expression1,
                Expression.Block(
                    Expression.Call(typeof(CallSiteOps), "AddRule", typeArray, parameterExpression1,
                        parameterExpression3),
                    Expression.Call(typeof(CallSiteOps), "MoveRule", typeArray, parameterExpression9,
                        parameterExpression3, parameterExpression8))));
        expression3 = Expression.Assign(Expression.Field(parameterExpression1, "Target"),
            Expression.Assign(parameterExpression3,
                Expression.ArrayAccess(parameterExpression2, parameterExpression8)));
        expressions.Add(Expression.Assign(parameterExpression8, Expression.Constant(0)));
        expressions.Add(Expression.Assign(parameterExpression7, Expression.ArrayLength(parameterExpression2)));
        expressions.Add(Expression.Loop(
            Expression.Block(conditionalExpression, expression3, tryExpression, expression2, unaryExpression),
            labelTarget1, null));
        expressions.Add(Expression.Assign(parameterExpression3, Expression.Constant(null, parameterExpression3.Type)));
        var parameterExpression10 = Expression.Variable(typeof(object[]), "args");
        parameterExpressions.Add(parameterExpression10);
        expressions.Add(Expression.Assign(parameterExpression10,
            Expression.NewArrayInit(typeof(object), parameterExpressionArray1.Map(p => Convert(p, typeof(object))))));
        Expression expression4 =
            Expression.Assign(Expression.Field(parameterExpression1, "Target"), parameterExpression4);
        expression3 = Expression.Assign(Expression.Field(parameterExpression1, "Target"),
            Expression.Assign(parameterExpression3,
                Expression.Call(typeof(CallSiteOps), "Bind", typeArray,
                    Expression.Property(parameterExpression1, "Binder"), parameterExpression1, parameterExpression10)));
        tryExpression = Expression.TryFinally(expression,
            Expression.IfThen(expression1,
                Expression.Call(typeof(CallSiteOps), "AddRule", typeArray, parameterExpression1,
                    parameterExpression3)));
        expressions.Add(Expression.Loop(Expression.Block(expression4, expression3, tryExpression, expression2), null,
            null));
        expressions.Add(Expression.Default(labelTarget.Type));
        var expression5 = Expression.Lambda<T>(
            Expression.Label(labelTarget,
                Expression.Block(new ReadOnlyCollection<ParameterExpression>(parameterExpressions),
                    new ReadOnlyCollection<Expression>(expressions))), "CallSite.Target", true,
            new ReadOnlyCollection<ParameterExpression>(parameterExpressionArray));
        return expression5.Compile();
    }

    private static Delegate CreateDelegateHelper(Type delegateType, MethodInfo method)
    {
        return Delegate.CreateDelegate(delegateType, method);
    }

    private T GetUpdateDelegate()
    {
        return GetUpdateDelegate(ref _CachedUpdate);
    }

    private T GetUpdateDelegate(ref T addr)
    {
        if (addr == null)
        {
            addr = MakeUpdateDelegate();
        }

        return addr;
    }

    private static bool IsSimpleSignature(MethodInfo invoke, out Type[] sig)
    {
        var parametersCached = invoke.GetParameters();
        ContractUtils.Requires(
            parametersCached.Length == 0 ? false : parametersCached[0].ParameterType == typeof(CallSite), "T");
        var parameterType =
            new Type[invoke.ReturnType != typeof(void) ? parametersCached.Length : parametersCached.Length - 1];
        var flag = true;
        for (var i = 1; i < parametersCached.Length; i++)
        {
            var parameterInfo = parametersCached[i];
            if (parameterInfo.IsByRefParameter())
            {
                flag = false;
            }

            parameterType[i - 1] = parameterInfo.ParameterType;
        }

        if (invoke.ReturnType != typeof(void))
        {
            parameterType[parameterType.Length - 1] = invoke.ReturnType;
        }

        sig = parameterType;
        return flag;
    }
}