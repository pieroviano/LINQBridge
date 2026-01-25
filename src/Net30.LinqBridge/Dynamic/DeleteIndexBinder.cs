#nullable disable
using System.Runtime.CompilerServices;

namespace System.Dynamic;

/// <summary>
///     Represents the dynamic delete index operation at the call site, providing the binding semantic and the details
///     about the operation.
/// </summary>
public abstract class DeleteIndexBinder : DynamicMetaObjectBinder
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Dynamic.DeleteIndexBinder" />.</summary>
    /// <param name="callInfo">The signature of the arguments at the call site.</param>
    protected DeleteIndexBinder(CallInfo callInfo)
    {
        ContractUtils.RequiresNotNull(callInfo, nameof(callInfo));
        CallInfo = callInfo;
    }

    /// <summary>The result type of the operation.</summary>
    /// <returns>The <see cref="T:System.Type" /> object representing the result type of the operation.</returns>
    public sealed override Type ReturnType => typeof(void);

    /// <summary>Gets the signature of the arguments at the call site.</summary>
    /// <returns>The signature of the arguments at the call site.</returns>
    public CallInfo CallInfo { get; }

    internal sealed override bool IsStandardBinder => true;

    /// <summary>Performs the binding of the dynamic delete index operation.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic delete index operation.</param>
    /// <param name="args">An array of arguments of the dynamic delete index operation.</param>
    public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
    {
        ContractUtils.RequiresNotNull(target, nameof(target));
        ContractUtils.RequiresNotNullItems(args, nameof(args));
        return target.BindDeleteIndex(this, args);
    }

    /// <summary>Performs the binding of the dynamic delete index operation if the target dynamic object cannot bind.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic delete index operation.</param>
    /// <param name="indexes">The arguments of the dynamic delete index operation.</param>
    public DynamicMetaObject FallbackDeleteIndex(
        DynamicMetaObject target,
        DynamicMetaObject[] indexes)
    {
        return FallbackDeleteIndex(target, indexes, null);
    }

    /// <summary>
    ///     When overridden in the derived class, performs the binding of the dynamic delete index operation if the target
    ///     dynamic object cannot bind.
    /// </summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic delete index operation.</param>
    /// <param name="indexes">The arguments of the dynamic delete index operation.</param>
    /// <param name="errorSuggestion">The binding result to use if binding fails, or null.</param>
    public abstract DynamicMetaObject FallbackDeleteIndex(
        DynamicMetaObject target,
        DynamicMetaObject[] indexes,
        DynamicMetaObject errorSuggestion);
}