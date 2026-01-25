#nullable disable
using System.Runtime.CompilerServices;

namespace System.Dynamic;

/// <summary>
///     Represents the invoke dynamic operation at the call site, providing the binding semantic and the details about
///     the operation.
/// </summary>
public abstract class InvokeBinder : DynamicMetaObjectBinder
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Dynamic.InvokeBinder" />.</summary>
    /// <param name="callInfo">The signature of the arguments at the call site.</param>
    protected InvokeBinder(CallInfo callInfo)
    {
        ContractUtils.RequiresNotNull(callInfo, nameof(callInfo));
        CallInfo = callInfo;
    }

    /// <summary>The result type of the operation.</summary>
    /// <returns>The <see cref="T:System.Type" /> object representing the result type of the operation.</returns>
    public sealed override Type ReturnType => typeof(object);

    /// <summary>Gets the signature of the arguments at the call site.</summary>
    /// <returns>The signature of the arguments at the call site.</returns>
    public CallInfo CallInfo { get; }

    internal sealed override bool IsStandardBinder => true;

    /// <summary>Performs the binding of the dynamic invoke operation.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic invoke operation.</param>
    /// <param name="args">An array of arguments of the dynamic invoke operation.</param>
    public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
    {
        ContractUtils.RequiresNotNull(target, nameof(target));
        ContractUtils.RequiresNotNullItems(args, nameof(args));
        return target.BindInvoke(this, args);
    }

    /// <summary>Performs the binding of the dynamic invoke operation if the target dynamic object cannot bind.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic invoke operation.</param>
    /// <param name="args">The arguments of the dynamic invoke operation.</param>
    public DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args)
    {
        return FallbackInvoke(target, args, null);
    }

    /// <summary>Performs the binding of the dynamic invoke operation if the target dynamic object cannot bind.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic invoke operation.</param>
    /// <param name="args">The arguments of the dynamic invoke operation.</param>
    /// <param name="errorSuggestion">The binding result to use if binding fails, or null.</param>
    public abstract DynamicMetaObject FallbackInvoke(
        DynamicMetaObject target,
        DynamicMetaObject[] args,
        DynamicMetaObject errorSuggestion);
}