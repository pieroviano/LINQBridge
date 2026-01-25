using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace System.Runtime.CompilerServices;

/// <summary>Class responsible for runtime binding of the dynamic operations on the dynamic call site.</summary>
public abstract class CallSiteBinder
{
    private static readonly LabelTarget _updateLabel;

    internal Dictionary<Type, object> Cache;

    static CallSiteBinder()
    {
        _updateLabel = Expression.Label("CallSiteBinder.UpdateLabel");
    }

    /// <summary>Initializes a new instance of the <see cref="T:System.Runtime.CompilerServices.CallSiteBinder" /> class.</summary>
    protected CallSiteBinder()
    {
    }

    /// <summary>
    ///     Gets a label that can be used to cause the binding to be updated. It indicates that the expression's binding
    ///     is no longer valid. This is typically used when the "version" of a dynamic object has changed.
    /// </summary>
    /// <returns>
    ///     The <see cref="T:System.Linq.Expressions.LabelTarget" /> object representing a label that can be used to
    ///     trigger the binding update.
    /// </returns>
    public static LabelTarget UpdateLabel => _updateLabel;

    /// <summary>Performs the runtime binding of the dynamic operation on a set of arguments.</summary>
    /// <returns>
    ///     An Expression that performs tests on the dynamic operation arguments, and performs the dynamic operation if
    ///     the tests are valid. If the tests fail on subsequent occurrences of the dynamic operation, Bind will be called
    ///     again to produce a new <see cref="T:System.Linq.Expressions.Expression" /> for the new argument types.
    /// </returns>
    /// <param name="args">An array of arguments to the dynamic operation.</param>
    /// <param name="parameters">
    ///     The array of <see cref="T:System.Linq.Expressions.ParameterExpression" /> instances that
    ///     represent the parameters of the call site in the binding process.
    /// </param>
    /// <param name="returnLabel">A LabelTarget used to return the result of the dynamic binding.</param>
    public abstract Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters,
        LabelTarget returnLabel);

    /// <summary>
    ///     Provides low-level runtime binding support. Classes can override this and provide a direct delegate for the
    ///     implementation of rule. This can enable saving rules to disk, having specialized rules available at runtime, or
    ///     providing a different caching policy.
    /// </summary>
    /// <returns>A new delegate which replaces the CallSite Target.</returns>
    /// <param name="site">The CallSite the bind is being performed for.</param>
    /// <param name="args">The arguments for the binder.</param>
    /// <typeparam name="T">The target type of the CallSite.</typeparam>
    public virtual T BindDelegate<T>(CallSite<T> site, object[] args)
        where T : class
    {
        return default;
    }

    /// <summary>
    ///     Adds a target to the cache of known targets. The cached targets will be scanned before calling BindDelegate to
    ///     produce the new rule.
    /// </summary>
    /// <param name="target">The target delegate to be added to the cache.</param>
    /// <typeparam name="T">The type of target being added.</typeparam>
    protected void CacheTarget<T>(T target)
        where T : class
    {
        GetRuleCache<T>().AddRule(target);
    }

    internal T BindCore<T>(CallSite<T> site, object[] args)
        where T : class
    {
        var t = BindDelegate(site, args);
        if (t != null)
        {
            return t;
        }

        var instance = LambdaSignature<T>.Instance;
        var expression = Bind(args, instance.Parameters, instance.ReturnLabel);
        if (expression == null)
        {
            throw Error.NoOrInvalidRuleProduced();
        }

        var t1 = Stitch(expression, instance).Compile();
        CacheTarget<T>(t1);
        return t1;
    }

    internal RuleCache<T> GetRuleCache<T>()
        where T : class
    {
        object obj;
        if (Cache == null)
        {
            Interlocked.CompareExchange<Dictionary<Type, object>>(ref Cache, new Dictionary<Type, object>(), null);
        }

        var cache = Cache;
        lock (cache)
        {
            if (!cache.TryGetValue(typeof(T), out obj))
            {
                var type = typeof(T);
                var ruleCache = new RuleCache<T>();
                obj = ruleCache;
                cache[type] = ruleCache;
            }
        }

        return obj as RuleCache<T>;
    }

    private static Expression<T> Stitch<T>(Expression binding, LambdaSignature<T> signature)
        where T : class
    {
        var type = typeof(CallSite<T>);
        var expressions = new ReadOnlyCollectionBuilder<Expression>(3)
        {
            binding
        };
        var parameterExpression = Expression.Parameter(typeof(CallSite), "$site");
        var parameterExpressionArray = signature.Parameters.AddFirst(parameterExpression);
        expressions.Add(Expression.Label(UpdateLabel));
        Expression[] expressionArray = parameterExpressionArray;
        expressions.Add(Expression.Label(signature.ReturnLabel,
            Expression.Condition(
                Expression.Call(typeof(CallSiteOps).GetMethod("SetNotMatched"), parameterExpressionArray.First()),
                Expression.Default(signature.ReturnLabel.Type),
                Expression.Invoke(
                    Expression.Property(Expression.Convert(parameterExpression, type),
                        typeof(CallSite<T>).GetProperty("Update")),
                    new TrueReadOnlyCollection<Expression>(expressionArray)))));
        return new Expression<T>(Expression.Block(expressions), "CallSite.Target", true,
            new TrueReadOnlyCollection<ParameterExpression>(parameterExpressionArray));
    }

    private sealed class LambdaSignature<T>
        where T : class
    {
        internal static readonly LambdaSignature<T> Instance;

        internal readonly ReadOnlyCollection<ParameterExpression> Parameters;

        internal readonly LabelTarget ReturnLabel;

        static LambdaSignature()
        {
            Instance = new LambdaSignature<T>();
        }

        private LambdaSignature()
        {
            var type = typeof(T);
            if (!type.IsSubclassOf(typeof(MulticastDelegate)))
            {
                throw Error.TypeParameterIsNotDelegate(type);
            }

            var method = type.GetMethod("Invoke");
            var parametersCached = method.GetParameters();
            if (parametersCached[0].ParameterType != typeof(CallSite))
            {
                throw Error.FirstArgumentMustBeCallSite();
            }

            var parameterExpressionArray = new ParameterExpression[parametersCached.Length - 1];
            for (var i = 0; i < parameterExpressionArray.Length; i++)
            {
                parameterExpressionArray[i] = Expression.Parameter(parametersCached[i + 1].ParameterType,
                    string.Concat("$arg", i.ToString()));
            }

            Parameters = new TrueReadOnlyCollection<ParameterExpression>(parameterExpressionArray);
            ReturnLabel = Expression.Label(method.GetReturnType());
        }
    }
}