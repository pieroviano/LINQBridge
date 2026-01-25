#nullable disable
using System.Runtime.CompilerServices;

namespace System.Dynamic;

/// <summary>
///     Represents the convert dynamic operation at the call site, providing the binding semantic and the details
///     about the operation.
/// </summary>
public abstract class ConvertBinder : DynamicMetaObjectBinder
{
    /// <summary>Initializes a new instance of the <see cref="T:System.Dynamic.ConvertBinder" />.</summary>
    /// <param name="type">The type to convert to.</param>
    /// <param name="explicit">Is true if the conversion should consider explicit conversions; otherwise, false.</param>
    protected ConvertBinder(Type type, bool @explicit)
    {
        ContractUtils.RequiresNotNull(type, nameof(type));
        Type = type;
        Explicit = @explicit;
    }

    /// <summary>The type to convert to.</summary>
    /// <returns>The <see cref="T:System.Type" /> object that represents the type to convert to.</returns>
    public Type Type { get; }

    /// <summary>Gets the value indicating if the conversion should consider explicit conversions.</summary>
    /// <returns>True if there is an explicit conversion, otherwise false.</returns>
    public bool Explicit { get; }

    internal sealed override bool IsStandardBinder => true;

    /// <summary>The result type of the operation.</summary>
    /// <returns>The <see cref="T:System.Type" /> object representing the result type of the operation.</returns>
    public sealed override Type ReturnType => Type;

    /// <summary>Performs the binding of the dynamic convert operation.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic convert operation.</param>
    /// <param name="args">An array of arguments of the dynamic convert operation.</param>
    public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
    {
        ContractUtils.RequiresNotNull(target, nameof(target));
        ContractUtils.Requires(args == null || args.Length == 0, nameof(args));
        return target.BindConvert(this);
    }

    /// <summary>Performs the binding of the dynamic convert operation if the target dynamic object cannot bind.</summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic convert operation.</param>
    public DynamicMetaObject FallbackConvert(DynamicMetaObject target)
    {
        return FallbackConvert(target, null);
    }

    /// <summary>
    ///     When overridden in the derived class, performs the binding of the dynamic convert operation if the target
    ///     dynamic object cannot bind.
    /// </summary>
    /// <returns>The <see cref="T:System.Dynamic.DynamicMetaObject" /> representing the result of the binding.</returns>
    /// <param name="target">The target of the dynamic convert operation.</param>
    /// <param name="errorSuggestion">The binding result to use if binding fails, or null.</param>
    public abstract DynamicMetaObject FallbackConvert(
        DynamicMetaObject target,
        DynamicMetaObject errorSuggestion);
}