#nullable disable
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Expressions.Compiler;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace System.Dynamic;

/// <summary>
///     The dynamic call site binder that participates in the <see cref="T:System.Dynamic.DynamicMetaObject" />
///     binding protocol.
/// </summary>
public abstract class DynamicMetaObjectBinder : CallSiteBinder
{
    private static readonly Type ComObjectType = typeof(object).Assembly.GetType("System.__ComObject");

    /// <summary>Initializes a new instance of the <see cref="T:System.Dynamic.DynamicMetaObjectBinder" /> class.</summary>
    protected DynamicMetaObjectBinder()
    {
    }

    /// <summary>The result type of the operation.</summary>
    /// <returns>The <see cref="T:System.Type" /> object representing the result type of the operation.</returns>
    public virtual Type ReturnType => typeof(object);

    internal virtual bool IsStandardBinder => false;

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
    public sealed override Expression Bind(
        object[] args,
        ReadOnlyCollection<ParameterExpression> parameters,
        LabelTarget returnLabel)
    {
        ContractUtils.RequiresNotNull(args, nameof(args));
        ContractUtils.RequiresNotNull(parameters, nameof(parameters));
        ContractUtils.RequiresNotNull(returnLabel, nameof(returnLabel));
        if (args.Length == 0)
        {
            throw Error.OutOfRange("args.Length", 1);
        }

        if (parameters.Count == 0)
        {
            throw Error.OutOfRange("parameters.Count", 1);
        }

        if (args.Length != parameters.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(args));
        }

        Type type;
        if (IsStandardBinder)
        {
            type = ReturnType;
            if (returnLabel.Type != typeof(void) && !TypeUtils.AreReferenceAssignable(returnLabel.Type, type))
            {
                throw Error.BinderNotCompatibleWithCallSite(type, this, returnLabel.Type);
            }
        }
        else
        {
            type = returnLabel.Type;
        }

        var target = DynamicMetaObject.Create(args[0], parameters[0]);
        var argumentMetaObjects = CreateArgumentMetaObjects(args, parameters);
        var dynamicMetaObject = Bind(target, argumentMetaObjects);
        var ifTrue = dynamicMetaObject != null ? dynamicMetaObject.Expression : throw Error.BindingCannotBeNull();
        var restrictions = dynamicMetaObject.Restrictions;
        if (type != typeof(void) && !TypeUtils.AreReferenceAssignable(type, ifTrue.Type))
        {
            if (target.Value is IDynamicMetaObjectProvider)
            {
                throw Error.DynamicObjectResultNotAssignable(ifTrue.Type, target.Value.GetType(), this, type);
            }

            throw Error.DynamicBinderResultNotAssignable(ifTrue.Type, this, type);
        }

        if (IsStandardBinder && args[0] is IDynamicMetaObjectProvider && restrictions == BindingRestrictions.Empty)
        {
            throw Error.DynamicBindingNeedsRestrictions(target.Value.GetType(), this);
        }

        var bindingRestrictions = AddRemoteObjectRestrictions(restrictions, args, parameters);
        if (ifTrue.NodeType != ExpressionType.Goto)
        {
            ifTrue = Expression.Return(returnLabel, ifTrue);
        }

        if (bindingRestrictions != BindingRestrictions.Empty)
        {
            ifTrue = Expression.IfThen(bindingRestrictions.ToExpression(), ifTrue);
        }

        return ifTrue;
    }

    /// <summary>When overridden in the derived class, performs the binding of the dynamic operation.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic operation.</param>
    /// <param name="args">An array of arguments of the dynamic operation.</param>
    public abstract DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args);

    /// <summary>
    ///     Defers the binding of the operation until later time when the runtime values of all dynamic operation
    ///     arguments have been computed.
    /// </summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic operation.</param>
    /// <param name="args">An array of arguments of the dynamic operation.</param>
    public DynamicMetaObject Defer(DynamicMetaObject target, params DynamicMetaObject[] args)
    {
        ContractUtils.RequiresNotNull(target, nameof(target));
        if (args != null)
        {
            return MakeDeferred(target.Restrictions.Merge(BindingRestrictions.Combine(args)), args.AddFirst(target));
        }

        return MakeDeferred(target.Restrictions, target);
    }

    /// <summary>
    ///     Defers the binding of the operation until later time when the runtime values of all dynamic operation
    ///     arguments have been computed.
    /// </summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="args">An array of arguments of the dynamic operation.</param>
    public DynamicMetaObject Defer(params DynamicMetaObject[] args)
    {
        return MakeDeferred(BindingRestrictions.Combine(args), args);
    }

    /// <summary>
    ///     Gets an expression that will cause the binding to be updated. It indicates that the expression's binding is no
    ///     longer valid. This is typically used when the "version" of a dynamic object has changed.
    /// </summary>
    /// <returns>The update expression.</returns>
    /// <param name="type">
    ///     The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of the resulting expression;
    ///     any type is allowed.
    /// </param>
    public Expression GetUpdateExpression(Type type)
    {
        return Expression.Goto(UpdateLabel, type);
    }

    private static BindingRestrictions AddRemoteObjectRestrictions(
        BindingRestrictions restrictions,
        object[] args,
        ReadOnlyCollection<ParameterExpression> parameters)
    {
        for (var index = 0; index < parameters.Count; ++index)
        {
            var parameter = parameters[index];
            if (args[index] is MarshalByRefObject tp && !IsComObject(tp))
            {
                var restrictions1 = !RemotingServices.IsObjectOutOfAppDomain(tp)
                    ? BindingRestrictions.GetExpressionRestriction(Expression.AndAlso(
                        Expression.NotEqual(parameter, Expression.Constant(null)),
                        Expression.Not(Expression.Call(typeof(RemotingServices).GetMethod("IsObjectOutOfAppDomain"),
                            parameter))))
                    : BindingRestrictions.GetExpressionRestriction(Expression.AndAlso(
                        Expression.NotEqual(parameter, Expression.Constant(null)),
                        Expression.Call(typeof(RemotingServices).GetMethod("IsObjectOutOfAppDomain"), parameter)));
                restrictions = restrictions.Merge(restrictions1);
            }
        }

        return restrictions;
    }

    private static DynamicMetaObject[] CreateArgumentMetaObjects(
        object[] args,
        ReadOnlyCollection<ParameterExpression> parameters)
    {
        DynamicMetaObject[] argumentMetaObjects;
        if (args.Length != 1)
        {
            argumentMetaObjects = new DynamicMetaObject[args.Length - 1];
            for (var index = 1; index < args.Length; ++index)
            {
                argumentMetaObjects[index - 1] = DynamicMetaObject.Create(args[index], parameters[index]);
            }
        }
        else
        {
            argumentMetaObjects = DynamicMetaObject.EmptyMetaObjects;
        }

        return argumentMetaObjects;
    }

    private static bool IsComObject(object obj)
    {
        return obj != null && ComObjectType.IsAssignableFrom(obj.GetType());
    }

    private DynamicMetaObject MakeDeferred(BindingRestrictions rs, params DynamicMetaObject[] args)
    {
        var expressions = DynamicMetaObject.GetExpressions(args);
        return new DynamicMetaObject(
            DynamicExpression.Make(ReturnType, DelegateHelpers.MakeDeferredSiteDelegate(args, ReturnType), this,
                new TrueReadOnlyCollection<Expression>(expressions)), rs);
    }
}